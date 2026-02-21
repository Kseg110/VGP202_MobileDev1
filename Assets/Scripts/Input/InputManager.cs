using Unity.VisualScripting;
using UnityEngine;

public class InputManager : Singleton<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions input;
    public event System.Action<Vector2> OnMoveEvent;
    public event System.Action<bool> OnJumpEvent;
    public event System.Action<bool> OnDropEvent;

    #region Input Action Callbacks
    public void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnCrouch(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context) => OnJumpEvent?.Invoke(context.ReadValueAsButton());
    public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
            return;
        }

        OnMoveEvent?.Invoke(Vector2.zero);
    }

    public void OnNext(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPrevious(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnDrop(UnityEngine.InputSystem.InputAction.CallbackContext context) => OnDropEvent?.Invoke(context.ReadValueAsButton());
    #endregion

    #region Input Callback Setup
    void Awake()
    {
        input = new InputSystem_Actions();
        input.Player.SetCallbacks(this);
    }
    void OnEnable()
    {
        input.Enable();
    }
    void OnDisable()
    {
        input.Disable();
    }

    void OnDestroy()
    {
        input.Dispose();
    }
    #endregion
}
