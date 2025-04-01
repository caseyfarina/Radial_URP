using UnityEngine;
using DG.Tweening;

/// <summary>
/// Flips a GameObject using DOTween with configurable parameters
/// </summary>
public class ObjectFlipper : MonoBehaviour
{
    [Header("Flip Animation Settings")]
    [Tooltip("Duration of the flip animation in seconds")]
    [SerializeField, Range(0.1f, 15f)] private float flipDuration = 0.7f;

    [Tooltip("Amount of rotation in degrees (180° for a complete flip)")]
    [SerializeField, Range(0f, 360f)] private float rotationAmount = 180f;

    [Tooltip("Axis around which to rotate the object")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("Easing function to use for the animation")]
    [SerializeField] private Ease easingType = Ease.OutSine;

    [Tooltip("Whether to log debug information")]
    [SerializeField] private bool debugMode = false;

    [Header("Animation Curve Option")]
    [Tooltip("If assigned, this curve will be used instead of the ease type")]
    [SerializeField] private AnimationCurve customAnimationCurve;
    [Tooltip("Event triggered on odd-numbered flips (first, third, etc.)")]
    [SerializeField] private UnityEngine.Events.UnityEvent onFirstFlip;

    [Tooltip("Event triggered on even-numbered flips (second, fourth, etc.)")]
    [SerializeField] private UnityEngine.Events.UnityEvent onSecondFlip;

    // Enum for selecting rotation axis
    public enum RotationAxis { X, Y, Z }

    // Store active animation tween
    private Tween activeFlipTween;

    // Track which event should be triggered next
    private bool isFirstFlipNext = true;

    // Cache transform for better performance
    private Transform cachedTransform;

    private void Awake()
    {
        // Cache transform for better performance
        cachedTransform = transform;
    }

    /// <summary>
    /// Starts a flip animation with default parameters
    /// </summary>
    public void Flip()
    {
        // Check if a custom animation curve should be used
        if (customAnimationCurve != null && customAnimationCurve.length >= 2)
        {
            FlipWithCurve(customAnimationCurve);
        }
        else
        {
            // Use default settings
            FlipWithParameters(rotationAmount, flipDuration, rotationAxis, easingType);
        }
    }

    /// <summary>
    /// Starts a flip animation with custom parameters
    /// </summary>
    /// <param name="degrees">Amount to rotate in degrees</param>
    /// <param name="duration">Duration of the animation in seconds</param>
    /// <param name="axis">Axis to rotate around</param>
    /// <param name="easing">Easing type to use</param>
    public void FlipWithParameters(float degrees, float duration, RotationAxis axis, Ease easing)
    {
        DebugLog($"Starting flip animation: {degrees}° on {axis} axis over {duration} seconds with {easing} easing");

        // Kill any active animation
        if (activeFlipTween != null && activeFlipTween.IsActive())
        {
            DebugLog("Killing previous flip animation");
            activeFlipTween.Kill();
            activeFlipTween = null;
        }

        // Calculate target rotation vector based on selected axis
        Vector3 rotationVector = Vector3.zero;
        switch (axis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(degrees, 0, 0);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0, degrees, 0);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0, 0, degrees);
                break;
        }

        // Trigger event before starting the animation
        TriggerFlipEvent();

        // Create the flip animation using the specified rotation amount
        // Using DOLocalRotate with RotateMode.LocalAxisAdd will ensure consistent rotation
        activeFlipTween = cachedTransform.DOLocalRotate(rotationVector, duration, RotateMode.LocalAxisAdd)
            .SetEase(easing)
            .OnComplete(() => {
                DebugLog("Flip animation complete");
                activeFlipTween = null;
            });
    }

    /// <summary>
    /// Alternative flip method using a specified AnimationCurve instead of Ease preset
    /// </summary>
    /// <param name="animationCurve">Animation curve to control the flip motion</param>
    public void FlipWithCurve(AnimationCurve animationCurve)
    {
        if (animationCurve == null || animationCurve.length < 2)
        {
            Debug.LogError("Invalid animation curve provided. Using default ease instead.");
            Flip();
            return;
        }

        DebugLog($"Starting curve-based flip animation: {rotationAmount}° on {rotationAxis} axis over {flipDuration} seconds");

        // Kill any active animation
        if (activeFlipTween != null && activeFlipTween.IsActive())
        {
            activeFlipTween.Kill();
            activeFlipTween = null;
        }

        // Calculate target rotation vector based on selected axis
        Vector3 rotationVector = Vector3.zero;
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(rotationAmount, 0, 0);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0, rotationAmount, 0);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0, 0, rotationAmount);
                break;
        }

        // Trigger event before starting the animation
        TriggerFlipEvent();

        // Create the animation with custom curve
        activeFlipTween = cachedTransform.DOLocalRotate(rotationVector, flipDuration, RotateMode.LocalAxisAdd)
            .SetEase(animationCurve)
            .OnComplete(() => {
                DebugLog("Curve-based flip animation complete");
                activeFlipTween = null;
            });
    }

    /// <summary>
    /// Stop any active flip animation
    /// </summary>
    /// <param name="completeAnimation">If true, animation will be completed, otherwise it will be stopped at current state</param>
    public void StopFlip(bool completeAnimation = false)
    {
        if (activeFlipTween != null && activeFlipTween.IsActive())
        {
            if (completeAnimation)
            {
                activeFlipTween.Complete();
                // Note: The OnComplete callback will be triggered, which will invoke events
            }
            else
            {
                activeFlipTween.Kill();
                // No events are triggered when killed without completion
            }
            activeFlipTween = null;
            DebugLog($"Flip animation stopped (completed: {completeAnimation})");
        }
    }

    /// <summary>
    /// Resets the flip event toggle state
    /// </summary>
    /// <param name="setToFirst">If true, the next flip will trigger the first event</param>
    public void ResetFlipEventToggle(bool setToFirst = true)
    {
        isFirstFlipNext = setToFirst;
        DebugLog($"Flip event toggle reset. Next flip will trigger the {(setToFirst ? "first" : "second")} event.");
    }

    /// <summary>
    /// Helper method to trigger appropriate flip event and toggle state
    /// </summary>
    private void TriggerFlipEvent()
    {
        // Trigger appropriate event based on alternating state
        if (isFirstFlipNext)
        {
            DebugLog("Triggering first flip event");
            onFirstFlip?.Invoke();
        }
        else
        {
            DebugLog("Triggering second flip event");
            onSecondFlip?.Invoke();
        }

        // Toggle state for next flip
        isFirstFlipNext = !isFirstFlipNext;

        // Clear active tween reference
        activeFlipTween = null;
    }

    private void OnDestroy()
    {
        // Clean up any active tween when destroyed
        if (activeFlipTween != null && activeFlipTween.IsActive())
        {
            activeFlipTween.Kill();
            activeFlipTween = null;
        }
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ObjectFlipper] {message}");
        }
    }
}