using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplineTools
{
    [RequireComponent(typeof(SplineContainer))]
    public class CircularSplineGenerator : MonoBehaviour
    {
        [Header("Spline Settings")]
        [SerializeField] private float diameter = 5f;
        [SerializeField, Range(4, 32)] private int segments = 8;

        [Header("Noise Settings")]
        [SerializeField, Range(0f, 1f)] private float noiseAmount = 0f;
        [SerializeField] private float noiseScale = 1f;
        [SerializeField] private Vector2 noiseOffset;

        [Header("Prefab Distribution")]
        [SerializeField] private GameObject[] prefabs;
        [SerializeField, Range(4, 100)] private int numberOfPrefabs = 10;
        [SerializeField] private bool alignTangent = true;
        [SerializeField] private Vector3 prefabOffset = Vector3.zero;
        [SerializeField, Range(0f, 1f)] private float randomRotation = 0f;
        [SerializeField] private bool rotateYAxisOnly = true;

        [Header("Prefab Scaling")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField, Range(0.1f, 10f)] private float scaleMultiplier = 1.0f;

        private SplineContainer splineContainer;
        private Spline spline;
        private List<GameObject> spawnedPrefabs = new List<GameObject>();

        // Cached values for efficiency
        private float radius;
        private float angleStep;
        private Vector3[] cachedPositions;
        private Vector3[] cachedTangents;
        private bool isDirty = true;

        private void Awake()
        {
            Initialize();
            GenerateCircularSpline();
            DistributePrefabs();
        }

        private void OnValidate()
        {
            // Make sure we're initialized before generating
            Initialize();
            isDirty = true;
            GenerateCircularSpline();
        }

        private void Initialize()
        {
            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();
            if (spline == null && splineContainer != null)
                spline = splineContainer.Spline;
            if (spawnedPrefabs == null)
                spawnedPrefabs = new List<GameObject>();

            radius = diameter * 0.5f;
            angleStep = 360f / segments * Mathf.Deg2Rad;

            if (cachedPositions == null || cachedPositions.Length != segments)
            {
                cachedPositions = new Vector3[segments];
                cachedTangents = new Vector3[segments];
            }
        }

        public void GenerateCircularSpline()
        {
            if (spline == null || !isDirty) return;

            spline.Clear();
            CalculateSplinePoints();

            for (int i = 0; i < segments; i++)
            {
                float handleLength = CalculateHandleLength(i);

                BezierKnot knot = new BezierKnot(
                    cachedPositions[i],
                    -cachedTangents[i] * handleLength,
                    cachedTangents[i] * handleLength
                );

                spline.Add(knot);
            }

            spline.Closed = true;
            isDirty = false;
        }

        private void CalculateSplinePoints()
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;

                // Base circle position
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                // Apply noise if needed
                float noisyRadius = radius;
                if (noiseAmount > 0)
                {
                    float noise = CalculateNoise(x, z);
                    noisyRadius *= (1f + noise * noiseAmount);
                }

                // Store position and tangent
                cachedPositions[i] = new Vector3(
                    Mathf.Cos(angle) * noisyRadius,
                    0,
                    Mathf.Sin(angle) * noisyRadius
                );

                cachedTangents[i] = new Vector3(
                    -Mathf.Sin(angle),
                    0,
                    Mathf.Cos(angle)
                );
            }
        }

        private float CalculateNoise(float x, float z)
        {
            float noiseX = (x / radius + 1f) * noiseScale + noiseOffset.x;
            float noiseZ = (z / radius + 1f) * noiseScale + noiseOffset.y;
            return Mathf.PerlinNoise(noiseX, noiseZ) * 2f - 1f;
        }

        private float CalculateHandleLength(int index)
        {
            // Mathematical solution for perfect circular Bezier curves
            float baseLength = radius * (4f / 3f) * Mathf.Tan(Mathf.PI / (2f * segments));

            if (noiseAmount <= 0) return baseLength;

            // Adjust handle length based on local radius change
            Vector3 position = cachedPositions[index];
            float currentRadius = new Vector2(position.x, position.z).magnitude;
            return baseLength * (currentRadius / radius);
        }

        public void DistributePrefabs()
        {
            // Only distribute prefabs in play mode
            if (!Application.isPlaying) return;

            if (!CanDistributePrefabs()) return;

            // Always clean up existing prefabs first to ensure a clean slate
            CleanupPrefabs();

            Transform cachedTransform = transform;
            int prefabCount = prefabs.Length;

            // Pre-size the list for better performance
            spawnedPrefabs.Capacity = numberOfPrefabs;

            for (int i = 0; i < numberOfPrefabs; i++)
            {
                float t = (float)i / numberOfPrefabs;

                // Get local position and direction from spline
                Vector3 localPosition = spline.EvaluatePosition(t);
                Vector3 localDirection = spline.EvaluateTangent(t);

                // Transform to world space
                Vector3 worldPosition = cachedTransform.TransformPoint(localPosition);
                Vector3 worldDirection = cachedTransform.TransformDirection(localDirection);

                // Calculate rotation
                Quaternion rotation = Quaternion.identity;

                // First set the base rotation based on tangent alignment
                if (alignTangent && worldDirection != Vector3.zero)
                {
                    rotation = Quaternion.LookRotation(worldDirection);
                }

                // Then apply random rotation if needed
                if (randomRotation > 0f)
                {
                    if (rotateYAxisOnly)
                    {
                        // Random rotation around Y-axis only
                        float randomAngle = Random.Range(0f, 360f * randomRotation);
                        rotation *= Quaternion.Euler(0f, randomAngle, 0f);
                    }
                    else
                    {
                        // Random rotation on all three axes
                        float randomAngleX = Random.Range(0f, 360f * randomRotation);
                        float randomAngleY = Random.Range(0f, 360f * randomRotation);
                        float randomAngleZ = Random.Range(0f, 360f * randomRotation);
                        rotation *= Quaternion.Euler(randomAngleX, randomAngleY, randomAngleZ);
                    }
                }

                // Apply offset in local space of the rotation
                Vector3 offsetPosition = worldPosition + rotation * prefabOffset;

                // Select random prefab and instantiate
                int randomPrefabIndex = Random.Range(0, prefabCount);
                GameObject prefab = prefabs[randomPrefabIndex];

                // Skip null prefabs
                if (prefab == null) continue;

                // Instantiate in play mode
                GameObject instance = Instantiate(prefab, offsetPosition, rotation, cachedTransform);

                // Apply scale based on a random point on the animation curve
                float randomPoint = Random.value; // Random value between 0-1
                float curveValue = scaleCurve.Evaluate(randomPoint);
                float finalScale = curveValue * scaleMultiplier;
                instance.transform.localScale = Vector3.one * finalScale;

                spawnedPrefabs.Add(instance);
            }
        }

        private bool CanDistributePrefabs()
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("No prefabs assigned to CircularSplineGenerator.");
                return false;
            }

            // Make sure we have a valid spline before distributing
            if (spline == null || spline.Count == 0)
            {
                GenerateCircularSpline();

                // If spline is still invalid after generation, don't proceed
                if (spline == null || spline.Count == 0)
                {
                    Debug.LogWarning("Cannot distribute prefabs, spline is invalid.");
                    return false;
                }
            }

            return true;
        }

        public void CleanupPrefabs()
        {
            if (spawnedPrefabs == null)
            {
                spawnedPrefabs = new List<GameObject>();
                return;
            }

            // Remove any null entries that might have accumulated
            spawnedPrefabs.RemoveAll(item => item == null);

            // Destroy all remaining prefab instances
            for (int i = spawnedPrefabs.Count - 1; i >= 0; i--)
            {
                if (spawnedPrefabs[i] != null)
                {
                    Destroy(spawnedPrefabs[i]);
                }
            }
            spawnedPrefabs.Clear();
        }

        private void OnDestroy()
        {
            CleanupPrefabs();
        }

        // Method to set all properties and regenerate spline/prefabs
        public void UpdateSpline()
        {
            isDirty = true;
            GenerateCircularSpline();

            if (Application.isPlaying)
            {
                CleanupPrefabs();
                DistributePrefabs();
            }
        }
    }

#if UNITY_EDITOR
    // Minimal custom editor that just adds info about play mode
    [CustomEditor(typeof(CircularSplineGenerator))]
    public class CircularSplineGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Prefabs will only be instantiated when in Play Mode.", MessageType.Info);
            }

            // Draw default inspector with all properties
            DrawDefaultInspector();

            // Add a button to update the spline
            if (GUILayout.Button("Update Spline"))
            {
                CircularSplineGenerator generator = (CircularSplineGenerator)target;
                generator.UpdateSpline(); // Use UpdateSpline() which sets isDirty = true
                SceneView.RepaintAll();
            }

            // Add a button to distribute prefabs (only in play mode)
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Distribute Prefabs"))
            {
                CircularSplineGenerator generator = (CircularSplineGenerator)target;
                generator.CleanupPrefabs();
                generator.DistributePrefabs();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}