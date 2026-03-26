using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

sealed class MidiInputToEvent : MonoBehaviour
{
    [SerializeField] InputAction _action = null;
    public UnityEvent myevent;

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
        myevent.Invoke();
    }
}
