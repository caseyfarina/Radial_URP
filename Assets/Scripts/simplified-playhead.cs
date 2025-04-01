using UnityEngine;

/// <summary>
/// Simplified playhead that triggers events on objects that enter the trigger zone
/// </summary>
public class _playhead : MonoBehaviour
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
        DebugLog("_playhead script initialized");
        DebugLog($"Target tag: {targetTag}");
        DebugLog($"Enter: {respondOnEnter}, Exit: {respondOnExit}, Stay: {respondOnStay}");
        
        // Check if this object actually has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("ERROR: No Collider found on _playhead object!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("ERROR: Collider on _playhead object is not set as a trigger!");
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
        // Find _playheadEventRelay component and trigger its event
        _playheadEventRelay eventRelay = targetObject.GetComponent<_playheadEventRelay>();
        if (eventRelay != null)
        {
            DebugLog($"_playheadEventRelay found on {targetObject.name}, invoking event");
            eventRelay.OnTriggerActivated.Invoke();
        }
        else
        {
            // Check children if not found on parent
            eventRelay = targetObject.GetComponentInChildren<_playheadEventRelay>();
            if (eventRelay != null)
            {
                DebugLog($"_playheadEventRelay found in children of {targetObject.name}, invoking event");
                eventRelay.OnTriggerActivated.Invoke();
            }
            else
            {
                DebugLog($"No _playheadEventRelay found on {targetObject.name} or its children");
            }
        }
    }
    
    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[_playhead] {message}");
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visualize the trigger zone in the Scene view
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                // Approximate capsule visualization
                Vector3 size = new Vector3(
                    capsuleCol.radius * 2,
                    capsuleCol.height,
                    capsuleCol.radius * 2
                );
                Gizmos.DrawCube(capsuleCol.center, size);
            }
        }
    }
}