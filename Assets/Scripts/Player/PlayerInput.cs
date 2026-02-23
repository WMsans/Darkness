using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput
{
    private InputSystem_Actions _actions;

    public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => _actions.Player.Look.ReadValue<Vector2>();
    public bool JumpPressed => _actions.Player.Jump.WasPressedThisFrame();
    public bool SprintHeld => _actions.Player.Sprint.IsPressed();
    public bool SneakHeld => _actions.Player.Crouch.IsPressed();
    public bool ToggleGravityPressed => _actions.Player.ToggleGravity.WasPressedThisFrame();
    public bool LockTargetPressed => _actions.Player.LockTarget.WasPressedThisFrame();

    public PlayerInput()
    {
        _actions = new InputSystem_Actions();
        _actions.Enable();
    }

    public void Dispose()
    {
        _actions?.Dispose();
    }
}
