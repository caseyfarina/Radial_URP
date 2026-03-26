using UnityEngine;
using DG.Tweening;

/// <summary>
/// Base class for DOTween-based animation effects.
/// Provides shared tween lifecycle management, debug logging, and loop configuration.
/// </summary>
public abstract class DOTweenEffectBase : MonoBehaviour
{
    [Header("Base Effect Settings")]
    [SerializeField, Range(-1, 10)] protected int defaultLoopCount = 0;
    [SerializeField] protected bool debugMode = false;

    protected Tween activeTween;

    protected virtual void OnDestroy()
    {
        KillActiveTween();
    }

    /// <summary>
    /// Kills the active tween if it exists and is active.
    /// </summary>
    protected void KillActiveTween()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
            activeTween = null;
        }
    }

    /// <summary>
    /// Kills any active tween and prepares for a new animation.
    /// Returns true if a previous tween was interrupted.
    /// </summary>
    protected bool PrepareForNewTween()
    {
        bool wasActive = activeTween != null && activeTween.IsActive();
        if (wasActive)
        {
            DebugLog("Interrupting active animation, restarting...");
            activeTween.Kill();
            activeTween = null;
        }
        return wasActive;
    }

    protected void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }
}
