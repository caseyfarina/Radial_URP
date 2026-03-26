using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple script that allows toggling the active state of a GameObject.
/// Can be controlled through a public method, unity events, or key press.
/// </summary>
public class GameObjectToggler : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField, Tooltip("The GameObject to toggle on/off")]
    private GameObject targetObject;
    
    [SerializeField, Tooltip("Whether to automatically find the target if not assigned")]
    private bool findTargetIfNull = false;
    
    [SerializeField, Tooltip("Path/name to search for if finding target automatically")]
    private string targetPath = "TargetObject";
    
    [Header("Toggle Options")]
    [SerializeField, Tooltip("Optional key to toggle when pressed")]
    private KeyCode toggleKey = KeyCode.None;
    
    [SerializeField, Tooltip("Whether to toggle on component start")]
    private bool toggleOnStart = false;
    
    [SerializeField, Tooltip("Whether to toggle on component enable")]
    private bool toggleOnEnable = false;
    
    [Header("Events")]
    [SerializeField, Tooltip("Event triggered when the target becomes active")]
    private UnityEvent onTargetEnabled;
    
    [SerializeField, Tooltip("Event triggered when the target becomes inactive")]
    private UnityEvent onTargetDisabled;
    
    private void Start()
    {
        InitializeTarget();
        
        if (toggleOnStart)
        {
            ToggleTarget();
        }
    }
    
    private void OnEnable()
    {
        if (toggleOnEnable && targetObject != null)
        {
            ToggleTarget();
        }
    }
    
    private void Update()
    {
        // Check for key press if a toggle key is assigned
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            ToggleTarget();
        }
    }
    
    private void InitializeTarget()
    {
        // Try to find the target if not manually assigned
        if (targetObject == null && findTargetIfNull)
        {
            // First try to find by path/name
            GameObject foundObject = GameObject.Find(targetPath);
            
            if (foundObject != null)
            {
                targetObject = foundObject;
                Debug.Log($"GameObjectToggler: Found target at {targetPath}");
            }
            else
            {
                Debug.LogWarning($"GameObjectToggler: Could not find GameObject at {targetPath}");
            }
        }
    }
    
    /// <summary>
    /// Toggles the active state of the target GameObject
    /// </summary>
    public void ToggleTarget()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("GameObjectToggler: No target GameObject assigned to toggle");
            return;
        }
        
        // Toggle active state
        bool newState = !targetObject.activeSelf;
        targetObject.SetActive(newState);
        
        // Trigger appropriate event
        if (newState)
        {
            onTargetEnabled?.Invoke();
        }
        else
        {
            onTargetDisabled?.Invoke();
        }
    }
    
    /// <summary>
    /// Sets the target GameObject that will be toggled
    /// </summary>
    /// <param name="newTarget">New target GameObject</param>
    public void SetTarget(GameObject newTarget)
    {
        targetObject = newTarget;
    }
    
    /// <summary>
    /// Gets the current state of the target
    /// </summary>
    /// <returns>True if target is active, false otherwise</returns>
    public bool IsTargetActive()
    {
        return targetObject != null && targetObject.activeSelf;
    }
}
