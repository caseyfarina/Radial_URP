using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls scale animation effects using DOTween for improved performance
/// </summary>
public class ScaleAnimationEffect : DOTweenEffectBase
{
    [Header("Scale Animation Settings")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float scaleDuration = 1f;
    [SerializeField, Range(0.1f, 2f)] private float scaleMultiplier = 1.2f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
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
    public void PlayScaleAnimationWithLoops(int loopCount)
    {
        DebugLog($"PlayScaleAnimation called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        PrepareForNewTween();

        transform.localScale = originalScale;

        Vector3 targetScale = originalScale * scaleMultiplier;

        DebugLog($"Scale animation from {originalScale} to {targetScale}");

        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(transform.DOScale(targetScale, scaleDuration / 2).SetEase(scaleCurve));
        scaleSequence.Append(transform.DOScale(originalScale, scaleDuration / 2).SetEase(scaleCurve));

        if (loopCount != 0)
        {
            scaleSequence.SetLoops(loopCount);
        }

        scaleSequence.OnComplete(() => {
            DebugLog("Scale animation complete");
            transform.localScale = originalScale;
            activeTween = null;
        });

        activeTween = scaleSequence;
    }

    /// <summary>
    /// Custom implementation with a scaled curve effect using default loop count
    /// </summary>
    public void PlayScaleAnimationCurve()
    {
        PlayScaleAnimationCurveWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Custom implementation with a scaled curve effect with specified loop count
    /// </summary>
    public void PlayScaleAnimationCurveWithLoops(int loopCount)
    {
        DebugLog($"PlayScaleAnimationCurve called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        PrepareForNewTween();

        transform.localScale = originalScale;

        activeTween = transform.DOScale(originalScale, scaleDuration)
            .SetEase((time, duration, overshootOrAmplitude, period) => {
                float curveValue = scaleCurve.Evaluate(time / duration);
                return 1 + (curveValue * (scaleMultiplier - 1));
            })
            .SetLoops(loopCount < 0 ? -1 : loopCount + 1)
            .OnUpdate(() => {
                if (debugMode && Time.frameCount % 30 == 0)
                {
                    DebugLog($"Current scale: {transform.localScale}");
                }
            })
            .OnComplete(() => {
                DebugLog("Scale animation curve complete");
                transform.localScale = originalScale;
                activeTween = null;
            });
    }

    /// <summary>
    /// Stops any active scale animation and resets to original scale
    /// </summary>
    public void StopScaleAnimation()
    {
        DebugLog("Stopping scale animation");
        KillActiveTween();
        transform.localScale = originalScale;
    }
}
