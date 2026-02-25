using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput
{
    private InputSystem_Actions _actions;
    private bool _jumpBuffer;
    private bool _toggleGravityBuffer;
    private bool _lockTargetBuffer;
    private int _hotbarSlotBuffer = -1;
    private bool _toggleInventoryBuffer;

    public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => _actions.Player.Look.ReadValue<Vector2>();
    public bool SprintHeld => _actions.Player.Sprint.IsPressed();
    public bool SneakHeld => _actions.Player.Crouch.IsPressed();
    public bool JumpHeld => _actions.Player.Jump.IsPressed();

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

    public bool ToggleInventoryPressed
    {
        get
        {
            if (_toggleInventoryBuffer)
            {
                _toggleInventoryBuffer = false;
                return true;
            }
            return false;
        }
    }

    public int HotbarSlotPressed
    {
        get
        {
            int slot = _hotbarSlotBuffer;
            _hotbarSlotBuffer = -1;
            return slot;
        }
    }

    public float HotbarScrollDelta => Mouse.current?.scroll.y.ReadValue() ?? 0f;

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

        if (Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
            _toggleInventoryBuffer = true;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) _hotbarSlotBuffer = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) _hotbarSlotBuffer = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) _hotbarSlotBuffer = 2;
        if (Keyboard.current.digit4Key.wasPressedThisFrame) _hotbarSlotBuffer = 3;
        if (Keyboard.current.digit5Key.wasPressedThisFrame) _hotbarSlotBuffer = 4;
        if (Keyboard.current.digit6Key.wasPressedThisFrame) _hotbarSlotBuffer = 5;
        if (Keyboard.current.digit7Key.wasPressedThisFrame) _hotbarSlotBuffer = 6;
        if (Keyboard.current.digit8Key.wasPressedThisFrame) _hotbarSlotBuffer = 7;
    }

    public void Dispose()
    {
        _actions?.Dispose();
    }
}
