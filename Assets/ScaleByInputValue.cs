using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;


sealed class midiInputToEvent
: MonoBehaviour
{
   // [SerializeField] Transform _transform = null;
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
       // _transform.localScale = Vector3.one * ctx.ReadValue<float>();
       // Debug.Log(ctx.ReadValue<float>());
        myevent.Invoke();
    }
}
