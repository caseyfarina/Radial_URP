using UnityEngine;

/// <summary>
/// Simplified playhead that triggers events on objects that enter the trigger zone
/// </summary>
public class Playhead : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string targetTag = "Entity";
    [SerializeField] private bool debugMode = true;

    [Header("Trigger Response Settings")]
    [SerializeField] private bool respondOnEnter = true;
    [SerializeField] private bool respondOnExit = false;
    [SerializeField] private bool respondOnStay = false;
    [SerializeField, Range(0f, 10f)] private float stayInterval = 2f;

    private float nextStayResponseTime;

    private void Awake()
    {
        // Check for required components
        if (GetComponent<Rigidbody>() == null && GetComponent<Collider>() != null)
        {
            Debug.LogWarning("Adding Rigidbody component as it's required for trigger detection");
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Don't let physics move the object
            rb.useGravity = false; // Don't let gravity affect it
        }
    }

    private void Start()
    {
        nextStayResponseTime = Time.time;
        DebugLog("Playhead script initialized");
        DebugLog($"Target tag: {targetTag}");
        DebugLog($"Enter: {respondOnEnter}, Exit: {respondOnExit}, Stay: {respondOnStay}");

        // Check if this object actually has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("ERROR: No Collider found on Playhead object!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("ERROR: Collider on Playhead object is not set as a trigger!");
        }
        else
        {
            DebugLog("Collider check: OK");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DebugLog($"OnTriggerEnter detected with {other.name}, tag: {other.tag}");

        if (other.CompareTag(targetTag))
        {
            DebugLog("Tag match: YES");

            if (respondOnEnter)
            {
                DebugLog("Respond on enter: YES - Triggering event");
                TriggerEventOnObject(other.gameObject);
            }
            else
            {
                DebugLog("Respond on enter: NO - No actions taken");
            }
        }
        else
        {
            DebugLog($"Tag match: NO (Expected '{targetTag}', got '{other.tag}')");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DebugLog($"OnTriggerExit detected with {other.name}, tag: {other.tag}");

        if (other.CompareTag(targetTag))
        {
            DebugLog("Tag match: YES");

            if (respondOnExit)
            {
                DebugLog("Respond on exit: YES - Triggering event");
                TriggerEventOnObject(other.gameObject);
            }
            else
            {
                DebugLog("Respond on exit: NO - No actions taken");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (respondOnStay && other.CompareTag(targetTag) && Time.time >= nextStayResponseTime)
        {
            DebugLog("Respond on stay: YES - Triggering event");
            TriggerEventOnObject(other.gameObject);
            nextStayResponseTime = Time.time + stayInterval;
        }
    }

    private void TriggerEventOnObject(GameObject targetObject)
    {
        // Find PlayheadEventRelay component and call its ActivateObject method
        PlayheadEventRelay eventRelay = targetObject.GetComponent<PlayheadEventRelay>();
        if (eventRelay != null)
        {
            DebugLog($"PlayheadEventRelay found on {targetObject.name}, calling ActivateObject");
            eventRelay.ActivateObject();
        }
        else
        {
            // Check children if not found on parent
            eventRelay = targetObject.GetComponentInChildren<PlayheadEventRelay>();
            if (eventRelay != null)
            {
                DebugLog($"PlayheadEventRelay found in children of {targetObject.name}, calling ActivateObject");
                eventRelay.ActivateObject();
            }
            else
            {
                DebugLog($"No PlayheadEventRelay found on {targetObject.name} or its children");
            }
        }
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Playhead] {message}");
        }
    }


}