using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class _playhead : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string targetTag = "Entity";
    [SerializeField] private bool debugMode = true;

    [Header("Trigger Response Settings")]
    [SerializeField] private bool respondOnEnter = true;
    [SerializeField] private bool respondOnExit = false;
    [SerializeField] private bool respondOnStay = false;
    [SerializeField, Range(0f, 10f)] private float stayInterval = 2f;

    [Header("Audio Settings")]
    [SerializeField] private bool playSound = true;

    [Header("Emission Settings")]
    [SerializeField] private bool pulseEmission = true;
    [SerializeField] private AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float emissionDuration = 1f;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float emissionIntensity = 1f;

    [Header("Scale Animation Settings")]
    [SerializeField] private bool animateScale = true;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float scaleDuration = 1f;
    [SerializeField, Range(0.1f, 2f)] private float scaleMultiplier = 1.2f;

    [Header("Rotation Settings")]
    [SerializeField] private bool rotateObject = true;
    [SerializeField, Range(0f, 360f)] private float minRotation = 30f;
    [SerializeField, Range(0f, 360f)] private float maxRotation = 90f;
    [SerializeField] private enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField, Range(0.1f, 5f)] private float rotationDuration = 1f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float nextStayResponseTime;
    private Dictionary<Renderer, Coroutine> activeEmissionPulses = new Dictionary<Renderer, Coroutine>();
    private Dictionary<Transform, Coroutine> activeScaleAnimations = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Coroutine> activeRotations = new Dictionary<Transform, Coroutine>();

    private void Awake()
    {
        // Check for required components
        if (GetComponent<Rigidbody>() == null && GetComponent<Collider>() != null)
        {
            Debug.LogWarning("Adding Rigidbody component as it's required for trigger detection");
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Don't let physics move the object
            rb.useGravity = false; // Don't let gravity affect it
        }
    }

    private void Start()
    {
        nextStayResponseTime = Time.time;
        DebugLog("_playhead script initialized");
        DebugLog($"Target tag: {targetTag}");
        DebugLog($"Enter: {respondOnEnter}, Exit: {respondOnExit}, Stay: {respondOnStay}");
        DebugLog($"Sound: {playSound}, Emission: {pulseEmission}, Scale: {animateScale}, Rotate: {rotateObject}");

        // Check if this object actually has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("ERROR: No Collider found on _playhead object!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("ERROR: Collider on _playhead object is not set as a trigger!");
        }
        else
        {
            DebugLog("Collider check: OK");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DebugLog($"OnTriggerEnter detected with {other.name}, tag: {other.tag}");

        if (other.CompareTag(targetTag))
        {
            DebugLog("Tag match: YES");

            if (respondOnEnter)
            {
                DebugLog("Respond on enter: YES - Triggering responses");
                TriggerAllResponses(other.gameObject);
            }
            else
            {
                DebugLog("Respond on enter: NO - No actions taken");
            }
        }
        else
        {
            DebugLog($"Tag match: NO (Expected '{targetTag}', got '{other.tag}')");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DebugLog($"OnTriggerExit detected with {other.name}, tag: {other.tag}");

        if (other.CompareTag(targetTag))
        {
            DebugLog("Tag match: YES");

            if (respondOnExit)
            {
                DebugLog("Respond on exit: YES - Triggering responses");
                TriggerAllResponses(other.gameObject);
            }
            else
            {
                DebugLog("Respond on exit: NO - No actions taken");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Only log occasionally to avoid spam
        if (Time.time >= nextStayResponseTime - 0.1f)
        {
            DebugLog($"OnTriggerStay detected with {other.name}, tag: {other.tag}");
        }

        if (respondOnStay && other.CompareTag(targetTag) && Time.time >= nextStayResponseTime)
        {
            DebugLog("Respond on stay: YES - Triggering responses");
            TriggerAllResponses(other.gameObject);
            nextStayResponseTime = Time.time + stayInterval;
        }
    }

    private void TriggerAllResponses(GameObject targetObject)
    {
        DebugLog($"TriggerAllResponses called for {targetObject.name}");

        if (playSound)
        {
            DebugLog("Attempting to play sound...");
            bool success = PlayRandomSoundFromObject(targetObject);
            DebugLog($"Sound play result: {(success ? "SUCCESS" : "FAILED")}");
        }

        if (pulseEmission)
        {
            DebugLog("Attempting to pulse emission...");
            PulseEmissionOnObject(targetObject);
        }

        if (animateScale)
        {
            DebugLog("Attempting to animate scale...");
            AnimateScaleOnObject(targetObject);
        }

        if (rotateObject)
        {
            DebugLog("Attempting to rotate object...");
            RotateObject(targetObject);
        }
    }

    private bool PlayRandomSoundFromObject(GameObject targetObject)
    {
        // Try to get component from the object itself
        RandomAudioPlayer audioPlayer = targetObject.GetComponent<RandomAudioPlayer>();

        if (audioPlayer != null)
        {
            DebugLog($"RandomAudioPlayer found on {targetObject.name}");
            audioPlayer.PlayRandomSound();
            return true;
        }

        // If not found, try children
        DebugLog("No RandomAudioPlayer on object, checking children...");
        audioPlayer = targetObject.GetComponentInChildren<RandomAudioPlayer>();

        if (audioPlayer != null)
        {
            DebugLog($"RandomAudioPlayer found in children of {targetObject.name}");
            audioPlayer.PlayRandomSound();
            return true;
        }

        Debug.LogWarning($"WARNING: No RandomAudioPlayer found on {targetObject.name} or its children");
        return false;
    }

    private void PulseEmissionOnObject(GameObject targetObject)
    {
        // Get all renderers
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"WARNING: No Renderers found on {targetObject.name}");
            return;
        }

        DebugLog($"Found {renderers.Length} renderers to check for emission");

        bool anyEmissionStarted = false;

        foreach (Renderer renderer in renderers)
        {
            // Check if already pulsing this renderer
            if (activeEmissionPulses.ContainsKey(renderer))
            {
                DebugLog($"Already pulsing renderer {renderer.name}, restarting...");
                StopCoroutine(activeEmissionPulses[renderer]);
                activeEmissionPulses.Remove(renderer);
            }

            // Check materials for emission capability
            bool foundEmissionMaterial = false;
            Material[] materials = renderer.materials;

            DebugLog($"Checking {materials.Length} materials on renderer {renderer.name}");

            foreach (Material material in materials)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    foundEmissionMaterial = true;
                    DebugLog($"Found emission-capable material on {renderer.name}");
                    Coroutine pulseCoroutine = StartCoroutine(PulseEmissionCoroutine(renderer, material));
                    activeEmissionPulses.Add(renderer, pulseCoroutine);
                    anyEmissionStarted = true;
                    break; // Only need one emission material per renderer
                }
            }

            if (!foundEmissionMaterial)
            {
                DebugLog($"No emission-capable materials found on {renderer.name}");
            }
        }

        if (anyEmissionStarted)
        {
            DebugLog("Emission pulse successfully started");
        }
        else
        {
            Debug.LogWarning("WARNING: No emission pulses were started - no compatible materials found");
        }
    }

    private IEnumerator PulseEmissionCoroutine(Renderer renderer, Material material)
    {
        DebugLog($"Starting emission pulse coroutine for {renderer.name}");

        // Store original emission color
        Color originalEmission = Color.black;
        if (material.HasProperty("_EmissionColor"))
        {
            originalEmission = material.GetColor("_EmissionColor");
            DebugLog($"Original emission color: {originalEmission}");
        }

        // Enable emission
        material.EnableKeyword("_EMISSION");

        float startTime = Time.time;

        while (Time.time < startTime + emissionDuration)
        {
            if (renderer == null)
            {
                DebugLog("Renderer was destroyed during emission pulse, aborting");
                yield break;
            }

            float progress = (Time.time - startTime) / emissionDuration;
            float curveValue = emissionCurve.Evaluate(progress);

            // Apply emission
            Color newEmission = emissionColor * curveValue * emissionIntensity;
            material.SetColor("_EmissionColor", newEmission);

            yield return null;
        }

        // Reset to original emission
        if (renderer != null)
        {
            DebugLog($"Emission pulse complete for {renderer.name}, resetting");
            material.SetColor("_EmissionColor", originalEmission);
        }

        // Remove from active dictionary
        activeEmissionPulses.Remove(renderer);
    }

    private void AnimateScaleOnObject(GameObject targetObject)
    {
        Transform objTransform = targetObject.transform;
        DebugLog($"Starting scale animation for {targetObject.name}");

        // Check if already animating scale
        if (activeScaleAnimations.ContainsKey(objTransform))
        {
            DebugLog("Already animating scale, restarting...");
            StopCoroutine(activeScaleAnimations[objTransform]);
            activeScaleAnimations.Remove(objTransform);
        }

        // Start scale animation
        Coroutine scaleCoroutine = StartCoroutine(AnimateScaleCoroutine(objTransform));
        activeScaleAnimations.Add(objTransform, scaleCoroutine);
    }

    private IEnumerator AnimateScaleCoroutine(Transform targetTransform)
    {
        // Store original scale
        Vector3 originalScale = targetTransform.localScale;
        Vector3 targetScale = originalScale * scaleMultiplier;

        DebugLog($"Scale animation from {originalScale} to {targetScale}");

        float startTime = Time.time;

        // Animate to target scale and back
        while (Time.time < startTime + scaleDuration)
        {
            if (targetTransform == null)
            {
                DebugLog("Transform was destroyed during scale animation, aborting");
                yield break;
            }

            float progress = (Time.time - startTime) / scaleDuration;
            float curveValue = scaleCurve.Evaluate(progress);

            // Calculate current scale
            Vector3 newScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            targetTransform.localScale = newScale;

            yield return null;
        }

        // Reset to original scale
        if (targetTransform != null)
        {
            DebugLog("Scale animation complete, resetting");
            targetTransform.localScale = originalScale;
        }

        // Remove from active dictionary
        activeScaleAnimations.Remove(targetTransform);
    }

    private void RotateObject(GameObject targetObject)
    {
        Transform objTransform = targetObject.transform;
        DebugLog($"Starting rotation for {targetObject.name}");

        // Check if already rotating
        if (activeRotations.ContainsKey(objTransform))
        {
            DebugLog("Already rotating, restarting...");
            StopCoroutine(activeRotations[objTransform]);
            activeRotations.Remove(objTransform);
        }

        // Generate random rotation amount between min and max
        float randomAngle = Random.Range(minRotation, maxRotation);
        // Randomly choose positive or negative rotation
        if (Random.value > 0.5f) randomAngle *= -1;

        DebugLog($"Random rotation angle: {randomAngle} degrees on {rotationAxis} axis");

        // Start rotation animation
        Coroutine rotationCoroutine = StartCoroutine(RotateObjectCoroutine(objTransform, randomAngle));
        activeRotations.Add(objTransform, rotationCoroutine);
    }

    private IEnumerator RotateObjectCoroutine(Transform targetTransform, float angle)
    {
        // Store original rotation
        Quaternion originalRotation = targetTransform.localRotation;

        // Create target rotation based on selected axis
        Vector3 rotationVector = Vector3.zero;
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(angle, 0, 0);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0, angle, 0);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0, 0, angle);
                break;
        }

        Quaternion targetRotation = originalRotation * Quaternion.Euler(rotationVector);

        DebugLog($"Rotating from {originalRotation.eulerAngles} to {targetRotation.eulerAngles}");

        float startTime = Time.time;

        // Animate rotation
        while (Time.time < startTime + rotationDuration)
        {
            if (targetTransform == null)
            {
                DebugLog("Transform was destroyed during rotation, aborting");
                yield break;
            }

            float progress = (Time.time - startTime) / rotationDuration;
            float curveValue = rotationCurve.Evaluate(progress);

            // Calculate current rotation
            Quaternion newRotation = Quaternion.Slerp(originalRotation, targetRotation, curveValue);
            targetTransform.localRotation = newRotation;

            yield return null;
        }

        // Ensure final rotation is exactly the target
        if (targetTransform != null)
        {
            targetTransform.localRotation = targetRotation;
            DebugLog("Rotation complete");
        }

        // Remove from active dictionary
        activeRotations.Remove(targetTransform);
    }

    private void OnDestroy()
    {
        DebugLog("_playhead script being destroyed, cleaning up coroutines");

        // Clean up all coroutines
        foreach (var coroutine in activeEmissionPulses.Values)
        {
            StopCoroutine(coroutine);
        }

        foreach (var coroutine in activeScaleAnimations.Values)
        {
            StopCoroutine(coroutine);
        }

        foreach (var coroutine in activeRotations.Values)
        {
            StopCoroutine(coroutine);
        }

        activeEmissionPulses.Clear();
        activeScaleAnimations.Clear();
        activeRotations.Clear();
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[_playhead] {message}");
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the trigger zone in the Scene view
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                // Approximate capsule visualization
                Vector3 size = new Vector3(
                    capsuleCol.radius * 2,
                    capsuleCol.height,
                    capsuleCol.radius * 2
                );
                Gizmos.DrawCube(capsuleCol.center, size);
            }
        }
    }
}