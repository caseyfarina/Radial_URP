using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns audio prefabs with random meshes, materials and spotlight settings.
/// Works in both editor and builds.
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
    [SerializeField] private float ySpawnPosition = 0f;
    [SerializeField] private bool useGroundHeight = false;

    [Header("Audio Folder Settings")]
    [SerializeField] private string resourcesSamplesPath = "Samples";
    [SerializeField] private KeyCode nextFolderKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode previousFolderKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode spawnKey = KeyCode.Space;

    [Tooltip("List of folder names to use in builds (these should be subfolders in Resources/Samples)")]
    [SerializeField] private List<string> buildFolderNames = new List<string>() { "sauron", "music", "ambience" };

    [Header("Mesh Settings")]
    [SerializeField] private string resourcesMeshesPath = "EntityMeshes1";
    [SerializeField] private bool assignRandomMeshes = true;

    [Header("Material Settings")]
    [SerializeField] private string resourcesMaterialsPath = "EntityMaterials";
    [SerializeField] private bool assignRandomMaterials = true;

    [Header("Spotlight Settings")]
    [SerializeField, Range(0f, 1f)] private float spotlightDeleteProbability = 0.5f;
    [SerializeField]
    private Color[] spotlightColors = new Color[] {
        Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta
    };
    [SerializeField, Range(0.1f, 8f)] private float minIntensity = 0.5f;
    [SerializeField, Range(0.1f, 8f)] private float maxIntensity = 3f;
    [SerializeField] private string spotlightChildName = "SpotLight";

    [Header("UI Settings")]
    [SerializeField] private TMP_Text folderDisplayText;
    [SerializeField] private string displayPrefix = "Folder: ";

    [Header("Organization")]
    [SerializeField] private Transform spawnedObjectsParent;

    private Camera mainCamera;
    private string[] availableFolders;
    private int currentFolderIndex = 0;
    private string currentFolder => availableFolders != null && availableFolders.Length > 0 ?
                                   availableFolders[currentFolderIndex] : "";

    // Current selections
    private Mesh[] availableMeshes;
    private int currentMeshIndex = 0;
    private Mesh currentMesh => availableMeshes != null && availableMeshes.Length > 0 ?
                              availableMeshes[currentMeshIndex] : null;

    private Material[] availableMaterials;
    private int currentMaterialIndex = 0;
    private Material currentMaterial => availableMaterials != null && availableMaterials.Length > 0 ?
                                       availableMaterials[currentMaterialIndex] : null;

    private Color currentSpotlightColor = Color.white;
    private float currentSpotlightIntensity = 1f;
    private bool currentSpotlightEnabled;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("AudioPrefabSpawner: No main camera found!");
        }

        // Initialize parent transform if needed
        if (spawnedObjectsParent == null)
        {
            GameObject parentObj = new GameObject("SpawnedAudioPrefabs");
            spawnedObjectsParent = parentObj.transform;
        }

        // Load available resources
        LoadAvailableFolders();
        LoadAvailableMeshes();
        LoadAvailableMaterials();

        // Initialize spotlight settings
        RandomizeSpotlightSettings();
    }

    private void LoadAvailableFolders()
    {
        List<string> folderList = new List<string>();

        try
        {
#if UNITY_EDITOR
            // In editor, use Directory.GetDirectories to find folders
            string samplesPath = Path.Combine(Application.dataPath, "Resources", resourcesSamplesPath);

            if (Directory.Exists(samplesPath))
            {
                folderList = Directory.GetDirectories(samplesPath)
                    .Select(Path.GetFileName)
                    .ToList();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: Directory does not exist: {samplesPath}");
                // Fall back to the build folder names
                folderList = new List<string>(buildFolderNames);
            }
#else
            // In builds, use the pre-defined folder list
            folderList = new List<string>(buildFolderNames);
            
            // Validate that these folders actually exist by attempting to load a test file from each
            for (int i = folderList.Count - 1; i >= 0; i--)
            {
                string testPath = $"{resourcesSamplesPath}/{folderList[i]}/.exists";
                // We don't actually need to load anything - just see if the folder exists
                // If any folder doesn't exist, we'll get null (which is fine)
                // We keep folders in the list even if they're empty
            }
#endif

            availableFolders = folderList.ToArray();

            if (availableFolders.Length > 0)
            {
                Debug.Log($"AudioPrefabSpawner: Found {availableFolders.Length} folders");
                UpdateFolderDisplayText();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: No folders found in {resourcesSamplesPath}");
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
        }
    }

    private void LoadAvailableMeshes()
    {
        if (!assignRandomMeshes) return;

        try
        {
            Object[] meshObjects = Resources.LoadAll(resourcesMeshesPath, typeof(Mesh));
            availableMeshes = meshObjects.Cast<Mesh>().ToArray();

            if (availableMeshes.Length > 0)
            {
                Debug.Log($"AudioPrefabSpawner: Found {availableMeshes.Length} meshes");
                SelectRandomMesh();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: No meshes found in Resources/{resourcesMeshesPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AudioPrefabSpawner: Error loading meshes: {e.Message}");
            availableMeshes = new Mesh[0];
        }
    }

    private void LoadAvailableMaterials()
    {
        if (!assignRandomMaterials) return;

        try
        {
            Object[] materialObjects = Resources.LoadAll(resourcesMaterialsPath, typeof(Material));
            availableMaterials = materialObjects.Cast<Material>().ToArray();

            if (availableMaterials.Length > 0)
            {
                Debug.Log($"AudioPrefabSpawner: Found {availableMaterials.Length} materials");
                SelectRandomMaterial();
            }
            else
            {
                Debug.LogWarning($"AudioPrefabSpawner: No materials found in Resources/{resourcesMaterialsPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AudioPrefabSpawner: Error loading materials: {e.Message}");
            availableMaterials = new Material[0];
        }
    }

    private void SelectRandomMesh()
    {
        if (availableMeshes == null || availableMeshes.Length == 0) return;
        currentMeshIndex = Random.Range(0, availableMeshes.Length);
    }

    private void SelectRandomMaterial()
    {
        if (availableMaterials == null || availableMaterials.Length == 0) return;
        currentMaterialIndex = Random.Range(0, availableMaterials.Length);
    }

    private void RandomizeSpotlightSettings()
    {
        // Determine if spotlight will be enabled
        currentSpotlightEnabled = Random.value > spotlightDeleteProbability;

        // Select random color and intensity
        if (spotlightColors != null && spotlightColors.Length > 0)
        {
            currentSpotlightColor = spotlightColors[Random.Range(0, spotlightColors.Length)];
        }

        currentSpotlightIntensity = Random.Range(minIntensity, maxIntensity);
    }

    private void Update()
    {
        // Check for folder cycling keys
        if (availableFolders != null && availableFolders.Length > 0)
        {
            if (Input.GetKeyDown(nextFolderKey))
            {
                CycleFolder(1);

                // Select new random appearance when changing folder
                if (assignRandomMeshes) SelectRandomMesh();
                if (assignRandomMaterials) SelectRandomMaterial();
                RandomizeSpotlightSettings();
            }
            else if (Input.GetKeyDown(previousFolderKey))
            {
                CycleFolder(-1);

                // Select new random appearance when changing folder
                if (assignRandomMeshes) SelectRandomMesh();
                if (assignRandomMaterials) SelectRandomMaterial();
                RandomizeSpotlightSettings();
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
    }

    private void UpdateFolderDisplayText()
    {
        if (folderDisplayText != null)
        {
            folderDisplayText.text = displayPrefix + currentFolder;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;

        if (useGroundHeight)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
            {
                return hit.point;
            }
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (ray.direction.y != 0) // Avoid division by zero
            {
                float t = (ySpawnPosition - ray.origin.y) / ray.direction.y;
                if (t > 0) // Only use result if it's in front of the camera
                {
                    Vector3 position = ray.origin + ray.direction * t;
                    return position;
                }
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

        // Assign the mesh if available
        if (assignRandomMeshes && currentMesh != null)
        {
            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = currentMesh;
            }
        }

        // Assign the material if available
        if (assignRandomMaterials && currentMaterial != null)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = currentMaterial;
            }
        }

        // Handle spotlight child
        Transform spotlightChild = instance.transform.Find(spotlightChildName);
        if (spotlightChild != null)
        {
            if (!currentSpotlightEnabled)
            {
                // Delete the spotlight based on probability
                Destroy(spotlightChild.gameObject);
            }
            else
            {
                // Change spotlight color and intensity
                Light spotlightComponent = spotlightChild.GetComponent<Light>();
                if (spotlightComponent != null)
                {
                    spotlightComponent.color = currentSpotlightColor;
                    spotlightComponent.intensity = currentSpotlightIntensity;
                }
            }
        }

        // Set the audio folder on the newly spawned prefab
        if (!string.IsNullOrEmpty(currentFolder))
        {
            RandomAudioPlayer audioPlayer = instance.GetComponent<RandomAudioPlayer>();
            if (audioPlayer != null)
            {
                // Use reflection to access the private field
                System.Reflection.FieldInfo folderField = typeof(RandomAudioPlayer).GetField("selectedFolder",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (folderField != null)
                {
                    folderField.SetValue(audioPlayer, currentFolder);

                    // Call the LoadAudioClips method to refresh
                    audioPlayer.SendMessage("LoadAudioClips", null, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AudioPrefabSpawner))]
public class AudioPrefabSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Use the default inspector
        DrawDefaultInspector();

        // Add simple control instructions
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Controls:\n" +
            "• Press Space to spawn prefab\n" +
            "• Press Up/Down arrows to change folder\n\n" +
            "Each folder change randomizes mesh, material, and spotlight settings.",
            MessageType.Info
        );

        // Build-specific information
        EditorGUILayout.HelpBox(
            "Build Information:\n" +
            "Make sure to populate the 'Build Folder Names' list with the names of your Audio folders.\n" +
            "These names will be used in builds where direct folder access is not available.",
            MessageType.Info
        );
    }
}
#endif