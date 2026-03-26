using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns different prefabs at mouse position on key press
/// </summary>
public class KeyboardPrefabSpawner : MonoBehaviour
{
    [System.Serializable]
    public class KeyPrefabPair
    {
        public KeyCode key;
        public GameObject prefab;
        [Tooltip("Optional name to identify this prefab")]
        public string prefabName;

        [Header("Scale Randomization")]
        [Tooltip("Minimum random scale to apply")]
        [Range(0.1f, 5f)]
        public float scaleMin = 0.5f;

        [Tooltip("Maximum random scale to apply")]
        [Range(0.1f, 5f)]
        public float scaleMax = 1.5f;

        [Header("Rotation Randomization")]
        [Tooltip("Minimum random Y rotation to apply (degrees)")]
        [Range(0f, 360f)]
        public float yRotationMin = 0f;

        [Tooltip("Maximum random Y rotation to apply (degrees)")]
        [Range(0f, 360f)]
        public float yRotationMax = 360f;
    }

    [Header("Spawn Settings")]
    [SerializeField]
    private KeyPrefabPair[] keyPrefabPairs = new KeyPrefabPair[]
    {
        new KeyPrefabPair { key = KeyCode.A, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.S, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.D, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.F, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.G, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.H, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.J, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.K, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f },
        new KeyPrefabPair { key = KeyCode.L, scaleMin = 0.5f, scaleMax = 1.5f, yRotationMin = 0f, yRotationMax = 360f }
    };

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float ySpawnPosition = 0f;
    [SerializeField] private bool useGroundHeight = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Optional Transform to parent spawned objects to
    [Header("Organization")]
    [SerializeField] private Transform spawnedObjectsParent;
    [SerializeField] private bool createParentIfMissing = true;

    private Camera mainCamera;
    private Dictionary<KeyCode, GameObject> keyToPrefabMap = new Dictionary<KeyCode, GameObject>();

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("KeyboardPrefabSpawner: No main camera found!");
        }

        // Initialize parent transform if needed
        if (spawnedObjectsParent == null && createParentIfMissing)
        {
            GameObject parentObj = new GameObject("SpawnedPrefabs");
            spawnedObjectsParent = parentObj.transform;
        }

        // Validate scale and rotation values
        for (int i = 0; i < keyPrefabPairs.Length; i++)
        {
            KeyPrefabPair pair = keyPrefabPairs[i];

            // Ensure min scale doesn't exceed max scale
            if (pair.scaleMin > pair.scaleMax)
            {
                pair.scaleMin = pair.scaleMax;
            }

            // Ensure min rotation doesn't exceed max rotation
            if (pair.yRotationMin > pair.yRotationMax)
            {
                pair.yRotationMin = pair.yRotationMax;
            }
        }
    }

    private void Update()
    {
        // Check for key presses in our key bindings array (to get the full settings)
        for (int i = 0; i < keyPrefabPairs.Length; i++)
        {
            KeyPrefabPair pair = keyPrefabPairs[i];

            // Skip if no prefab assigned or key is None
            if (pair.prefab == null || pair.key == KeyCode.None) continue;

            // Check if this key was pressed
            if (Input.GetKeyDown(pair.key))
            {
                Vector3 spawnPosition = GetMouseWorldPosition();

                if (spawnPosition != Vector3.zero)
                {
                    // Spawn with full settings
                    SpawnPrefab(pair.prefab, spawnPosition, pair);

                    if (showDebugInfo)
                    {
                        string prefabName = !string.IsNullOrEmpty(pair.prefabName) ? pair.prefabName : pair.prefab.name;
                        Debug.Log($"Spawned prefab {prefabName} at position {spawnPosition}");
                    }
                }
            }
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
                Debug.LogWarning("KeyboardPrefabSpawner: Raycast did not hit any ground!");
                return Vector3.zero;
            }
        }
        else
        {
            // Project to fixed Y coordinate
            // We need to convert from screen to world space, but constrain to the XZ plane

            // Create two points along the ray
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
                Debug.LogWarning("KeyboardPrefabSpawner: Could not project mouse position onto XZ plane!");
            }
        }

        return Vector3.zero;
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, KeyPrefabPair prefabSettings = null)
    {
        if (prefab == null) return;

        // Ensure Y position is correct if not using ground height
        if (!useGroundHeight)
        {
            position.y = ySpawnPosition;
        }

        // Default rotation (identity)
        Quaternion rotation = Quaternion.identity;

        // Default scale (no change)
        Vector3 scale = Vector3.one;

        // Apply randomization if settings are provided
        if (prefabSettings != null)
        {
            // Random Y rotation
            float yRotation = Random.Range(prefabSettings.yRotationMin, prefabSettings.yRotationMax);
            rotation = Quaternion.Euler(0f, yRotation, 0f);

            // Random uniform scale
            float randomScale = Random.Range(prefabSettings.scaleMin, prefabSettings.scaleMax);
            scale = new Vector3(randomScale, randomScale, randomScale);
        }

        // Instantiate the prefab at the calculated position with rotation
        GameObject instance = Instantiate(prefab, position, rotation);

        // Apply scale
        instance.transform.localScale = scale;

        // Parent the object if parent transform is set
        if (spawnedObjectsParent != null)
        {
            instance.transform.SetParent(spawnedObjectsParent);
        }

        if (showDebugInfo && prefabSettings != null)
        {
            Debug.Log($"Spawned prefab with scale: {scale.x}, Y rotation: {rotation.eulerAngles.y}");
        }
    }

    /// <summary>
    /// Programmatically spawn a prefab at the current mouse position
    /// </summary>
    /// <param name="prefabIndex">Index of the prefab in the keyPrefabPairs array</param>
    public void SpawnPrefabAtMouse(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= keyPrefabPairs.Length)
        {
            Debug.LogError($"KeyboardPrefabSpawner: Invalid prefab index: {prefabIndex}");
            return;
        }

        KeyPrefabPair pair = keyPrefabPairs[prefabIndex];
        if (pair.prefab == null)
        {
            Debug.LogWarning($"KeyboardPrefabSpawner: No prefab assigned at index {prefabIndex}");
            return;
        }

        Vector3 spawnPosition = GetMouseWorldPosition();
        if (spawnPosition != Vector3.zero)
        {
            SpawnPrefab(pair.prefab, spawnPosition, pair);
        }
    }

    /// <summary>
    /// Clears all spawned prefabs that are children of the spawnedObjectsParent
    /// </summary>
    public void ClearSpawnedPrefabs()
    {
        if (spawnedObjectsParent == null) return;

        // Destroy all children of the parent transform
        int childCount = spawnedObjectsParent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(spawnedObjectsParent.GetChild(i).gameObject);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(KeyboardPrefabSpawner))]
public class KeyboardPrefabSpawnerEditor : Editor
{
    private SerializedProperty keyPrefabPairsProp;
    private SerializedProperty groundLayerMaskProp;
    private SerializedProperty maxRaycastDistanceProp;
    private SerializedProperty ySpawnPositionProp;
    private SerializedProperty useGroundHeightProp;
    private SerializedProperty showDebugInfoProp;
    private SerializedProperty spawnedObjectsParentProp;
    private SerializedProperty createParentIfMissingProp;

    private void OnEnable()
    {
        keyPrefabPairsProp = serializedObject.FindProperty("keyPrefabPairs");
        groundLayerMaskProp = serializedObject.FindProperty("groundLayerMask");
        maxRaycastDistanceProp = serializedObject.FindProperty("maxRaycastDistance");
        ySpawnPositionProp = serializedObject.FindProperty("ySpawnPosition");
        useGroundHeightProp = serializedObject.FindProperty("useGroundHeight");
        showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
        spawnedObjectsParentProp = serializedObject.FindProperty("spawnedObjectsParent");
        createParentIfMissingProp = serializedObject.FindProperty("createParentIfMissing");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(keyPrefabPairsProp);

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
        EditorGUILayout.LabelField("Organization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnedObjectsParentProp);
        EditorGUILayout.PropertyField(createParentIfMissingProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(showDebugInfoProp);

        // Display key binding table
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Key Bindings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        for (int i = 0; i < keyPrefabPairsProp.arraySize; i++)
        {
            SerializedProperty pairProp = keyPrefabPairsProp.GetArrayElementAtIndex(i);
            SerializedProperty keyProp = pairProp.FindPropertyRelative("key");
            SerializedProperty prefabProp = pairProp.FindPropertyRelative("prefab");
            SerializedProperty nameProp = pairProp.FindPropertyRelative("prefabName");

            string prefabName = nameProp.stringValue;
            if (string.IsNullOrEmpty(prefabName) && prefabProp.objectReferenceValue != null)
            {
                prefabName = prefabProp.objectReferenceValue.name;
            }
            else if (string.IsNullOrEmpty(prefabName))
            {
                prefabName = "None";
            }

            SerializedProperty scaleMinProp = pairProp.FindPropertyRelative("scaleMin");
            SerializedProperty scaleMaxProp = pairProp.FindPropertyRelative("scaleMax");
            SerializedProperty rotMinProp = pairProp.FindPropertyRelative("yRotationMin");
            SerializedProperty rotMaxProp = pairProp.FindPropertyRelative("yRotationMax");

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Key: {keyProp.enumDisplayNames[keyProp.enumValueIndex]}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Prefab: {prefabName}");
            EditorGUILayout.EndHorizontal();

            // Show scale and rotation info in a more compact form
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Scale: {scaleMinProp.floatValue:F1} - {scaleMaxProp.floatValue:F1}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Y Rotation: {rotMinProp.floatValue:F0}° - {rotMaxProp.floatValue:F0}°");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        // Add test buttons in play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear All Spawned Objects"))
            {
                KeyboardPrefabSpawner spawner = (KeyboardPrefabSpawner)target;
                spawner.ClearSpawnedPrefabs();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif