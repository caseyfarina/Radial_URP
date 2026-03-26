using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

sealed class MidiInputValueToEvent : MonoBehaviour
{
    [SerializeField] InputAction _action = null;
    [SerializeField] UnityEvent<float> MyFloatEvent;

    void OnEnable()
    {
        _action.performed += OnPerformed;
        _action.Enable();
    }

    void OnDisable()
    {
        _action.performed -= OnPerformed;
        _action.Disable();
    }

    void OnPerformed(InputAction.CallbackContext ctx)
    {
        MyFloatEvent.Invoke(ctx.ReadValue<float>());
    }
}
