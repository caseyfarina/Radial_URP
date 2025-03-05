using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class CurvedConnectionDrawer : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask targetLayers = ~0; // Default to all layers
    [SerializeField] private string targetTag = "Entity"; // Default tag to connect to
    [SerializeField] private int maxConnections = 10;
    [SerializeField] private float refreshRate = 0.5f; // Seconds between detection refreshes

    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial; // Material for line renderers
    [SerializeField, Range(0.01f, 1f)] private float lineWidth = 0.1f;
    [SerializeField, Range(0f, 10f)] private float curvatureAmount = 1.0f;
    [SerializeField, Range(8, 24)] private int lineSegments = 12;
    [SerializeField] private Color lineColor = Color.white;

    [Header("Line Animation")]
    [SerializeField] private bool enableEmissionBurst = true;
    [SerializeField] private AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float emissionDuration = 1f;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float emissionIntensity = 1f;

    [Header("Sequential Connection")]
    [SerializeField] private bool enableSequentialConnections = true;
    [SerializeField, Range(0f, 2f)] private float timeBetweenConnections = 0.2f;
    [SerializeField] private bool randomizeConnectionOrder = false;

    [Header("Line Trimming")]
    [SerializeField, Range(0f, 1f)] private float sourceTrimPercentage = 0.1f; // Percentage to trim from source end
    [SerializeField, Range(0f, 1f)] private float targetTrimPercentage = 0.1f; // Percentage to trim from target end
    [SerializeField] private bool useFixedDistance = false; // Use fixed distance instead of percentage
    [SerializeField, Range(0f, 2f)] private float sourceTrimDistance = 0.5f; // Fixed distance to trim from source
    [SerializeField, Range(0f, 2f)] private float targetTrimDistance = 0.5f; // Fixed distance to trim from target

    [Header("Endpoint Prefabs")]
    [SerializeField] private GameObject sourceEndpointPrefab; // Prefab to instantiate at source endpoint
    [SerializeField] private GameObject targetEndpointPrefab; // Prefab to instantiate at target endpoint
    [SerializeField] private bool lookAtDirection = true; // Orient prefabs to face along the curve direction

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onConnectionEstablished = new UnityEvent<GameObject>();
    [SerializeField] private UnityEvent<GameObject> onConnectionBroken = new UnityEvent<GameObject>();

    // Runtime variables
    private LineRenderer[] lineRenderers;
    private Transform[] targetTransforms;
    private int activeLineCount;
    private Transform cachedTransform;
    private float nextRefreshTime;

    // Sequential connection variables
    private Queue<GameObject> pendingConnections = new Queue<GameObject>();
    private HashSet<GameObject> activeConnections = new HashSet<GameObject>();
    private HashSet<GameObject> pendingSet = new HashSet<GameObject>(); // For quick lookup

    // Sequential removal variables
    private Queue<GameObject> pendingRemovals = new Queue<GameObject>();
    private HashSet<GameObject> pendingRemovalSet = new HashSet<GameObject>(); // For quick lookup

    private float nextSequentialTime;

    // Endpoint prefab instances
    private GameObject[] sourceEndpointInstances;
    private GameObject[] targetEndpointInstances;
    private Vector3[] sourceEndpointPositions;
    private Vector3[] targetEndpointPositions;
    private Vector3[] sourceEndpointDirections;
    private Vector3[] targetEndpointDirections;

    // Animation tracking
    private float[] lineCreationTimes;
    private bool[] isNewLine;
    private Material[] lineMaterialInstances;

    // Job system variables
    private NativeArray<float3> linePoints;
    private NativeArray<float3> targetPositions;
    private NativeArray<float3> sourcePosition;
    private NativeArray<float3> curveDirections; // Store consistent curve directions
    private JobHandle curveJobHandle;
    private bool jobsActive = false;

    private void Awake()
    {
        cachedTransform = transform;
        InitializeLineRenderers();
        InitializeJobsData();
        nextSequentialTime = Time.time;
    }

    private void OnEnable()
    {
        // Re-initialize Native Arrays if they were disposed
        if (!linePoints.IsCreated)
        {
            InitializeJobsData();
        }
    }

    private void Start()
    {
        RefreshTargets();
        nextRefreshTime = Time.time + refreshRate;
    }

    private void Update()
    {
        // Check if it's time to refresh targets
        if (Time.time >= nextRefreshTime)
        {
            if (jobsActive)
            {
                // Ensure previous jobs are complete before refreshing
                curveJobHandle.Complete();
                jobsActive = false;
            }

            RefreshTargets();
            nextRefreshTime = Time.time + refreshRate;
        }

        // Process pending sequential actions (connections and removals)
        if (enableSequentialConnections && Time.time >= nextSequentialTime)
        {
            bool didProcess = false;

            // Prioritize removals to avoid exceeding maxConnections
            if (pendingRemovals.Count > 0)
            {
                ProcessNextPendingRemoval();
                didProcess = true;
            }
            // Then process connections if we have space
            else if (pendingConnections.Count > 0 && activeLineCount < maxConnections)
            {
                ProcessNextPendingConnection();
                didProcess = true;
            }

            if (didProcess)
            {
                nextSequentialTime = Time.time + timeBetweenConnections;
            }
        }

        // Update line positions using Jobs
        UpdateLinePositionsWithJobs();

        // Update emission animations if enabled
        if (enableEmissionBurst)
        {
            UpdateEmissionAnimations();
        }
    }

    private void ProcessNextPendingConnection()
    {
        if (pendingConnections.Count == 0 || activeLineCount >= maxConnections)
            return;

        // Get the next target from the queue
        GameObject targetObj = pendingConnections.Dequeue();
        pendingSet.Remove(targetObj);

        // Skip if the target is no longer valid
        if (targetObj == null ||
            !IsTargetInRange(targetObj) ||
            !IsTargetValid(targetObj))
        {
            // Try the next one if available
            if (pendingConnections.Count > 0)
            {
                ProcessNextPendingConnection();
            }
            return;
        }

        // Add to active connections
        EstablishConnection(targetObj);
    }

    private bool IsTargetInRange(GameObject targetObj)
    {
        // Check if target is still within connection range
        return Vector3.Distance(cachedTransform.position, targetObj.transform.position) <= detectionRadius;
    }

    private bool IsTargetValid(GameObject targetObj)
    {
        // Check if the object has the required tag
        if (!string.IsNullOrEmpty(targetTag) && !targetObj.CompareTag(targetTag))
            return false;

        return true;
    }

    private void EstablishConnection(GameObject targetObj)
    {
        // Store target transform reference
        targetTransforms[activeLineCount] = targetObj.transform;
        lineRenderers[activeLineCount].enabled = true;
        activeConnections.Add(targetObj);

        // Calculate and store a consistent curve direction for this connection
        CalculateCurveDirection(activeLineCount, targetObj.transform.position);

        // Mark as new line for animation
        if (enableEmissionBurst)
        {
            isNewLine[activeLineCount] = true;
            lineCreationTimes[activeLineCount] = Time.time;
        }

        // Trigger the connection established event
        onConnectionEstablished.Invoke(targetObj);

        activeLineCount++;
    }

    private void RefreshTargets()
    {
        // Create a set of current valid targets
        HashSet<GameObject> validTargets = new HashSet<GameObject>();

        // Find all colliders within radius
        Collider[] colliders = Physics.OverlapSphere(cachedTransform.position, detectionRadius, targetLayers);

        // Filter valid targets
        foreach (Collider collider in colliders)
        {
            GameObject targetObj = collider.gameObject;

            // Skip self
            if (targetObj == gameObject) continue;

            // Check if the object has the required tag
            if (!string.IsNullOrEmpty(targetTag) && !targetObj.CompareTag(targetTag)) continue;

            validTargets.Add(targetObj);
        }

        // Check for broken connections (previously active, but no longer valid)
        List<GameObject> brokenConnections = new List<GameObject>();
        foreach (GameObject activeTarget in activeConnections)
        {
            if (activeTarget == null || !validTargets.Contains(activeTarget))
            {
                brokenConnections.Add(activeTarget);
            }
        }

        // Handle broken connections
        foreach (GameObject broken in brokenConnections)
        {
            // Skip if already pending removal
            if (pendingRemovalSet.Contains(broken))
                continue;

            if (enableSequentialConnections)
            {
                // Add to pending removals queue for sequential processing
                pendingRemovals.Enqueue(broken);
                pendingRemovalSet.Add(broken);
            }
            else
            {
                // Remove immediately if sequential mode is disabled
                RemoveConnection(broken);
            }
        }

        // Find valid new targets (not already connected or pending)
        List<GameObject> newTargets = new List<GameObject>();
        foreach (GameObject target in validTargets)
        {
            if (!activeConnections.Contains(target) && !pendingSet.Contains(target))
            {
                newTargets.Add(target);
            }
        }

        // If sequential connections are enabled, add new targets to the pending queue
        if (enableSequentialConnections)
        {
            // Randomize the order if needed
            if (randomizeConnectionOrder)
            {
                newTargets = newTargets.OrderBy(x => UnityEngine.Random.value).ToList();
            }

            foreach (GameObject target in newTargets)
            {
                if (!pendingSet.Contains(target))
                {
                    pendingConnections.Enqueue(target);
                    pendingSet.Add(target);
                }
            }
        }
        else
        {
            // If sequential connections are disabled, create connections immediately
            foreach (GameObject target in newTargets)
            {
                if (activeLineCount < maxConnections)
                {
                    EstablishConnection(target);
                }
                else
                {
                    break; // Stop if we've reached max connections
                }
            }
        }
    }

    private void InitializeLineRenderers()
    {
        // Pre-allocate arrays for better performance
        lineRenderers = new LineRenderer[maxConnections];
        targetTransforms = new Transform[maxConnections];
        sourceEndpointInstances = new GameObject[maxConnections];
        targetEndpointInstances = new GameObject[maxConnections];
        sourceEndpointPositions = new Vector3[maxConnections];
        targetEndpointPositions = new Vector3[maxConnections];
        sourceEndpointDirections = new Vector3[maxConnections];
        targetEndpointDirections = new Vector3[maxConnections];
        lineCreationTimes = new float[maxConnections];
        isNewLine = new bool[maxConnections];
        lineMaterialInstances = new Material[maxConnections];

        // Create a default material if none is assigned
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = lineColor;
        }

        // Ensure the material supports emission if needed
        if (enableEmissionBurst && !lineMaterial.HasProperty("_EmissionColor"))
        {
            Debug.LogWarning("Line material does not support emission. Using a default material with emission.");
            lineMaterial = new Material(Shader.Find("Standard"));
            lineMaterial.EnableKeyword("_EMISSION");
            lineMaterial.color = lineColor;
        }

        // Create LineRenderer components
        for (int i = 0; i < maxConnections; i++)
        {
            GameObject lineObj = new GameObject($"ConnectionLine_{i}");
            lineObj.transform.SetParent(transform, false);

            LineRenderer line = lineObj.AddComponent<LineRenderer>();

            // Create a unique material instance for each line if emission is enabled
            if (enableEmissionBurst)
            {
                lineMaterialInstances[i] = new Material(lineMaterial);
                lineMaterialInstances[i].color = lineColor;
                if (lineMaterialInstances[i].HasProperty("_EmissionColor"))
                {
                    lineMaterialInstances[i].EnableKeyword("_EMISSION");
                    lineMaterialInstances[i].SetColor("_EmissionColor", Color.black); // Start with no emission
                }
                line.material = lineMaterialInstances[i];
            }
            else
            {
                line.material = lineMaterial;
            }

            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = lineSegments;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.useWorldSpace = true;
            line.enabled = false; // Start disabled

            lineRenderers[i] = line;
            lineCreationTimes[i] = 0f;
            isNewLine[i] = false;
        }
    }

    private void UpdateEmissionAnimations()
    {
        // Skip if emission burst is disabled
        if (!enableEmissionBurst) return;

        for (int i = 0; i < activeLineCount; i++)
        {
            // Skip lines that aren't new
            if (!isNewLine[i]) continue;

            // Calculate animation progress
            float elapsed = Time.time - lineCreationTimes[i];
            if (elapsed < emissionDuration)
            {
                float t = elapsed / emissionDuration;
                float intensity = emissionCurve.Evaluate(t) * emissionIntensity;

                // Apply emission
                if (lineMaterialInstances[i] != null && lineMaterialInstances[i].HasProperty("_EmissionColor"))
                {
                    Color emissionValue = emissionColor * intensity;
                    lineMaterialInstances[i].SetColor("_EmissionColor", emissionValue);
                }
            }
            else
            {
                // Animation complete
                isNewLine[i] = false;

                // Reset emission to zero
                if (lineMaterialInstances[i] != null && lineMaterialInstances[i].HasProperty("_EmissionColor"))
                {
                    lineMaterialInstances[i].SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    private void InitializeJobsData()
    {
        // Native arrays for job system
        linePoints = new NativeArray<float3>(lineSegments * maxConnections, Allocator.Persistent);
        targetPositions = new NativeArray<float3>(maxConnections, Allocator.Persistent);
        sourcePosition = new NativeArray<float3>(1, Allocator.Persistent);
        curveDirections = new NativeArray<float3>(maxConnections, Allocator.Persistent);

        // Initialize curve directions with default values
        for (int i = 0; i < maxConnections; i++)
        {
            curveDirections[i] = new float3(0, 1, 0); // Default up direction
        }
    }

    private void CalculateCurveDirection(int index, Vector3 targetPosition)
    {
        // Get direction from source to target
        Vector3 direction = targetPosition - cachedTransform.position;
        Vector3 directionNormalized = direction.normalized;

        // Create a consistent perpendicular vector for this connection
        // We'll use a combination of cross products to ensure we get a stable, non-zero result
        Vector3 perpendicular;

        // Try crossing with up vector first
        perpendicular = Vector3.Cross(directionNormalized, Vector3.up);

        // If the result is too small (nearly parallel to up), use forward instead
        if (perpendicular.sqrMagnitude < 0.01f)
        {
            perpendicular = Vector3.Cross(directionNormalized, Vector3.forward);
        }

        // If still too small, use right
        if (perpendicular.sqrMagnitude < 0.01f)
        {
            perpendicular = Vector3.Cross(directionNormalized, Vector3.right);
        }

        // Normalize and store the consistent direction
        perpendicular = perpendicular.normalized;

        // Add a bit of deterministic randomization based on targetPosition hash
        // This ensures different targets have different curve directions
        int targetHash = targetPosition.GetHashCode();
        float randomSign = ((targetHash % 2) == 0) ? 1.0f : -1.0f;

        // Save the final curve direction
        curveDirections[index] = (float3)(perpendicular * randomSign);
    }

    private void UpdateLinePositionsWithJobs()
    {
        if (activeLineCount <= 0) return;

        // If a job is already running, wait for it to complete
        if (jobsActive)
        {
            curveJobHandle.Complete();
            jobsActive = false;

            // Apply results from previous job
            ApplyJobResults();
        }

        // Fill source position
        sourcePosition[0] = cachedTransform.position;

        // Fill target positions array for active connections
        for (int i = 0; i < activeLineCount; i++)
        {
            if (targetTransforms[i] != null)
            {
                targetPositions[i] = targetTransforms[i].position;
            }
            else
            {
                targetPositions[i] = sourcePosition[0]; // Fallback
                lineRenderers[i].enabled = false;
            }
        }

        // Schedule job to calculate curve points
        CalculateCurvePointsJob curveJob = new CalculateCurvePointsJob
        {
            SourcePosition = sourcePosition,
            TargetPositions = targetPositions,
            CurveDirections = curveDirections,
            LinePoints = linePoints,
            CurvatureAmount = curvatureAmount,
            LineSegments = lineSegments,
            ActiveCount = activeLineCount,
            SourceTrimPercentage = sourceTrimPercentage,
            TargetTrimPercentage = targetTrimPercentage,
            UseFixedDistance = useFixedDistance ? 1 : 0,
            SourceTrimDistance = sourceTrimDistance,
            TargetTrimDistance = targetTrimDistance
        };

        curveJobHandle = curveJob.Schedule();
        jobsActive = true;

        // Force immediate execution for simplicity
        // In a more complex system, you could wait until LateUpdate to complete
        curveJobHandle.Complete();
        jobsActive = false;

        // Apply results immediately
        ApplyJobResults();
    }

    private void ApplyJobResults()
    {
        // Copy calculated line points back to LineRenderer components
        for (int i = 0; i < activeLineCount; i++)
        {
            if (lineRenderers[i].enabled)
            {
                for (int j = 0; j < lineSegments; j++)
                {
                    int index = i * lineSegments + j;
                    lineRenderers[i].SetPosition(j, linePoints[index]);
                }

                // Store the endpoints for prefab positioning
                sourceEndpointPositions[i] = linePoints[i * lineSegments]; // First point
                targetEndpointPositions[i] = linePoints[i * lineSegments + lineSegments - 1]; // Last point

                // Calculate directions at endpoints (for orientation)
                if (lineSegments > 1)
                {
                    // Source direction (from first point toward second point)
                    sourceEndpointDirections[i] = (Vector3)math.normalize(linePoints[i * lineSegments + 1] - linePoints[i * lineSegments]);

                    // Target direction (from second-to-last point toward last point)
                    targetEndpointDirections[i] = (Vector3)math.normalize(linePoints[i * lineSegments + lineSegments - 1] -
                                                 linePoints[i * lineSegments + lineSegments - 2]);
                }

                // Update or create endpoint prefabs
                UpdateEndpointPrefabs(i);
            }
            else
            {
                // Destroy any endpoint prefabs for disabled lines
                DestroyEndpointPrefabs(i);
            }
        }

        // Clean up any endpoint prefabs for inactive lines
        for (int i = activeLineCount; i < maxConnections; i++)
        {
            DestroyEndpointPrefabs(i);
        }
    }

    private void UpdateEndpointPrefabs(int lineIndex)
    {
        // Update source endpoint
        if (sourceEndpointPrefab != null)
        {
            if (sourceEndpointInstances[lineIndex] == null)
            {
                // Create new instance if none exists
                sourceEndpointInstances[lineIndex] = Instantiate(sourceEndpointPrefab,
                                                                sourceEndpointPositions[lineIndex],
                                                                Quaternion.identity,
                                                                transform);
                sourceEndpointInstances[lineIndex].name = $"SourceEndpoint_{lineIndex}";
            }
            else
            {
                // Update existing instance
                sourceEndpointInstances[lineIndex].transform.position = sourceEndpointPositions[lineIndex];
            }

            // Update rotation to face along the curve if needed
            if (lookAtDirection && sourceEndpointDirections[lineIndex] != Vector3.zero)
            {
                sourceEndpointInstances[lineIndex].transform.rotation =
                    Quaternion.LookRotation(sourceEndpointDirections[lineIndex]);
            }
        }
        else if (sourceEndpointInstances[lineIndex] != null)
        {
            // Destroy if prefab reference was removed
            Destroy(sourceEndpointInstances[lineIndex]);
            sourceEndpointInstances[lineIndex] = null;
        }

        // Update target endpoint
        if (targetEndpointPrefab != null)
        {
            if (targetEndpointInstances[lineIndex] == null)
            {
                // Create new instance if none exists
                targetEndpointInstances[lineIndex] = Instantiate(targetEndpointPrefab,
                                                                targetEndpointPositions[lineIndex],
                                                                Quaternion.identity,
                                                                transform);
                targetEndpointInstances[lineIndex].name = $"TargetEndpoint_{lineIndex}";
            }
            else
            {
                // Update existing instance
                targetEndpointInstances[lineIndex].transform.position = targetEndpointPositions[lineIndex];
            }

            // Update rotation to face along the curve if needed
            if (lookAtDirection && targetEndpointDirections[lineIndex] != Vector3.zero)
            {
                targetEndpointInstances[lineIndex].transform.rotation =
                    Quaternion.LookRotation(targetEndpointDirections[lineIndex]);
            }
        }
        else if (targetEndpointInstances[lineIndex] != null)
        {
            // Destroy if prefab reference was removed
            Destroy(targetEndpointInstances[lineIndex]);
            targetEndpointInstances[lineIndex] = null;
        }
    }

    private void DestroyEndpointPrefabs(int lineIndex)
    {
        // Destroy source endpoint
        if (sourceEndpointInstances[lineIndex] != null)
        {
            Destroy(sourceEndpointInstances[lineIndex]);
            sourceEndpointInstances[lineIndex] = null;
        }

        // Destroy target endpoint
        if (targetEndpointInstances[lineIndex] != null)
        {
            Destroy(targetEndpointInstances[lineIndex]);
            targetEndpointInstances[lineIndex] = null;
        }
    }

    private void OnDisable()
    {
        // Complete any pending jobs before disabling
        if (jobsActive)
        {
            curveJobHandle.Complete();
            jobsActive = false;
        }
    }

    private void OnDestroy()
    {
        // Clean up native arrays when destroyed
        if (jobsActive)
        {
            curveJobHandle.Complete();
        }

        if (linePoints.IsCreated) linePoints.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (sourcePosition.IsCreated) sourcePosition.Dispose();
        if (curveDirections.IsCreated) curveDirections.Dispose();

        // Clean up all endpoint prefab instances
        for (int i = 0; i < maxConnections; i++)
        {
            DestroyEndpointPrefabs(i);
        }

        // Clean up material instances
        for (int i = 0; i < maxConnections; i++)
        {
            if (lineMaterialInstances[i] != null)
            {
                Destroy(lineMaterialInstances[i]);
                lineMaterialInstances[i] = null;
            }
        }
    }

    // Helper method to remove a connection
    private void RemoveConnection(GameObject targetObj)
    {
        // Find the index of this connection
        int index = -1;
        for (int i = 0; i < activeLineCount; i++)
        {
            if (targetTransforms[i] != null && targetTransforms[i].gameObject == targetObj)
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
        {
            // Remove this connection by shifting all connections after it
            for (int i = index; i < activeLineCount - 1; i++)
            {
                targetTransforms[i] = targetTransforms[i + 1];
                lineRenderers[i].enabled = lineRenderers[i + 1].enabled;
                isNewLine[i] = isNewLine[i + 1];
                lineCreationTimes[i] = lineCreationTimes[i + 1];
                curveDirections[i] = curveDirections[i + 1];

                // Shift endpoint data
                sourceEndpointPositions[i] = sourceEndpointPositions[i + 1];
                targetEndpointPositions[i] = targetEndpointPositions[i + 1];
                sourceEndpointDirections[i] = sourceEndpointDirections[i + 1];
                targetEndpointDirections[i] = targetEndpointDirections[i + 1];

                // Move endpoint prefabs
                DestroyEndpointPrefabs(i);
                sourceEndpointInstances[i] = sourceEndpointInstances[i + 1];
                targetEndpointInstances[i] = targetEndpointInstances[i + 1];
                sourceEndpointInstances[i + 1] = null;
                targetEndpointInstances[i + 1] = null;
            }

            // Clear the last connection
            int lastIndex = activeLineCount - 1;
            targetTransforms[lastIndex] = null;
            lineRenderers[lastIndex].enabled = false;
            isNewLine[lastIndex] = false;
            DestroyEndpointPrefabs(lastIndex);

            // Decrement active count
            activeLineCount--;
        }

        // Remove from active set and trigger event
        activeConnections.Remove(targetObj);
        onConnectionBroken.Invoke(targetObj);
    }

    // Process the next pending removal
    private void ProcessNextPendingRemoval()
    {
        if (pendingRemovals.Count == 0)
            return;

        // Get the next target from the queue
        GameObject targetObj = pendingRemovals.Dequeue();
        pendingRemovalSet.Remove(targetObj);

        // Skip if this target is no longer relevant (already removed or null)
        if (targetObj == null || !activeConnections.Contains(targetObj))
            return;

        // Remove the connection
        RemoveConnection(targetObj);
    }

    // Visualization for debugging
    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (Application.isPlaying && enableSequentialConnections)
        {
            // Show pending connections
            if (pendingConnections.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.5f); // Orange

                foreach (GameObject target in pendingConnections)
                {
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.transform.position);
                    }
                }
            }

            // Show pending removals
            if (pendingRemovals.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f); // Red

                foreach (GameObject target in pendingRemovals)
                {
                    if (target != null && activeConnections.Contains(target))
                    {
                        Gizmos.DrawLine(transform.position, target.transform.position);
                    }
                }
            }
        }
    }

    // Public method to manually refresh targets
    public void ForceRefresh()
    {
        if (jobsActive)
        {
            curveJobHandle.Complete();
            jobsActive = false;
        }

        RefreshTargets();
        UpdateLinePositionsWithJobs();
    }

    // Toggle sequential connections
    public void SetSequentialConnections(bool enable)
    {
        if (enableSequentialConnections == enable) return;

        enableSequentialConnections = enable;

        // If we're disabling sequential connections, process all pending actions immediately
        if (!enableSequentialConnections)
        {
            // Process all pending removals first
            while (pendingRemovals.Count > 0)
            {
                GameObject targetObj = pendingRemovals.Dequeue();
                pendingRemovalSet.Remove(targetObj);

                if (targetObj != null && activeConnections.Contains(targetObj))
                {
                    RemoveConnection(targetObj);
                }
            }

            // Then process pending connections
            while (pendingConnections.Count > 0 && activeLineCount < maxConnections)
            {
                ProcessNextPendingConnection();
            }

            pendingConnections.Clear();
            pendingSet.Clear();
        }
    }

    // Set the time between connections
    public void SetTimeBetweenConnections(float time)
    {
        timeBetweenConnections = Mathf.Max(0f, time);
    }

    // Set whether to randomize connection order
    public void SetRandomizeConnectionOrder(bool randomize)
    {
        randomizeConnectionOrder = randomize;

        // If we're turning on randomization, shuffle the pending queue
        if (randomizeConnectionOrder && pendingConnections.Count > 0)
        {
            List<GameObject> shuffled = pendingConnections.ToList().OrderBy(x => UnityEngine.Random.value).ToList();
            pendingConnections.Clear();

            foreach (GameObject obj in shuffled)
            {
                pendingConnections.Enqueue(obj);
            }
        }
    }

    // Allow runtime adjustment of parameters
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
        ForceRefresh();
    }

    // Set target tag at runtime
    public void SetTargetTag(string tag)
    {
        targetTag = tag;
        ForceRefresh();
    }

    // Set endpoint prefabs at runtime
    public void SetSourceEndpointPrefab(GameObject prefab)
    {
        bool prefabChanged = sourceEndpointPrefab != prefab;
        sourceEndpointPrefab = prefab;

        if (prefabChanged)
        {
            // Clean up old instances
            for (int i = 0; i < maxConnections; i++)
            {
                if (sourceEndpointInstances[i] != null)
                {
                    Destroy(sourceEndpointInstances[i]);
                    sourceEndpointInstances[i] = null;
                }
            }

            // Force update
            if (!jobsActive)
            {
                UpdateLinePositionsWithJobs();
            }
        }
    }

    public void SetTargetEndpointPrefab(GameObject prefab)
    {
        bool prefabChanged = targetEndpointPrefab != prefab;
        targetEndpointPrefab = prefab;

        if (prefabChanged)
        {
            // Clean up old instances
            for (int i = 0; i < maxConnections; i++)
            {
                if (targetEndpointInstances[i] != null)
                {
                    Destroy(targetEndpointInstances[i]);
                    targetEndpointInstances[i] = null;
                }
            }

            // Force update
            if (!jobsActive)
            {
                UpdateLinePositionsWithJobs();
            }
        }
    }

    public void SetLookAtDirection(bool lookAt)
    {
        lookAtDirection = lookAt;
        if (!jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetLineWidth(float width)
    {
        lineWidth = Mathf.Clamp(width, 0.01f, 1f);
        for (int i = 0; i < maxConnections; i++)
        {
            lineRenderers[i].startWidth = lineWidth;
            lineRenderers[i].endWidth = lineWidth;
        }
    }

    public void SetLineColor(Color color)
    {
        lineColor = color;
        for (int i = 0; i < maxConnections; i++)
        {
            if (lineRenderers[i] != null)
            {
                lineRenderers[i].startColor = color;
                lineRenderers[i].endColor = color;
            }
        }
    }

    public void SetCurvature(float curvature)
    {
        curvatureAmount = Mathf.Clamp(curvature, 0f, 10f);
        if (!jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetSourceTrimPercentage(float percentage)
    {
        sourceTrimPercentage = Mathf.Clamp01(percentage);
        if (!jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetTargetTrimPercentage(float percentage)
    {
        targetTrimPercentage = Mathf.Clamp01(percentage);
        if (!jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetUseFixedDistance(bool useFixed)
    {
        useFixedDistance = useFixed;
        if (!jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetSourceTrimDistance(float distance)
    {
        sourceTrimDistance = Mathf.Max(0f, distance);
        if (useFixedDistance && !jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetTargetTrimDistance(float distance)
    {
        targetTrimDistance = Mathf.Max(0f, distance);
        if (useFixedDistance && !jobsActive)
        {
            UpdateLinePositionsWithJobs();
        }
    }

    public void SetEmissionSettings(float intensity, float duration)
    {
        emissionIntensity = Mathf.Max(0f, intensity);
        emissionDuration = Mathf.Max(0.1f, duration);
    }

    public void SetEmissionColor(Color color)
    {
        emissionColor = color;
    }

    public void SetEmissionCurve(AnimationCurve curve)
    {
        if (curve != null && curve.length >= 2)
        {
            emissionCurve = curve;
        }
    }

    public void SetEnableEmissionBurst(bool enable)
    {
        if (enableEmissionBurst != enable)
        {
            enableEmissionBurst = enable;

            // Reset all emission colors if disabled
            if (!enableEmissionBurst)
            {
                for (int i = 0; i < maxConnections; i++)
                {
                    if (lineMaterialInstances[i] != null && lineMaterialInstances[i].HasProperty("_EmissionColor"))
                    {
                        lineMaterialInstances[i].SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }
    }

    public void TriggerEmissionBurst(int lineIndex)
    {
        if (enableEmissionBurst && lineIndex >= 0 && lineIndex < maxConnections)
        {
            isNewLine[lineIndex] = true;
            lineCreationTimes[lineIndex] = Time.time;
        }
    }

    public void TriggerAllEmissionBursts()
    {
        if (enableEmissionBurst)
        {
            float currentTime = Time.time;
            for (int i = 0; i < activeLineCount; i++)
            {
                isNewLine[i] = true;
                lineCreationTimes[i] = currentTime;
            }
        }
    }

    public void SetRefreshRate(float rate)
    {
        refreshRate = Mathf.Max(0.1f, rate);
        nextRefreshTime = Time.time; // Force refresh on next update
    }

    // Property to expose events for external access
    public UnityEvent<GameObject> OnConnectionEstablished => onConnectionEstablished;
    public UnityEvent<GameObject> OnConnectionBroken => onConnectionBroken;
}

[BurstCompile]
public struct CalculateCurvePointsJob : IJob
{
    [ReadOnly] public NativeArray<float3> SourcePosition;
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> CurveDirections;
    [WriteOnly] public NativeArray<float3> LinePoints;

    public float CurvatureAmount;
    public int LineSegments;
    public int ActiveCount;

    // Line trimming parameters
    public float SourceTrimPercentage;
    public float TargetTrimPercentage;
    public int UseFixedDistance; // Using int as bool in Burst jobs
    public float SourceTrimDistance;
    public float TargetTrimDistance;

    public void Execute()
    {
        float3 startPos = SourcePosition[0];

        for (int i = 0; i < ActiveCount; i++)
        {
            float3 endPos = TargetPositions[i];
            DrawCurvedLine(i, startPos, endPos);
        }
    }

    private void DrawCurvedLine(int lineIndex, float3 start, float3 end)
    {
        // Calculate direction vector
        float3 direction = end - start;
        float totalDistance = math.length(direction);

        // If the objects are too close or identical, just draw a straight line
        if (totalDistance < 0.001f)
        {
            for (int i = 0; i < LineSegments; i++)
            {
                int pointIndex = lineIndex * LineSegments + i;
                LinePoints[pointIndex] = start;
            }
            return;
        }

        // Get the pre-calculated curve direction for this connection
        float3 perpendicular = CurveDirections[lineIndex];

        // Calculate middle control point with curvature
        float3 middle = start + direction * 0.5f + perpendicular * totalDistance * CurvatureAmount * 0.25f;

        // Calculate the start and end trim lengths
        float sourceTrim, targetTrim;

        if (UseFixedDistance == 1) // Use fixed distance
        {
            // Cap the fixed distance to avoid over-trimming
            sourceTrim = math.min(SourceTrimDistance, totalDistance * 0.45f);
            targetTrim = math.min(TargetTrimDistance, totalDistance * 0.45f);
        }
        else // Use percentage
        {
            sourceTrim = totalDistance * SourceTrimPercentage;
            targetTrim = totalDistance * TargetTrimPercentage;

            // Avoid trimming more than 90% of the line total
            float maxTotalTrim = totalDistance * 0.9f;
            if (sourceTrim + targetTrim > maxTotalTrim)
            {
                float scale = maxTotalTrim / (sourceTrim + targetTrim);
                sourceTrim *= scale;
                targetTrim *= scale;
            }
        }

        // Generate and store the curve points directly
        for (int i = 0; i < LineSegments; i++)
        {
            float t = i / (float)(LineSegments - 1);
            int pointIndex = lineIndex * LineSegments + i;

            // Apply trimming by adjusting the t parameter
            if (sourceTrim > 0 || targetTrim > 0)
            {
                // Adjust t to account for trimming at both ends
                float trimStartT = sourceTrim / totalDistance;
                float trimEndT = 1 - (targetTrim / totalDistance);

                // Ensure valid range
                if (trimStartT >= trimEndT)
                {
                    trimStartT = 0;
                    trimEndT = 1;
                }

                // Remap t from [0-1] to [trimStartT-trimEndT]
                t = trimStartT + t * (trimEndT - trimStartT);
            }

            // Calculate the final point position using the bezier formula
            LinePoints[pointIndex] = CalculateBezierPoint(start, middle, end, t);
        }
    }

    private float3 CalculateBezierPoint(float3 p0, float3 p1, float3 p2, float t)
    {
        // Quadratic Bezier formula: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
        float u = 1.0f - t;
        float tt = t * t;
        float uu = u * u;

        float3 point = uu * p0;
        point += 2.0f * u * t * p1;
        point += tt * p2;

        return point;
    }
}