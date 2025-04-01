using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls rotation animation effects using DOTween for improved performance
/// </summary>
public class RotationEffect : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField, Range(0f, 360f)] private float minRotation = 30f;
    [SerializeField, Range(0f, 360f)] private float maxRotation = 90f;

    public enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    [SerializeField, Range(0.1f, 5f)] private float rotationDuration = 1f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(-1, 10)] private int defaultLoopCount = 0; // -1 for infinite, 0 for single play, >0 for specific loop count
    [SerializeField] private bool debugMode = false;

    private Tween activeRotationTween;
    private Quaternion originalRotation;

    private void Awake()
    {
        // Store original rotation for reference
        originalRotation = transform.localRotation;
    }

    private void OnDestroy()
    {
        // Clean up any active tweens when destroyed
        if (activeRotationTween != null && activeRotationTween.IsActive())
        {
            activeRotationTween.Kill();
            activeRotationTween = null;
        }
    }

    /// <summary>
    /// Plays a rotation animation using DOTween with default loop count
    /// </summary>
    public void PlayRotation()
    {
        PlayRotationWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Plays a rotation animation using DOTween with specified loop count
    /// </summary>
    /// <param name="loopCount">Number of times to repeat the animation. -1 for infinite loops.</param>
    public void PlayRotationWithLoops(int loopCount)
    {
        DebugLog($"PlayRotation called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        // If already rotating, stop current rotation
        if (activeRotationTween != null && activeRotationTween.IsActive())
        {
            DebugLog("Already rotating, restarting...");
            activeRotationTween.Kill();
            activeRotationTween = null;
        }

        // Use current rotation as the starting point
        Quaternion currentRotation = transform.localRotation;

        // Generate random rotation amount between min and max
        float randomAngle = Random.Range(minRotation, maxRotation);
        // Randomly choose positive or negative rotation
        if (Random.value > 0.5f) randomAngle *= -1;

        DebugLog($"Random rotation angle: {randomAngle} degrees on {rotationAxis} axis");

        // Create target rotation based on selected axis
        Vector3 rotationVector = Vector3.zero;
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(randomAngle, 0, 0);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0, randomAngle, 0);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0, 0, randomAngle);
                break;
        }

        Quaternion targetRotation = currentRotation * Quaternion.Euler(rotationVector);

        DebugLog($"Rotating from {currentRotation.eulerAngles} to {targetRotation.eulerAngles}");

        // Create the rotation tween (single rotation without returning to original)
        activeRotationTween = transform.DOLocalRotateQuaternion(targetRotation, rotationDuration)
            .SetEase(rotationCurve)
            .OnComplete(() => {
                DebugLog("Rotation animation complete");
                activeRotationTween = null;
            });

        // Set loop count if needed - for a non-sequence tween
        if (loopCount != 0)
        {
            activeRotationTween.SetLoops(loopCount);
        }
    }

    /// <summary>
    /// Plays a continuous rotation animation on a specific axis
    /// </summary>
    /// <param name="degreesPerSecond">Speed of rotation in degrees per second</param>
    /// <param name="duration">Duration of the animation in seconds. Use -1 for infinite rotation.</param>
    public void PlayContinuousRotation(float degreesPerSecond, float duration = -1)
    {
        DebugLog($"PlayContinuousRotation called with {degreesPerSecond} degrees/sec for {(duration < 0 ? "infinite" : duration.ToString())} seconds");

        // If already rotating, stop current rotation
        if (activeRotationTween != null && activeRotationTween.IsActive())
        {
            DebugLog("Already rotating, restarting...");
            activeRotationTween.Kill();
            activeRotationTween = null;
        }

        // Calculate the total rotation based on duration and speed
        float totalRotation = duration < 0 ? 100000f : degreesPerSecond * duration;

        // Create rotation vector based on selected axis
        Vector3 rotationVector = Vector3.zero;
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(totalRotation, 0, 0);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0, totalRotation, 0);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0, 0, totalRotation);
                break;
        }

        // Create the rotation tween
        activeRotationTween = transform.DOLocalRotate(
            rotationVector,        // Target rotation (large value for continuous rotation)
            duration < 0 ? 100000f : duration,  // Duration (large value for continuous rotation)
            RotateMode.LocalAxisAdd  // Important: Add rotation to current rotation
        ).SetEase(Ease.Linear);      // Linear rotation for consistent speed

        // If duration is -1, set it to loop infinitely
        if (duration < 0)
        {
            activeRotationTween.SetLoops(-1, LoopType.Restart);
        }

        // Set completion callback
        activeRotationTween.OnComplete(() => {
            DebugLog("Continuous rotation complete");
            activeRotationTween = null;
        });
    }

    /// <summary>
    /// Stops any active rotation animation
    /// </summary>
    public void StopRotation()
    {
        if (activeRotationTween != null && activeRotationTween.IsActive())
        {
            DebugLog("Stopping rotation animation");
            activeRotationTween.Kill();
            activeRotationTween = null;
        }
    }

    /// <summary>
    /// Rotates to a specific angle on the configured axis
    /// </summary>
    /// <param name="angle">Target angle in degrees</param>
    /// <param name="duration">Duration of the rotation</param>
    /// <param name="relative">If true, angle is added to current rotation</param>
    public void RotateToAngle(float angle, float duration, bool relative = false)
    {
        DebugLog($"RotateToAngle called: {angle} degrees over {duration} seconds (relative: {relative})");

        // If already rotating, stop current rotation
        if (activeRotationTween != null && activeRotationTween.IsActive())
        {
            DebugLog("Already rotating, restarting...");
            activeRotationTween.Kill();
            activeRotationTween = null;
        }

        // Create rotation vector based on selected axis
        Vector3 targetEuler = transform.localEulerAngles;

        if (relative)
        {
            // Add to current rotation
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    targetEuler.x += angle;
                    break;
                case RotationAxis.Y:
                    targetEuler.y += angle;
                    break;
                case RotationAxis.Z:
                    targetEuler.z += angle;
                    break;
            }
        }
        else
        {
            // Set absolute rotation
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    targetEuler.x = angle;
                    break;
                case RotationAxis.Y:
                    targetEuler.y = angle;
                    break;
                case RotationAxis.Z:
                    targetEuler.z = angle;
                    break;
            }
        }

        // Create the rotation tween
        activeRotationTween = transform.DOLocalRotate(
            targetEuler,
            duration,
            RotateMode.FastBeyond360  // Allows for rotations beyond 360°
        ).SetEase(rotationCurve);

        // Set completion callback
        activeRotationTween.OnComplete(() => {
            DebugLog("RotateToAngle complete");
            activeRotationTween = null;
        });
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[RotationEffect] {message}");
        }
    }
}