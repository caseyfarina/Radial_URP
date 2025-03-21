using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;  // For TextMeshPro support

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns an audio prefab when spacebar is pressed and allows cycling through audio folders
/// with arrow keys. Follows a factory pattern where folder selection only affects new prefabs.
/// </summary>
public class AudioPrefabSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject audioPrefab;
    [SerializeField, Range(0.1f, 5f)] private float scaleMin = 0.5f;
    [SerializeField, Range(0.1f, 5f)] private float scaleMax = 1.5f;
    [SerializeField, Range(0f, 360f)] private float yRotationMin = 0f;
    [SerializeField, Range(0f, 360f)] private float yRotationMax = 360f;

    [Header("Placement Settings")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float ySpawnPosition = 0f;
    [SerializeField] private bool useGroundHeight = false;

    [Header("Audio Folder Settings")]
    [SerializeField] private string resourcesSamplesPath = "Samples";
    [SerializeField] private KeyCode nextFolderKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode previousFolderKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode spawnKey = KeyCode.Space;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text folderDisplayText;
    [SerializeField] private string displayPrefix = "Folder: ";
    [SerializeField] private Color highlightColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private bool useHighlightColor = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    [Header("Organization")]
    [SerializeField] private Transform spawnedObjectsParent;
    [SerializeField] private bool createParentIfMissing = true;

    private Camera mainCamera;
    private string[] availableFolders;
    private int currentFolderIndex = 0;
    private string currentFolder => availableFolders != null && availableFolders.Length > 0 ?
                                   availableFolders[currentFolderIndex] : "";
    private Color defaultTextColor;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("AudioPrefabSpawner: No main camera found!");
        }

        // Initialize parent transform if needed
        if (spawnedObjectsParent == null && createParentIfMissing)
        {
            GameObject parentObj = new GameObject("SpawnedAudioPrefabs");
            spawnedObjectsParent = parentObj.transform;
        }

        // Initialize UI
        InitializeUI();

        // Load available folders
        LoadAvailableFolders();
    }

    private void InitializeUI()
    {
        if (folderDisplayText != null)
        {
            // Store the default text color for later use
            defaultTextColor = folderDisplayText.color;

            // Set initial text
            UpdateFolderDisplayText();
        }
    }

    private void LoadAvailableFolders()
    {
        // Try to load folders from the Resources directory
        try
        {
#if UNITY_EDITOR
            string samplesPath = Path.Combine(Application.dataPath, "Resources", resourcesSamplesPath);

            if (Directory.Exists(samplesPath))
            {
                availableFolders = Directory.GetDirectories(samplesPath)
                    .Select(Path.GetFileName)
                    .ToArray();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: Directory does not exist: {samplesPath}");
                availableFolders = new string[0];
            }
#else
            // At runtime, we can't directly access the file system
            // Could use Resources.LoadAll, but can't easily get folder names
            // This is a limitation - consider using a ScriptableObject to store folder names
            availableFolders = new string[0];
            Debug.LogWarning("AudioPrefabSpawner: Can't load folder list at runtime. Consider using a ScriptableObject to list folders.");
#endif

            if (availableFolders.Length > 0)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"AudioPrefabSpawner: Found {availableFolders.Length} folders: {string.Join(", ", availableFolders)}");
                }

                // Update UI text after loading folders
                UpdateFolderDisplayText();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: No folders found in {resourcesSamplesPath}");

                // Update UI to show no folders are available
                if (folderDisplayText != null)
                {
                    folderDisplayText.text = displayPrefix + "No folders found";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AudioPrefabSpawner: Error loading folders: {e.Message}");
            availableFolders = new string[0];

            // Update UI to show error
            if (folderDisplayText != null)
            {
                folderDisplayText.text = displayPrefix + "Error loading folders";
            }
        }
    }

    private void Update()
    {
        // Check for folder cycling keys
        if (availableFolders != null && availableFolders.Length > 0)
        {
            if (Input.GetKeyDown(nextFolderKey))
            {
                CycleFolder(1);

                // Display notification about current folder
                if (showDebugInfo)
                {
                    Debug.Log($"Selected folder: {currentFolder} - Ready to spawn objects with this folder");
                }
            }
            else if (Input.GetKeyDown(previousFolderKey))
            {
                CycleFolder(-1);

                // Display notification about current folder
                if (showDebugInfo)
                {
                    Debug.Log($"Selected folder: {currentFolder} - Ready to spawn objects with this folder");
                }
            }
        }

        // Check for spawn key
        if (Input.GetKeyDown(spawnKey) && audioPrefab != null)
        {
            Vector3 spawnPosition = GetMouseWorldPosition();

            if (spawnPosition != Vector3.zero)
            {
                SpawnAudioPrefab(spawnPosition);
            }
        }
    }

    private void CycleFolder(int direction)
    {
        if (availableFolders == null || availableFolders.Length == 0) return;

        // Calculate new index with wrap-around
        currentFolderIndex = (currentFolderIndex + direction + availableFolders.Length) % availableFolders.Length;

        // Update the UI text
        UpdateFolderDisplayText();

        // Update UI or display message to indicate the currently selected folder
        if (showDebugInfo)
        {
            Debug.Log($"AudioPrefabSpawner: Selected folder: {currentFolder} (factory setting for new prefabs)");
        }
    }

    private void UpdateFolderDisplayText()
    {
        if (folderDisplayText != null)
        {
            // Update the text with current folder name
            folderDisplayText.text = displayPrefix + currentFolder;

            // Apply highlight color if enabled
            if (useHighlightColor)
            {
                folderDisplayText.color = highlightColor;

                // Use an animation to fade back to default color after a short delay
                CancelInvoke(nameof(ResetTextColor));
                Invoke(nameof(ResetTextColor), 0.5f);
            }
        }
    }

    private void ResetTextColor()
    {
        if (folderDisplayText != null)
        {
            folderDisplayText.color = defaultTextColor;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        // Get mouse position in screen space
        Vector3 mousePos = Input.mousePosition;

        if (useGroundHeight)
        {
            // Use raycast to find the ground
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            // Perform the raycast
            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, groundLayerMask))
            {
                return hit.point;
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("AudioPrefabSpawner: Raycast did not hit any ground!");
                return Vector3.zero;
            }
        }
        else
        {
            // Project to fixed Y coordinate
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            // Calculate where the ray intersects our Y plane
            if (ray.direction.y != 0) // Avoid division by zero
            {
                float t = (ySpawnPosition - ray.origin.y) / ray.direction.y;

                if (t > 0) // Only use result if it's in front of the camera
                {
                    // Get the position and clamp Y to our target height
                    Vector3 position = ray.origin + ray.direction * t;
                    return position;
                }
            }

            if (showDebugInfo)
            {
                Debug.LogWarning("AudioPrefabSpawner: Could not project mouse position onto XZ plane!");
            }
        }

        return Vector3.zero;
    }

    private void SpawnAudioPrefab(Vector3 position)
    {
        // Ensure Y position is correct if not using ground height
        if (!useGroundHeight)
        {
            position.y = ySpawnPosition;
        }

        // Generate random scale and rotation
        float randomScale = Random.Range(scaleMin, scaleMax);
        Vector3 scale = new Vector3(randomScale, randomScale, randomScale);

        float yRotation = Random.Range(yRotationMin, yRotationMax);
        Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Instantiate the prefab with random properties
        GameObject instance = Instantiate(audioPrefab, position, rotation);
        instance.transform.localScale = scale;

        // Parent the object if parent transform is set
        if (spawnedObjectsParent != null)
        {
            instance.transform.SetParent(spawnedObjectsParent);
        }

        // Set the audio folder on the newly spawned prefab only
        if (!string.IsNullOrEmpty(currentFolder))
        {
            RandomAudioPlayer audioPlayer = instance.GetComponent<RandomAudioPlayer>();
            if (audioPlayer != null)
            {
                // Use reflection to access the private field (since we can't modify the original class)
                System.Reflection.FieldInfo folderField = typeof(RandomAudioPlayer).GetField("selectedFolder",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (folderField != null)
                {
                    folderField.SetValue(audioPlayer, currentFolder);

                    // Call the LoadAudioClips method to refresh
                    audioPlayer.SendMessage("LoadAudioClips", null, SendMessageOptions.DontRequireReceiver);

                    // Optional: Name the instance to indicate which folder it's using
                    instance.name = $"AudioPrefab_{currentFolder}";

                    if (showDebugInfo)
                    {
                        Debug.Log($"Spawned audio prefab with folder: {currentFolder}");
                    }
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning("AudioPrefabSpawner: Could not access selectedFolder field using reflection");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("AudioPrefabSpawner: Prefab does not have a RandomAudioPlayer component");
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Spawned audio prefab at {position}, scale: {scale.x}, Y rotation: {rotation.eulerAngles.y}");
        }
    }

    /// <summary>
    /// Get the currently selected folder name
    /// </summary>
    public string GetCurrentFolder()
    {
        return currentFolder;
    }

    /// <summary>
    /// Set the current folder by name
    /// </summary>
    public void SetFolder(string folderName)
    {
        if (availableFolders == null || availableFolders.Length == 0) return;

        for (int i = 0; i < availableFolders.Length; i++)
        {
            if (availableFolders[i] == folderName)
            {
                currentFolderIndex = i;
                UpdateFolderDisplayText();
                break;
            }
        }
    }

    /// <summary>
    /// Set the current folder by index
    /// </summary>
    public void SetFolderIndex(int index)
    {
        if (availableFolders == null || availableFolders.Length == 0) return;

        currentFolderIndex = Mathf.Clamp(index, 0, availableFolders.Length - 1);
        UpdateFolderDisplayText();
    }

    /// <summary>
    /// Get the list of available folders
    /// </summary>
    public string[] GetAvailableFolders()
    {
        return availableFolders;
    }

    /// <summary>
    /// Force a refresh of the available folders
    /// </summary>
    public void RefreshFolders()
    {
        LoadAvailableFolders();
        UpdateFolderDisplayText();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AudioPrefabSpawner))]
public class AudioPrefabSpawnerEditor : Editor
{
    private SerializedProperty audioPrefabProp;
    private SerializedProperty scaleMinProp;
    private SerializedProperty scaleMaxProp;
    private SerializedProperty yRotationMinProp;
    private SerializedProperty yRotationMaxProp;
    private SerializedProperty groundLayerMaskProp;
    private SerializedProperty maxRaycastDistanceProp;
    private SerializedProperty ySpawnPositionProp;
    private SerializedProperty useGroundHeightProp;
    private SerializedProperty resourcesSamplesPathProp;
    private SerializedProperty nextFolderKeyProp;
    private SerializedProperty previousFolderKeyProp;
    private SerializedProperty spawnKeyProp;
    private SerializedProperty showDebugInfoProp;
    private SerializedProperty spawnedObjectsParentProp;
    private SerializedProperty createParentIfMissingProp;
    private SerializedProperty folderDisplayTextProp;
    private SerializedProperty displayPrefixProp;
    private SerializedProperty highlightColorProp;
    private SerializedProperty useHighlightColorProp;

    private void OnEnable()
    {
        audioPrefabProp = serializedObject.FindProperty("audioPrefab");
        scaleMinProp = serializedObject.FindProperty("scaleMin");
        scaleMaxProp = serializedObject.FindProperty("scaleMax");
        yRotationMinProp = serializedObject.FindProperty("yRotationMin");
        yRotationMaxProp = serializedObject.FindProperty("yRotationMax");
        groundLayerMaskProp = serializedObject.FindProperty("groundLayerMask");
        maxRaycastDistanceProp = serializedObject.FindProperty("maxRaycastDistance");
        ySpawnPositionProp = serializedObject.FindProperty("ySpawnPosition");
        useGroundHeightProp = serializedObject.FindProperty("useGroundHeight");
        resourcesSamplesPathProp = serializedObject.FindProperty("resourcesSamplesPath");
        nextFolderKeyProp = serializedObject.FindProperty("nextFolderKey");
        previousFolderKeyProp = serializedObject.FindProperty("previousFolderKey");
        spawnKeyProp = serializedObject.FindProperty("spawnKey");
        showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
        spawnedObjectsParentProp = serializedObject.FindProperty("spawnedObjectsParent");
        createParentIfMissingProp = serializedObject.FindProperty("createParentIfMissing");
        folderDisplayTextProp = serializedObject.FindProperty("folderDisplayText");
        displayPrefixProp = serializedObject.FindProperty("displayPrefix");
        highlightColorProp = serializedObject.FindProperty("highlightColor");
        useHighlightColorProp = serializedObject.FindProperty("useHighlightColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(audioPrefabProp);

        // Check if prefab has RandomAudioPlayer component
        GameObject prefab = audioPrefabProp.objectReferenceValue as GameObject;
        if (prefab != null)
        {
            bool hasAudioPlayer = false;

#if UNITY_2018_3_OR_NEWER
            // Use prefab API if available
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                hasAudioPlayer = prefab.GetComponent<RandomAudioPlayer>() != null;
            }
#else
            // Fallback for older Unity versions
            hasAudioPlayer = prefab.GetComponent<RandomAudioPlayer>() != null;
#endif

            if (!hasAudioPlayer)
            {
                EditorGUILayout.HelpBox("The selected prefab does not have a RandomAudioPlayer component!", MessageType.Warning);
            }
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(scaleMinProp);
        EditorGUILayout.PropertyField(scaleMaxProp);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(yRotationMinProp);
        EditorGUILayout.PropertyField(yRotationMaxProp);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(useGroundHeightProp);

        // Change inspector visibility based on useGroundHeight setting
        if (useGroundHeightProp.boolValue)
        {
            EditorGUILayout.PropertyField(groundLayerMaskProp);
            EditorGUILayout.PropertyField(maxRaycastDistanceProp);
        }
        else
        {
            EditorGUILayout.PropertyField(ySpawnPositionProp);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Audio Folder Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(resourcesSamplesPathProp);
        EditorGUILayout.PropertyField(spawnKeyProp);
        EditorGUILayout.PropertyField(nextFolderKeyProp);
        EditorGUILayout.PropertyField(previousFolderKeyProp);

        // Display available folders if in play mode
        if (Application.isPlaying)
        {
            AudioPrefabSpawner spawner = (AudioPrefabSpawner)target;
            string[] folders = spawner.GetAvailableFolders();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Available Folders:", EditorStyles.boldLabel);

            if (folders != null && folders.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                string currentFolder = spawner.GetCurrentFolder();

                foreach (string folder in folders)
                {
                    bool isCurrent = folder == currentFolder;

                    EditorGUILayout.BeginHorizontal();

                    if (isCurrent)
                    {
                        EditorGUILayout.LabelField("▶ " + folder, EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("   " + folder);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No folders found", MessageType.Info);
            }

            if (GUILayout.Button("Refresh Folders"))
            {
                spawner.RefreshFolders();
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("UI Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(folderDisplayTextProp);
        EditorGUILayout.PropertyField(displayPrefixProp);
        EditorGUILayout.PropertyField(useHighlightColorProp);

        if (useHighlightColorProp.boolValue)
        {
            EditorGUILayout.PropertyField(highlightColorProp);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Organization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnedObjectsParentProp);
        EditorGUILayout.PropertyField(createParentIfMissingProp);

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(showDebugInfoProp);

        serializedObject.ApplyModifiedProperties();

        // Add button to display key binding info
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            $"Controls:\n" +
            $"• Press {spawnKeyProp.enumDisplayNames[spawnKeyProp.enumValueIndex]} to spawn audio prefab at mouse position\n" +
            $"• Press {nextFolderKeyProp.enumDisplayNames[nextFolderKeyProp.enumValueIndex]} to cycle to next audio folder\n" +
            $"• Press {previousFolderKeyProp.enumDisplayNames[previousFolderKeyProp.enumValueIndex]} to cycle to previous audio folder\n\n" +
            $"Factory Pattern: Changing folders only affects newly spawned prefabs.",
            MessageType.Info
        );

        // Add warning if no TMP_Text component is assigned
        if (folderDisplayTextProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "No Text display object assigned. Assign a TextMeshPro component to display the current folder name.",
                MessageType.Warning
            );
        }
    }
}
#endif