using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls position animation effects using DOTween for improved performance
/// </summary>
public class PositionEffect : DOTweenEffectBase
{
    [Header("Position Settings")]
    [SerializeField, Range(0.01f, 10f)] private float moveDistance = 1f;
    [SerializeField, Range(0.1f, 5f)] private float moveDuration = 1f;

    [System.Flags]
    public enum MoveAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    [SerializeField] private MoveAxis moveAxis = MoveAxis.Y;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// Plays a position animation using DOTween with default loop count
    /// </summary>
    public void PlayMove()
    {
        PlayMoveWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Plays a position animation with specified loop count
    /// </summary>
    public void PlayMoveWithLoops(int loopCount)
    {
        DebugLog($"PlayMove called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        PrepareForNewTween();
        originalPosition = transform.localPosition;

        Vector3 targetPosition = CalculateTargetPosition(moveDistance);
        DebugLog($"Moving from {originalPosition} to {targetPosition}");

        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOLocalMove(targetPosition, moveDuration / 2).SetEase(moveCurve));
        moveSequence.Append(transform.DOLocalMove(originalPosition, moveDuration / 2).SetEase(moveCurve));

        if (loopCount != 0) moveSequence.SetLoops(loopCount);

        moveSequence.OnComplete(() => {
            DebugLog("Position animation complete");
            transform.localPosition = originalPosition;
            activeTween = null;
        });

        activeTween = moveSequence;
    }

    /// <summary>
    /// Plays a position animation with a custom distance and default loop count
    /// </summary>
    public void PlayMoveWithDistance(float distance)
    {
        PlayMoveWithDistanceAndLoops(distance, defaultLoopCount);
    }

    /// <summary>
    /// Plays a position animation with a custom distance and specified loop count
    /// </summary>
    public void PlayMoveWithDistanceAndLoops(float distance, int loopCount)
    {
        DebugLog($"PlayMoveWithDistance called with distance: {distance}, loops: {(loopCount == -1 ? "infinite" : loopCount.ToString())}");

        PrepareForNewTween();
        originalPosition = transform.localPosition;

        Vector3 targetPosition = CalculateTargetPosition(distance);
        DebugLog($"Moving from {originalPosition} to {targetPosition}");

        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOLocalMove(targetPosition, moveDuration / 2).SetEase(moveCurve));
        moveSequence.Append(transform.DOLocalMove(originalPosition, moveDuration / 2).SetEase(moveCurve));

        if (loopCount != 0) moveSequence.SetLoops(loopCount);

        moveSequence.OnComplete(() => {
            DebugLog("Position animation complete");
            transform.localPosition = originalPosition;
            activeTween = null;
        });

        activeTween = moveSequence;
    }

    /// <summary>
    /// Plays an infinite floating animation
    /// </summary>
    public void PlayFloatingAnimation(float amplitude, float period)
    {
        PlayFloatingAnimationWithDuration(amplitude, period, -1);
    }

    /// <summary>
    /// Plays a floating animation with specified duration
    /// </summary>
    public void PlayFloatingAnimationWithDuration(float amplitude, float period, float duration)
    {
        DebugLog($"PlayFloatingAnimation called with amplitude: {amplitude}, period: {period}, duration: {(duration < 0 ? "infinite" : duration.ToString())}");

        PrepareForNewTween();
        originalPosition = transform.localPosition;

        Sequence floatSequence = DOTween.Sequence();

        Vector3 moveAmount = new Vector3(
            (moveAxis.HasFlag(MoveAxis.X) ? amplitude : 0),
            (moveAxis.HasFlag(MoveAxis.Y) ? amplitude : 0),
            (moveAxis.HasFlag(MoveAxis.Z) ? amplitude : 0)
        );

        floatSequence.Append(transform.DOLocalMove(originalPosition + moveAmount, period / 2).SetEase(Ease.InOutSine));
        floatSequence.Append(transform.DOLocalMove(originalPosition - moveAmount, period / 2).SetEase(Ease.InOutSine));

        int loops = (duration < 0) ? -1 : Mathf.CeilToInt(duration / period);
        floatSequence.SetLoops(loops, LoopType.Yoyo);

        floatSequence.OnComplete(() => {
            DebugLog("Floating animation complete");
            transform.localPosition = originalPosition;
            activeTween = null;
        });

        activeTween = floatSequence;
    }

    /// <summary>
    /// Moves to a specific position over time
    /// </summary>
    public void MoveToPosition(Vector3 targetPosition, float duration)
    {
        DebugLog($"MoveToPosition called: to {targetPosition} over {duration} seconds");
        PrepareForNewTween();

        Vector3 filteredTarget = new Vector3(
            moveAxis.HasFlag(MoveAxis.X) ? targetPosition.x : transform.localPosition.x,
            moveAxis.HasFlag(MoveAxis.Y) ? targetPosition.y : transform.localPosition.y,
            moveAxis.HasFlag(MoveAxis.Z) ? targetPosition.z : transform.localPosition.z
        );

        activeTween = transform.DOLocalMove(filteredTarget, duration)
            .SetEase(moveCurve)
            .OnComplete(() => {
                DebugLog("MoveToPosition complete");
                activeTween = null;
            });
    }

    /// <summary>
    /// Moves by a relative offset over time
    /// </summary>
    public void MoveByOffset(Vector3 offset, float duration)
    {
        DebugLog($"MoveByOffset called: by {offset} over {duration} seconds");
        PrepareForNewTween();

        Vector3 filteredOffset = new Vector3(
            moveAxis.HasFlag(MoveAxis.X) ? offset.x : 0,
            moveAxis.HasFlag(MoveAxis.Y) ? offset.y : 0,
            moveAxis.HasFlag(MoveAxis.Z) ? offset.z : 0
        );

        Vector3 target = transform.localPosition + filteredOffset;
        activeTween = transform.DOLocalMove(target, duration)
            .SetEase(moveCurve)
            .OnComplete(() => {
                DebugLog("MoveByOffset complete");
                activeTween = null;
            });
    }

    /// <summary>
    /// Stops any active position animation
    /// </summary>
    public void StopMovement()
    {
        DebugLog("Stopping position animation");
        KillActiveTween();
    }

    private Vector3 CalculateTargetPosition(float distance)
    {
        Vector3 target = originalPosition;
        if (moveAxis.HasFlag(MoveAxis.X)) target.x += distance;
        if (moveAxis.HasFlag(MoveAxis.Y)) target.y += distance;
        if (moveAxis.HasFlag(MoveAxis.Z)) target.z += distance;
        return target;
    }
}
