using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;


sealed class midiInputValueToEvent : MonoBehaviour
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
    { //_transform.localScale = Vector3.one * ctx.ReadValue<float>();
        //transform.localScale = Vector3.one * ctx.ReadValue<float>();
        MyFloatEvent.Invoke(ctx.ReadValue<float>());
        float test = ctx.ReadValue<float>();
        Debug.Log(test.ToString());
    }
}
