using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput
{
    private InputSystem_Actions _actions;
    private bool _jumpBuffer;
    private bool _toggleGravityBuffer;
    private bool _lockTargetBuffer;

    public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => _actions.Player.Look.ReadValue<Vector2>();
    public bool SprintHeld => _actions.Player.Sprint.IsPressed();
    public bool SneakHeld => _actions.Player.Crouch.IsPressed();
    public bool JumpHeld => _actions.Player.Jump.IsPressed();
    public bool RollHeld => _actions.Player.Roll.IsPressed();

    public bool PlaceBoardPressed => _actions.Player.PlaceBoard.WasPressedThisFrame();
    public bool RemoveBoardPressed => _actions.Player.RemoveBoard.WasPressedThisFrame();
    public bool PlaceBoardHeld => _actions.Player.PlaceBoard.IsPressed();

    public bool JumpPressed
    {
        get
        {
            if (_jumpBuffer)
            {
                _jumpBuffer = false;
                return true;
            }
            return false;
        }
    }

    public bool ToggleGravityPressed
    {
        get
        {
            if (_toggleGravityBuffer)
            {
                _toggleGravityBuffer = false;
                return true;
            }
            return false;
        }
    }

    public bool LockTargetPressed
    {
        get
        {
            if (_lockTargetBuffer)
            {
                _lockTargetBuffer = false;
                return true;
            }
            return false;
        }
    }

    public PlayerInput()
    {
        _actions = new InputSystem_Actions();
        _actions.Enable();
    }

    public void Update()
    {
        if (_actions.Player.Jump.WasPressedThisFrame()) _jumpBuffer = true;
        if (_actions.Player.ToggleGravity.WasPressedThisFrame()) _toggleGravityBuffer = true;
        if (_actions.Player.LockTarget.WasPressedThisFrame()) _lockTargetBuffer = true;
    }

    public void Dispose()
    {
        _actions?.Dispose();
    }
}
