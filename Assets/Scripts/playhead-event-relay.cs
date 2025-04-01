using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Relays trigger events from the playhead to individual effect components via UnityEvents
/// </summary>
public class _playheadEventRelay : MonoBehaviour
{
    [Header("Event Settings")]
    [Tooltip("This event is invoked when triggered by the playhead")]
    public UnityEvent OnTriggerActivated = new UnityEvent();
    
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;
    
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
    }
    
    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[_playheadEventRelay: {gameObject.name}] {message}");
        }
    }
}