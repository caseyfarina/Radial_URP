using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Relays trigger events from the playhead to individual effect components via UnityEvents.
/// Supports both single trigger mode and multi-trigger mode with configurable parameters.
/// </summary>
public class PlayheadEventRelay : MonoBehaviour
{
    [Header("Event Settings")]
    [Tooltip("This event is invoked when triggered by the playhead")]
    public UnityEvent OnTriggerActivated = new UnityEvent();

    [Header("Mode Settings")]
    [Tooltip("Set to true for multi-trigger mode, false for single trigger mode")]
    [SerializeField] private bool multiTriggerMode = false;

    [Header("Multi-Trigger Settings")]
    [Tooltip("Minimum number of consecutive triggers in multi-trigger mode")]
    [SerializeField, Range(1, 10)] private int minConsecutiveTriggers = 3;

    [Tooltip("Maximum number of consecutive triggers in multi-trigger mode")]
    [SerializeField, Range(1, 20)] private int maxConsecutiveTriggers = 5;

    [Tooltip("Time between consecutive triggers in seconds")]
    [SerializeField, Range(0.05f, 1f)] private float timeBetweenTriggers = 0.2f;

    [Tooltip("Random variation percentage for time between triggers (0-100%)")]
    [SerializeField, Range(0f, 100f)] private float timeRandomizationPercent = 2f;

    [Header("Activation Cooldown")]
    [Tooltip("Time in seconds before the relay can be activated again")]
    [SerializeField, Range(0f, 5f)] private float activationCooldown = 0.3f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;

    // Track the time when we can activate again
    private float nextActivationTime = 0f;

    // Flag to prevent multiple multi-trigger sequences running simultaneously
    private bool isMultiTriggerRunning = false;

    private void Awake()
    {
        if (OnTriggerActivated.GetPersistentEventCount() == 0)
        {
            DebugLog("WARNING: No event listeners have been assigned in the Inspector!");
        }
    }

    private void Start()
    {
        DebugLog($"_playheadEventRelay initialized with {OnTriggerActivated.GetPersistentEventCount()} event listeners");
        DebugLog($"Mode: {(multiTriggerMode ? "Multi-Trigger" : "Single Trigger")}");
        DebugLog($"Activation cooldown: {activationCooldown} seconds");
    }

    /// <summary>
    /// Public method to be called by the playhead to activate this relay object
    /// </summary>
    public void ActivateObject()
    {
        // Check if we're still in activation cooldown
        if (Time.time < nextActivationTime)
        {
            float remainingCooldown = nextActivationTime - Time.time;
            DebugLog($"Activation attempted but still in cooldown: {remainingCooldown:F2} seconds remaining");
            return;
        }

        // Set next activation time based on cooldown
        nextActivationTime = Time.time + activationCooldown;

        // Determine which mode to use
        if (multiTriggerMode)
        {
            // Only start a new multi-trigger sequence if one isn't already running
            if (!isMultiTriggerRunning)
            {
                StartCoroutine(ExecuteMultiTrigger());
            }
            else
            {
                DebugLog("Multi-trigger sequence already running, ignoring activation");
            }
        }
        else
        {
            // Single trigger mode - just invoke the event once
            DebugLog("Single trigger activated");
            OnTriggerActivated.Invoke();
        }
    }

    /// <summary>
    /// Coroutine to handle multi-trigger sequence
    /// </summary>
    private IEnumerator ExecuteMultiTrigger()
    {
        isMultiTriggerRunning = true;

        // Determine number of triggers for this sequence
        int triggerCount = Random.Range(minConsecutiveTriggers, maxConsecutiveTriggers + 1);
        DebugLog($"Starting multi-trigger sequence with {triggerCount} triggers");

        for (int i = 0; i < triggerCount; i++)
        {
            // Invoke the event
            DebugLog($"Multi-trigger {i + 1}/{triggerCount}");
            OnTriggerActivated.Invoke();

            // Skip delay after the last trigger
            if (i < triggerCount - 1)
            {
                // Calculate randomized delay
                float randomizationFactor = 1f + Random.Range(-timeRandomizationPercent, timeRandomizationPercent) / 100f;
                float delay = timeBetweenTriggers * randomizationFactor;

                DebugLog($"Waiting {delay:F3}s before next trigger");
                yield return new WaitForSeconds(delay);
            }
        }

        DebugLog("Multi-trigger sequence complete");
        isMultiTriggerRunning = false;
    }

    /// <summary>
    /// Sets the mode to single trigger or multi-trigger
    /// </summary>
    /// <param name="useMultiTrigger">True for multi-trigger mode, false for single trigger mode</param>
    public void SetMultiTriggerMode(bool useMultiTrigger)
    {
        multiTriggerMode = useMultiTrigger;
        DebugLog($"Mode set to: {(multiTriggerMode ? "Multi-Trigger" : "Single Trigger")}");
    }

    /// <summary>
    /// Sets the range for the number of consecutive triggers in multi-trigger mode
    /// </summary>
    /// <param name="min">Minimum number of triggers</param>
    /// <param name="max">Maximum number of triggers</param>
    public void SetConsecutiveTriggerRange(int min, int max)
    {
        minConsecutiveTriggers = Mathf.Max(1, min);
        maxConsecutiveTriggers = Mathf.Max(minConsecutiveTriggers, max);
        DebugLog($"Consecutive trigger range set to {minConsecutiveTriggers}-{maxConsecutiveTriggers}");
    }

    /// <summary>
    /// Sets the time between consecutive triggers in multi-trigger mode
    /// </summary>
    /// <param name="time">Time in seconds</param>
    public void SetTimeBetweenTriggers(float time)
    {
        timeBetweenTriggers = Mathf.Max(0.05f, time);
        DebugLog($"Time between triggers set to {timeBetweenTriggers} seconds");
    }

    /// <summary>
    /// Sets the randomization percentage for timing between consecutive triggers
    /// </summary>
    /// <param name="percent">Percentage of randomization (0-100)</param>
    public void SetTimeRandomizationPercent(float percent)
    {
        timeRandomizationPercent = Mathf.Clamp(percent, 0f, 100f);
        DebugLog($"Time randomization set to {timeRandomizationPercent}%");
    }

    /// <summary>
    /// Sets a new activation cooldown duration
    /// </summary>
    /// <param name="duration">New cooldown duration in seconds</param>
    public void SetActivationCooldown(float duration)
    {
        activationCooldown = Mathf.Max(0f, duration);
        DebugLog($"Activation cooldown set to {activationCooldown} seconds");
    }

    /// <summary>
    /// Cancels any running multi-trigger sequence
    /// </summary>
    public void CancelMultiTrigger()
    {
        if (isMultiTriggerRunning)
        {
            StopAllCoroutines();
            isMultiTriggerRunning = false;
            DebugLog("Multi-trigger sequence canceled");
        }
    }

    /// <summary>
    /// Resets the activation cooldown, allowing immediate activation
    /// </summary>
    public void ResetCooldown()
    {
        nextActivationTime = 0f;
        DebugLog("Activation cooldown reset");
    }

    /// <summary>
    /// Returns true if the relay is currently in cooldown
    /// </summary>
    public bool IsInCooldown()
    {
        return Time.time < nextActivationTime;
    }

    /// <summary>
    /// Returns true if a multi-trigger sequence is currently running
    /// </summary>
    public bool IsMultiTriggerRunning()
    {
        return isMultiTriggerRunning;
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PlayheadEventRelay: {gameObject.name}] {message}");
        }
    }
}