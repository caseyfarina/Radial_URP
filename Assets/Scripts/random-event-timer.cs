using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Simple timer that triggers a Unity Event at random intervals.
/// </summary>
public class RandomEventTimer : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("Minimum time between events (seconds)")]
    [SerializeField, Range(0.1f, 60f)] private float minTime = 1f;

    [Tooltip("Maximum time between events (seconds)")]
    [SerializeField, Range(0.1f, 120f)] private float maxTime = 5f;

    [Header("Event")]
    [Tooltip("Event to trigger at random intervals")]
    [SerializeField] private UnityEvent onTimerTriggered;

    [Header("Debug")]
    [SerializeField] private bool logTimings = false;

    // Internal timer tracking
    private Coroutine timerCoroutine;

    private void OnEnable()
    {
        // Start timer when object is enabled
        StartTimer();
    }

    private void OnDisable()
    {
        // Stop timer when object is disabled
        StopTimer();
    }

    /// <summary>
    /// Starts the random timer
    /// </summary>
    public void StartTimer()
    {
        // Make sure any existing timer is stopped first
        StopTimer();

        // Start new timer coroutine
        timerCoroutine = StartCoroutine(TimerCoroutine());

        if (logTimings)
        {
            Debug.Log($"[RandomEventTimer] Timer started");
        }
    }

    /// <summary>
    /// Stops the random timer
    /// </summary>
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;

            if (logTimings)
            {
                Debug.Log($"[RandomEventTimer] Timer stopped");
            }
        }
    }

    /// <summary>
    /// Triggers the event immediately
    /// </summary>
    public void TriggerEventNow()
    {
        if (onTimerTriggered != null)
        {
            onTimerTriggered.Invoke();

            if (logTimings)
            {
                Debug.Log($"[RandomEventTimer] Event triggered manually");
            }
        }
    }

    /// <summary>
    /// Coroutine that handles the random timing
    /// </summary>
    private IEnumerator TimerCoroutine()
    {
        while (true) // Run indefinitely while enabled
        {
            // Calculate random wait time
            float waitTime = Random.Range(minTime, maxTime);

            if (logTimings)
            {
                Debug.Log($"[RandomEventTimer] Waiting for {waitTime:F2} seconds");
            }

            // Wait for the random interval
            yield return new WaitForSeconds(waitTime);

            // Trigger the event
            if (onTimerTriggered != null)
            {
                onTimerTriggered.Invoke();

                if (logTimings)
                {
                    Debug.Log($"[RandomEventTimer] Event triggered after {waitTime:F2} seconds");
                }
            }
        }
    }

    // For editor testing
    [ContextMenu("Trigger Event Now")]
    private void EditorTriggerEvent()
    {
        TriggerEventNow();
    }
}