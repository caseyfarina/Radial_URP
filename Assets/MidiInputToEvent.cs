using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System;

[Serializable]
public class InputActionEventPair
{
    [Tooltip("Input Action that will trigger the event")]
    public InputAction action;

    [Tooltip("Event to trigger when the action is performed")]
    public UnityEvent onActionPerformed;

    [Tooltip("Optional name to identify this action-event pair")]
    public string displayName;

    [Tooltip("Whether to pass the input value to the event (requires UnityEvent<float>)")]
    public bool passValueToEvent = false;

    [Tooltip("Event that will receive the input value")]
    public UnityEvent<float> onActionPerformedWithValue;
}

/// <summary>
/// Routes multiple Input System actions to Unity Events
/// </summary>
public class MidiInputToEvent : MonoBehaviour
{
    [SerializeField, Tooltip("List of action and event pairs")]
    private InputActionEventPair[] actionEventPairs = new InputActionEventPair[8];

    [Header("Debug Options")]
    [SerializeField] private bool logActionValues = false;

    private void OnEnable()
    {
        // Subscribe to and enable all valid actions
        foreach (var pair in actionEventPairs)
        {
            if (pair != null && pair.action != null)
            {
                pair.action.performed += OnActionPerformed;
                pair.action.Enable();
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from and disable all valid actions
        foreach (var pair in actionEventPairs)
        {
            if (pair != null && pair.action != null)
            {
                pair.action.performed -= OnActionPerformed;
                pair.action.Disable();
            }
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        // Find which action was triggered
        foreach (var pair in actionEventPairs)
        {
            if (pair != null && pair.action == context.action)
            {
                // Invoke the basic event
                pair.onActionPerformed?.Invoke();

                // If configured to pass the value, read and pass it to the value event
                if (pair.passValueToEvent)
                {
                    float value = context.ReadValue<float>();
                    pair.onActionPerformedWithValue?.Invoke(value);

                    if (logActionValues)
                    {
                        string actionName = !string.IsNullOrEmpty(pair.displayName)
                            ? pair.displayName
                            : pair.action.name;

                        Debug.Log($"Action {actionName} performed with value: {value}");
                    }
                }

                break; // Exit the loop once we found the matching action
            }
        }
    }

    /// <summary>
    /// Utility method to manually trigger an event by index
    /// </summary>
    /// <param name="index">Index of the action-event pair (0-7)</param>
    public void TriggerEventByIndex(int index)
    {
        if (index >= 0 && index < actionEventPairs.Length && actionEventPairs[index] != null)
        {
            actionEventPairs[index].onActionPerformed?.Invoke();
        }
    }

    /// <summary>
    /// Enable a specific action by index
    /// </summary>
    /// <param name="index">Index of the action to enable (0-7)</param>
    public void EnableAction(int index)
    {
        if (index >= 0 && index < actionEventPairs.Length &&
            actionEventPairs[index] != null &&
            actionEventPairs[index].action != null)
        {
            actionEventPairs[index].action.Enable();
        }
    }

    /// <summary>
    /// Disable a specific action by index
    /// </summary>
    /// <param name="index">Index of the action to disable (0-7)</param>
    public void DisableAction(int index)
    {
        if (index >= 0 && index < actionEventPairs.Length &&
            actionEventPairs[index] != null &&
            actionEventPairs[index].action != null)
        {
            actionEventPairs[index].action.Disable();
        }
    }
}