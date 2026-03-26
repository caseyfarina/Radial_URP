using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls rotation animation effects using DOTween for improved performance
/// </summary>
public class RotationEffect : DOTweenEffectBase
{
    [Header("Rotation Settings")]
    [SerializeField, Range(0f, 360f)] private float minRotation = 30f;
    [SerializeField, Range(0f, 360f)] private float maxRotation = 90f;

    public enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    [SerializeField, Range(0.1f, 5f)] private float rotationDuration = 1f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Quaternion originalRotation;

    private void Awake()
    {
        originalRotation = transform.localRotation;
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
    public void PlayRotationWithLoops(int loopCount)
    {
        DebugLog($"PlayRotation called with {(loopCount == -1 ? "infinite" : loopCount.ToString())} loops");

        PrepareForNewTween();

        Quaternion currentRotation = transform.localRotation;

        float randomAngle = Random.Range(minRotation, maxRotation);
        if (Random.value > 0.5f) randomAngle *= -1;

        DebugLog($"Random rotation angle: {randomAngle} degrees on {rotationAxis} axis");

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

        activeTween = transform.DOLocalRotateQuaternion(targetRotation, rotationDuration)
            .SetEase(rotationCurve)
            .OnComplete(() => {
                DebugLog("Rotation animation complete");
                activeTween = null;
            });

        if (loopCount != 0)
        {
            activeTween.SetLoops(loopCount);
        }
    }

    /// <summary>
    /// Plays a continuous rotation animation on a specific axis
    /// </summary>
    public void PlayContinuousRotation(float degreesPerSecond, float duration = -1)
    {
        DebugLog($"PlayContinuousRotation called with {degreesPerSecond} degrees/sec for {(duration < 0 ? "infinite" : duration.ToString())} seconds");

        PrepareForNewTween();

        float totalRotation = duration < 0 ? 100000f : degreesPerSecond * duration;

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

        activeTween = transform.DOLocalRotate(
            rotationVector,
            duration < 0 ? 100000f : duration,
            RotateMode.LocalAxisAdd
        ).SetEase(Ease.Linear);

        if (duration < 0)
        {
            activeTween.SetLoops(-1, LoopType.Restart);
        }

        activeTween.OnComplete(() => {
            DebugLog("Continuous rotation complete");
            activeTween = null;
        });
    }

    /// <summary>
    /// Stops any active rotation animation
    /// </summary>
    public void StopRotation()
    {
        DebugLog("Stopping rotation animation");
        KillActiveTween();
    }

    /// <summary>
    /// Rotates to a specific angle on the configured axis
    /// </summary>
    public void RotateToAngle(float angle, float duration, bool relative = false)
    {
        DebugLog($"RotateToAngle called: {angle} degrees over {duration} seconds (relative: {relative})");

        PrepareForNewTween();

        Vector3 targetEuler = transform.localEulerAngles;

        if (relative)
        {
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

        activeTween = transform.DOLocalRotate(
            targetEuler,
            duration,
            RotateMode.FastBeyond360
        ).SetEase(rotationCurve);

        activeTween.OnComplete(() => {
            DebugLog("RotateToAngle complete");
            activeTween = null;
        });
    }
}
