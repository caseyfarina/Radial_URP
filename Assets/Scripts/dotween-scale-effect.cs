using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls scale animation effects using DOTween for improved performance
/// </summary>
public class ScaleAnimationEffect : MonoBehaviour
{
    [Header("Scale Animation Settings")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float scaleDuration = 1f;
    [SerializeField, Range(0.1f, 2f)] private float scaleMultiplier = 1.2f;
    [SerializeField, Range(-1, 10)] private int defaultLoopCount = 0; // -1 for infinite, 0 for single play, >0 for specific loop count
    [SerializeField] private bool debugMode = false;

    private Tween activeScaleTween;
    private Vector3 originalScale;

    private void Awake()
    {
        // Store original scale for reference
        originalScale = transform.localScale;
    }

    private void OnDestroy()
    {
        // Clean up any active tweens when destroyed
        if (activeScaleTween != null && activeScaleTween.IsActive())
        {
            activeScaleTween.Kill();
            activeScaleTween = null;
        }
    }

    /// <summary>
    /// Plays a scale up and down animation using DOTween Sequence with default loop count
    /// </summary>
    public void PlayScaleAnimation()
    {
        PlayScaleAnimationWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Plays a scale up and down animation using DOTween Sequence with specified loop count
    /// </summary>
    /// <param name="loopCount">Number of times to repeat the animation. -1 for infinite loops.</param>
    public void PlayScaleAnimationWithLoops(int loopCount)
    {
        DebugLog($"PlayScaleAnimation called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        // Update original scale in case it changed
        originalScale = transform.localScale;
        Vector3 targetScale = originalScale * scaleMultiplier;

        // If there's an active tween, kill it
        if (activeScaleTween != null && activeScaleTween.IsActive())
        {
            DebugLog("Already animating scale, restarting...");
            activeScaleTween.Kill();
        }

        DebugLog($"Scale animation from {originalScale} to {targetScale}");

        // Create a sequence for the full animation
        Sequence scaleSequence = DOTween.Sequence();

        // Add scale up and down tweens
        scaleSequence.Append(transform.DOScale(targetScale, scaleDuration / 2).SetEase(scaleCurve));
        scaleSequence.Append(transform.DOScale(originalScale, scaleDuration / 2).SetEase(scaleCurve));

        // Set loop count if needed
        if (loopCount != 0)
        {
            scaleSequence.SetLoops(loopCount);
        }

        // Set callbacks
        scaleSequence.OnComplete(() => {
            DebugLog("Scale animation complete");
            activeScaleTween = null;
        });

        // Store the sequence as the active tween
        activeScaleTween = scaleSequence;
    }

    /// <summary>
    /// Custom implementation with a scaled curve (pulse effect) using default loop count
    /// </summary>
    public void PlayPulsatingScale()
    {
        PlayPulsatingScaleWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Custom implementation with a scaled curve (pulse effect) with specified loop count
    /// </summary>
    /// <param name="loopCount">Number of times to repeat the animation. -1 for infinite loops.</param>
    public void PlayPulsatingScaleWithLoops(int loopCount)
    {
        DebugLog($"PlayPulsatingScale called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        // Update original scale in case it changed
        originalScale = transform.localScale;

        // If there's an active tween, kill it
        if (activeScaleTween != null && activeScaleTween.IsActive())
        {
            DebugLog("Already animating scale, restarting...");
            activeScaleTween.Kill();
        }

        // Create a DOTween tween with a custom ease function that uses our AnimationCurve
        activeScaleTween = transform.DOScale(originalScale, scaleDuration)
            .SetEase((time, duration, overshootOrAmplitude, period) => {
                // Custom ease function using our animation curve
                float curveValue = scaleCurve.Evaluate(time / duration);

                // Scale the value to create pulsating effect
                // Map the curve value from 0-1 to 1-scaleMultiplier
                return 1 + (curveValue * (scaleMultiplier - 1));
            })
            .SetLoops(loopCount < 0 ? -1 : loopCount + 1) // -1 is infinite, otherwise add 1 for initial play
            .OnUpdate(() => {
                if (debugMode && Time.frameCount % 30 == 0) // Reduce log spam by only logging every 30 frames
                {
                    DebugLog($"Current scale: {transform.localScale}");
                }
            })
            .OnComplete(() => {
                DebugLog("Pulsating scale complete");
                transform.localScale = originalScale; // Ensure it ends at original scale
                activeScaleTween = null;
            });
    }

    /// <summary>
    /// Stops any active scale animation
    /// </summary>
    public void StopScaleAnimation()
    {
        if (activeScaleTween != null && activeScaleTween.IsActive())
        {
            DebugLog("Stopping scale animation");
            activeScaleTween.Kill();
            activeScaleTween = null;
        }
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ScaleAnimationEffect] {message}");
        }
    }
}