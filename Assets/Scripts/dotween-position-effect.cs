using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls position animation effects using DOTween for improved performance
/// </summary>
public class PositionEffect : MonoBehaviour
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
    [SerializeField, Range(-1, 10)] private int defaultLoopCount = 0; // -1 for infinite, 0 for single play, >0 for specific loop count
    [SerializeField] private bool debugMode = false;

    private Tween activePositionTween;
    private Vector3 originalPosition;

    private void Awake()
    {
        // Store original position for reference
        originalPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        // Clean up any active tweens when destroyed
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            activePositionTween.Kill();
            activePositionTween = null;
        }
    }

    /// <summary>
    /// Plays a position animation using DOTween, moving along specified axes and then returning with default loop count
    /// </summary>
    public void PlayMove()
    {
        PlayMoveWithLoops(defaultLoopCount);
    }

    /// <summary>
    /// Plays a position animation using DOTween, moving along specified axes and then returning with specified loop count
    /// </summary>
    /// <param name="loopCount">Number of times to repeat the animation. -1 for infinite loops.</param>
    public void PlayMoveWithLoops(int loopCount)
    {
        DebugLog($"PlayMove called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        // If already moving, stop current animation
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Already moving, restarting...");
            activePositionTween.Kill();
            activePositionTween = null;
        }

        // Update original position in case it changed
        originalPosition = transform.localPosition;

        // Calculate target position based on move axes and distance
        Vector3 targetPosition = CalculateTargetPosition(moveDistance);

        DebugLog($"Moving from {originalPosition} to {targetPosition}");

        // Create a sequence for the full animation
        Sequence moveSequence = DOTween.Sequence();

        // Add movement tweens
        moveSequence.Append(
            transform.DOLocalMove(targetPosition, moveDuration / 2)
                .SetEase(moveCurve)
        );

        moveSequence.Append(
            transform.DOLocalMove(originalPosition, moveDuration / 2)
                .SetEase(moveCurve)
        );

        // Set loop count if needed
        if (loopCount != 0)
        {
            moveSequence.SetLoops(loopCount);
        }

        // Set callbacks
        moveSequence.OnComplete(() => {
            DebugLog("Position animation complete");
            transform.localPosition = originalPosition; // Ensure we end at original position
            activePositionTween = null;
        });

        // Store the sequence as the active tween
        activePositionTween = moveSequence;
    }

    /// <summary>
    /// Plays a position animation using DOTween with a custom distance and default loop count
    /// </summary>
    /// <param name="distance">Distance to move (overrides the default moveDistance)</param>
    public void PlayMoveWithDistance(float distance)
    {
        PlayMoveWithDistanceAndLoops(distance, defaultLoopCount);
    }

    /// <summary>
    /// Plays a position animation using DOTween with a custom distance and specified loop count
    /// </summary>
    /// <param name="distance">Distance to move (overrides the default moveDistance)</param>
    /// <param name="loopCount">Number of times to repeat the animation. -1 for infinite loops.</param>
    public void PlayMoveWithDistanceAndLoops(float distance, int loopCount)
    {
        DebugLog($"PlayMoveWithDistance called with distance: {distance}, loops: {(loopCount == -1 ? "infinite" : loopCount.ToString())}");

        // If already moving, stop current animation
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Already moving, restarting...");
            activePositionTween.Kill();
            activePositionTween = null;
        }

        // Update original position in case it changed
        originalPosition = transform.localPosition;

        // Calculate target position based on move axes and custom distance
        Vector3 targetPosition = CalculateTargetPosition(distance);

        DebugLog($"Moving from {originalPosition} to {targetPosition}");

        // Create a sequence for the full animation
        Sequence moveSequence = DOTween.Sequence();

        // Add movement tweens
        moveSequence.Append(
            transform.DOLocalMove(targetPosition, moveDuration / 2)
                .SetEase(moveCurve)
        );

        moveSequence.Append(
            transform.DOLocalMove(originalPosition, moveDuration / 2)
                .SetEase(moveCurve)
        );

        // Set loop count if needed
        if (loopCount != 0)
        {
            moveSequence.SetLoops(loopCount);
        }

        // Set callbacks
        moveSequence.OnComplete(() => {
            DebugLog("Position animation complete");
            transform.localPosition = originalPosition; // Ensure we end at original position
            activePositionTween = null;
        });

        // Store the sequence as the active tween
        activePositionTween = moveSequence;
    }

    /// <summary>
    /// Plays an infinite floating animation with customizable settings
    /// </summary>
    /// <param name="amplitude">Amplitude of the float movement</param>
    /// <param name="period">Time in seconds for one complete cycle</param>
    public void PlayFloatingAnimation(float amplitude, float period)
    {
        PlayFloatingAnimationWithDuration(amplitude, period, -1);
    }

    /// <summary>
    /// Plays a floating animation with customizable settings and specified duration
    /// </summary>
    /// <param name="amplitude">Amplitude of the float movement</param>
    /// <param name="period">Time in seconds for one complete cycle</param>
    /// <param name="duration">Duration of the animation in seconds. Use -1 for infinite.</param>
    public void PlayFloatingAnimationWithDuration(float amplitude, float period, float duration)
    {
        DebugLog($"PlayFloatingAnimation called with amplitude: {amplitude}, period: {period}, duration: {(duration < 0 ? "infinite" : duration.ToString())}");

        // If already moving, stop current animation
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Already moving, restarting...");
            activePositionTween.Kill();
            activePositionTween = null;
        }

        // Update original position in case it changed
        originalPosition = transform.localPosition;

        // Create a sequence for more complex floating animation
        Sequence floatSequence = DOTween.Sequence();

        // Get the actual movement amount based on selected axes
        Vector3 moveAmount = new Vector3(
            (moveAxis.HasFlag(MoveAxis.X) ? amplitude : 0),
            (moveAxis.HasFlag(MoveAxis.Y) ? amplitude : 0),
            (moveAxis.HasFlag(MoveAxis.Z) ? amplitude : 0)
        );

        // First move up
        floatSequence.Append(
            transform.DOLocalMove(originalPosition + moveAmount, period / 2)
                .SetEase(Ease.InOutSine)
        );

        // Then move down
        floatSequence.Append(
            transform.DOLocalMove(originalPosition - moveAmount, period / 2)
                .SetEase(Ease.InOutSine)
        );

        // Calculate number of loops needed for the duration
        int loops = (duration < 0) ? -1 : Mathf.CeilToInt(duration / period);

        // Set to loop
        floatSequence.SetLoops(loops, LoopType.Yoyo);

        // Set callbacks
        floatSequence.OnComplete(() => {
            DebugLog("Floating animation complete");
            transform.localPosition = originalPosition; // Ensure we end at original position
            activePositionTween = null;
        });

        // Store the sequence as the active tween
        activePositionTween = floatSequence;
    }

    /// <summary>
    /// Moves to a specific position over time
    /// </summary>
    /// <param name="targetPosition">Target position (local space)</param>
    /// <param name="duration">Duration of the movement</param>
    public void MoveToPosition(Vector3 targetPosition, float duration)
    {
        DebugLog($"MoveToPosition called: to {targetPosition} over {duration} seconds");

        // If already moving, stop current animation
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Already moving, restarting...");
            activePositionTween.Kill();
            activePositionTween = null;
        }

        // Filter target position based on move axes to maintain original position on unused axes
        Vector3 filteredTarget = new Vector3(
            moveAxis.HasFlag(MoveAxis.X) ? targetPosition.x : transform.localPosition.x,
            moveAxis.HasFlag(MoveAxis.Y) ? targetPosition.y : transform.localPosition.y,
            moveAxis.HasFlag(MoveAxis.Z) ? targetPosition.z : transform.localPosition.z
        );

        // Create the movement tween
        activePositionTween = transform.DOLocalMove(filteredTarget, duration)
            .SetEase(moveCurve)
            .OnComplete(() => {
                DebugLog("MoveToPosition complete");
                activePositionTween = null;
            });
    }

    /// <summary>
    /// Moves by a relative offset over time
    /// </summary>
    /// <param name="offset">Position offset to add (local space)</param>
    /// <param name="duration">Duration of the movement</param>
    public void MoveByOffset(Vector3 offset, float duration)
    {
        DebugLog($"MoveByOffset called: by {offset} over {duration} seconds");

        // If already moving, stop current animation
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Already moving, restarting...");
            activePositionTween.Kill();
            activePositionTween = null;
        }

        // Filter offset based on move axes
        Vector3 filteredOffset = new Vector3(
            moveAxis.HasFlag(MoveAxis.X) ? offset.x : 0,
            moveAxis.HasFlag(MoveAxis.Y) ? offset.y : 0,
            moveAxis.HasFlag(MoveAxis.Z) ? offset.z : 0
        );

        // Create the movement tween - DOTween doesn't have DOLocalMoveBy, so use DOLocalMove with current + offset
        Vector3 targetPosition = transform.localPosition + filteredOffset;
        activePositionTween = transform.DOLocalMove(targetPosition, duration)
            .SetEase(moveCurve)
            .OnComplete(() => {
                DebugLog("MoveByOffset complete");
                activePositionTween = null;
            });
    }

    /// <summary>
    /// Stops any active position animation
    /// </summary>
    public void StopMovement()
    {
        if (activePositionTween != null && activePositionTween.IsActive())
        {
            DebugLog("Stopping position animation");
            activePositionTween.Kill();
            activePositionTween = null;
        }
    }

    /// <summary>
    /// Calculates target position based on selected axes and distance
    /// </summary>
    private Vector3 CalculateTargetPosition(float distance)
    {
        Vector3 target = originalPosition;

        // Apply movement on selected axes
        if (moveAxis.HasFlag(MoveAxis.X))
        {
            target.x += distance;
        }

        if (moveAxis.HasFlag(MoveAxis.Y))
        {
            target.y += distance;
        }

        if (moveAxis.HasFlag(MoveAxis.Z))
        {
            target.z += distance;
        }

        return target;
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PositionEffect] {message}");
        }
    }
}