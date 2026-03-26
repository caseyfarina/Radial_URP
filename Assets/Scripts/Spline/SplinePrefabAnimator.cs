using UnityEngine;
using UnityEngine.Splines;
using System.Collections;
using System.Collections.Generic;
using SplineTools; // Add namespace to match CircularSplineGenerator
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SplineContainer))]
public class SplinePrefabAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 2f;

    [Header("Rotation Settings")]
    [SerializeField, Range(0f, 360f)] private float minRotationDistance = 90f;
    [SerializeField, Range(0f, 360f)] private float maxRotationDistance = 180f;
    [SerializeField] private AnimationDirection rotationDirection = AnimationDirection.Clockwise;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Enum for rotation direction selection
    public enum AnimationDirection
    {
        Clockwise,
        CounterClockwise,
        Random
    }

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private CircularSplineGenerator splineGenerator;
    private SplineContainer splineContainer;
    private bool isAnimating;
    private List<(Transform transform, float startT)> prefabData;
    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // Re-initialize if previously disabled
        Initialize();
    }

    private void Initialize()
    {
        splineContainer = GetComponent<SplineContainer>();
        splineGenerator = GetComponent<CircularSplineGenerator>();

        if (splineContainer == null)
        {
            Debug.LogError("[SplinePrefabAnimator] Missing required SplineContainer component!", this);
            return;
        }

        if (splineGenerator == null)
        {
            Debug.LogError("[SplinePrefabAnimator] Missing required CircularSplineGenerator component!", this);
            return;
        }

        // Make sure we have valid data
        if (splineContainer.Spline == null || splineContainer.Spline.Count == 0)
        {
            if (showDebugInfo) Debug.Log("[SplinePrefabAnimator] Spline is empty or null. Requesting generation...", this);
            splineGenerator.GenerateCircularSpline();

            // Double-check it was generated properly
            if (splineContainer.Spline == null || splineContainer.Spline.Count == 0)
            {
                Debug.LogError("[SplinePrefabAnimator] Failed to generate spline!", this);
                return;
            }
        }

        prefabData = new List<(Transform, float)>();
        isInitialized = true;

        if (showDebugInfo) Debug.Log("[SplinePrefabAnimator] Successfully initialized", this);
    }

    public void StartAnimation()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[SplinePrefabAnimator] Not properly initialized. Attempting to initialize now...", this);
            Initialize();

            if (!isInitialized)
            {
                Debug.LogError("[SplinePrefabAnimator] Initialization failed, cannot start animation!", this);
                return;
            }
        }

        if (isAnimating)
        {
            Debug.LogWarning("[SplinePrefabAnimator] Animation already in progress!", this);
            return;
        }

        // Verify spline is valid
        if (splineContainer.Spline == null || splineContainer.Spline.Count == 0)
        {
            Debug.LogError("[SplinePrefabAnimator] Spline is invalid or empty!", this);
            return;
        }

        // Verify we have child objects to animate
        if (transform.childCount == 0)
        {
            Debug.LogWarning("[SplinePrefabAnimator] No child objects found to animate!", this);
            return;
        }

        // Collect all child transforms and their starting positions
        CollectPrefabData();

        if (prefabData.Count > 0)
        {
            if (showDebugInfo) Debug.Log($"[SplinePrefabAnimator] Starting animation with {prefabData.Count} prefabs", this);
            StartCoroutine(AnimatePrefabs());
        }
        else
        {
            Debug.LogWarning("[SplinePrefabAnimator] No prefabs found to animate!", this);
        }
    }

    private void CollectPrefabData()
    {
        prefabData.Clear();

        // Get all immediate children (skip nested children)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Skip inactive children
            if (!child.gameObject.activeInHierarchy)
                continue;

            // Calculate current position on spline (0-1)
            float currentT = CalculateSplinePosition(child.position);
            prefabData.Add((child, currentT));

            if (showDebugInfo) Debug.Log($"[SplinePrefabAnimator] Added prefab {child.name} at position T={currentT:F3}", this);
        }
    }

    private float CalculateSplinePosition(Vector3 worldPosition)
    {
        // Convert world position to local
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        // Number of samples to check along the spline
        const int samples = 100;
        float closestT = 0f;
        float closestDistance = float.MaxValue;

        // Sample points along the spline to find the closest one
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 point = splineContainer.Spline.EvaluatePosition(t);
            float distance = Vector3.Distance(point, localPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        // Fine-tune the result
        float stepSize = 1f / samples;
        float startT = Mathf.Max(0f, closestT - stepSize);
        float endT = Mathf.Min(1f, closestT + stepSize);

        for (float t = startT; t <= endT; t += stepSize / 10f)
        {
            Vector3 point = splineContainer.Spline.EvaluatePosition(t);
            float distance = Vector3.Distance(point, localPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        return closestT;
    }

    private IEnumerator AnimatePrefabs()
    {
        isAnimating = true;
        float elapsedTime = 0f;

        // Determine rotation distance for this animation
        float rotationDistance = Random.Range(minRotationDistance, maxRotationDistance);

        // Determine rotation direction for this animation
        float directionMultiplier = 1f;
        switch (rotationDirection)
        {
            case AnimationDirection.Clockwise:
                directionMultiplier = 1f;
                break;
            case AnimationDirection.CounterClockwise:
                directionMultiplier = -1f;
                break;
            case AnimationDirection.Random:
                directionMultiplier = (Random.value > 0.5f) ? 1f : -1f;
                break;
        }

        float rotationRadians = rotationDistance * Mathf.Deg2Rad * directionMultiplier;

        if (showDebugInfo)
        {
            string directionText = directionMultiplier > 0 ? "clockwise" : "counter-clockwise";
            Debug.Log($"[SplinePrefabAnimator] Starting animation: {rotationDistance:F1}° {directionText}", this);
        }

        // Cache transform for better performance
        Transform cachedTransform = transform;

        while (elapsedTime < animationDuration)
        {
            float normalizedTime = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(normalizedTime);

            foreach (var (prefabTransform, startT) in prefabData)
            {
                // Skip if transform was destroyed during animation
                if (prefabTransform == null) continue;

                // Calculate new position on spline
                float newT = startT + (curveValue * rotationRadians / (2f * Mathf.PI));

                // Keep within 0-1 range
                newT = Mathf.Repeat(newT, 1f);

                try
                {
                    // Get new position and direction from spline
                    Vector3 localPosition = splineContainer.Spline.EvaluatePosition(newT);
                    Vector3 localDirection = splineContainer.Spline.EvaluateTangent(newT);

                    // Transform to world space
                    Vector3 worldPosition = cachedTransform.TransformPoint(localPosition);
                    Vector3 worldDirection = cachedTransform.TransformDirection(localDirection);

                    // Update transform
                    prefabTransform.position = worldPosition;

                    // Update rotation if the prefab should face the movement direction
                    if (worldDirection != Vector3.zero)
                    {
                        prefabTransform.rotation = Quaternion.LookRotation(worldDirection);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SplinePrefabAnimator] Error during animation: {ex.Message}", this);
                    isAnimating = false;
                    yield break;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is exact
        float finalCurveValue = animationCurve.Evaluate(1f);

        try
        {
            foreach (var (prefabTransform, startT) in prefabData)
            {
                // Skip if transform was destroyed during animation
                if (prefabTransform == null) continue;

                // The modulo arithmetic with Mathf.Repeat ensures we stay within the 0-1 range
                // even with negative rotation (counter-clockwise)
                float finalT = Mathf.Repeat(startT + (finalCurveValue * rotationRadians / (2f * Mathf.PI)), 1f);
                Vector3 finalLocalPosition = splineContainer.Spline.EvaluatePosition(finalT);
                Vector3 finalLocalDirection = splineContainer.Spline.EvaluateTangent(finalT);

                Vector3 finalWorldPosition = cachedTransform.TransformPoint(finalLocalPosition);
                Vector3 finalWorldDirection = cachedTransform.TransformDirection(finalLocalDirection);

                prefabTransform.position = finalWorldPosition;
                if (finalWorldDirection != Vector3.zero)
                {
                    prefabTransform.rotation = Quaternion.LookRotation(finalWorldDirection);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SplinePrefabAnimator] Error finalizing animation: {ex.Message}", this);
        }

        if (showDebugInfo) Debug.Log("[SplinePrefabAnimator] Animation completed", this);
        isAnimating = false;
    }

    // Allow public methods to request spline refresh
    public void RefreshSpline()
    {
        if (splineGenerator != null)
        {
            splineGenerator.GenerateCircularSpline();
        }
    }

    public bool IsAnimating => isAnimating;
    public bool IsInitialized => isInitialized;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplinePrefabAnimator))]
public class SplinePrefabAnimatorEditor : Editor
{
    private SerializedProperty animationDurationProp;
    private SerializedProperty minRotationDistanceProp;
    private SerializedProperty maxRotationDistanceProp;
    private SerializedProperty rotationDirectionProp;
    private SerializedProperty animationCurveProp;
    private SerializedProperty showDebugInfoProp;

    private void OnEnable()
    {
        animationDurationProp = serializedObject.FindProperty("animationDuration");
        minRotationDistanceProp = serializedObject.FindProperty("minRotationDistance");
        maxRotationDistanceProp = serializedObject.FindProperty("maxRotationDistance");
        rotationDirectionProp = serializedObject.FindProperty("rotationDirection");
        animationCurveProp = serializedObject.FindProperty("animationCurve");
        showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(animationDurationProp, new GUIContent("Animation Duration (s)"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);

        // Show min and max rotation fields side by side
        EditorGUILayout.BeginHorizontal();

        float minVal = minRotationDistanceProp.floatValue;
        float maxVal = maxRotationDistanceProp.floatValue;

        // Use a custom layout to ensure min <= max
        EditorGUILayout.LabelField("Rotation Range (°):", GUILayout.Width(120));

        minVal = EditorGUILayout.Slider(minVal, 0f, 360f, GUILayout.MinWidth(50));
        EditorGUILayout.LabelField("to", GUILayout.Width(20));
        maxVal = EditorGUILayout.Slider(maxVal, 0f, 360f, GUILayout.MinWidth(50));

        // Ensure min doesn't exceed max
        if (minVal > maxVal) minVal = maxVal;

        minRotationDistanceProp.floatValue = minVal;
        maxRotationDistanceProp.floatValue = maxVal;

        EditorGUILayout.EndHorizontal();

        // Show enum field for direction selection
        EditorGUILayout.PropertyField(rotationDirectionProp, new GUIContent("Rotation Direction"));

        EditorGUILayout.PropertyField(animationCurveProp, new GUIContent("Animation Curve"));

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(showDebugInfoProp, new GUIContent("Show Debug Info"));

        EditorGUILayout.Space(10);

        SplinePrefabAnimator animator = (SplinePrefabAnimator)target;

        // Check for missing dependencies
        bool hasSplineContainer = animator.GetComponent<SplineContainer>() != null;
        bool hasCircularGenerator = animator.GetComponent<CircularSplineGenerator>() != null;

        if (!hasSplineContainer || !hasCircularGenerator)
        {
            string missingComponents = !hasSplineContainer ? "SplineContainer" : "";
            missingComponents += !hasCircularGenerator ? (missingComponents.Length > 0 ? ", CircularSplineGenerator" : "CircularSplineGenerator") : "";

            EditorGUILayout.HelpBox($"Missing required components: {missingComponents}", MessageType.Error);
        }

        // Check for child objects
        if (animator.transform.childCount == 0 && Application.isPlaying)
        {
            EditorGUILayout.HelpBox("No child objects found to animate! Use CircularSplineGenerator to distribute prefabs first.", MessageType.Warning);
        }

        // Add a button to refresh the spline
        if (GUILayout.Button("Refresh Spline"))
        {
            CircularSplineGenerator generator = animator.GetComponent<CircularSplineGenerator>();
            if (generator != null)
            {
                generator.UpdateSpline(); // This will regenerate the spline
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space(5);

        using (new EditorGUI.DisabledGroupScope(!Application.isPlaying || animator.IsAnimating))
        {
            if (GUILayout.Button("Animate Prefabs", GUILayout.Height(30)))
            {
                animator.StartAnimation();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test animation.", MessageType.Info);
            }
            else if (animator.IsAnimating)
            {
                EditorGUILayout.HelpBox("Animation in progress...", MessageType.Info);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif 