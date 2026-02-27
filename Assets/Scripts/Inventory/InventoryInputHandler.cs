using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryGridUI _inventoryGridUI;
    
    private InputSystem_Actions _input;
    private int _pendingHotbarSlot = -1;
    private bool _toggleInventoryPressed;

    private void Awake()
    {
        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.Player.ToggleInventory.performed += OnToggleInventory;
        _input.Player.Hotbar1.performed += ctx => OnHotbarSlot(0);
        _input.Player.Hotbar2.performed += ctx => OnHotbarSlot(1);
        _input.Player.Hotbar3.performed += ctx => OnHotbarSlot(2);
        _input.Player.Hotbar4.performed += ctx => OnHotbarSlot(3);
        _input.Player.Hotbar5.performed += ctx => OnHotbarSlot(4);
        _input.Player.Hotbar6.performed += ctx => OnHotbarSlot(5);
        _input.Player.Hotbar7.performed += ctx => OnHotbarSlot(6);
        _input.Player.Hotbar8.performed += ctx => OnHotbarSlot(7);
    }

    private void OnDisable()
    {
        _input.Player.ToggleInventory.performed -= OnToggleInventory;
        _input.Player.Hotbar1.performed -= ctx => OnHotbarSlot(0);
        _input.Player.Hotbar2.performed -= ctx => OnHotbarSlot(1);
        _input.Player.Hotbar3.performed -= ctx => OnHotbarSlot(2);
        _input.Player.Hotbar4.performed -= ctx => OnHotbarSlot(3);
        _input.Player.Hotbar5.performed -= ctx => OnHotbarSlot(4);
        _input.Player.Hotbar6.performed -= ctx => OnHotbarSlot(5);
        _input.Player.Hotbar7.performed -= ctx => OnHotbarSlot(6);
        _input.Player.Hotbar8.performed -= ctx => OnHotbarSlot(7);
        _input.Disable();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (_inventoryGridUI != null)
        {
            _inventoryGridUI.Toggle();
        }
    }

    private void OnHotbarSlot(int slotIndex)
    {
        if (_inventory != null)
        {
            _inventory.SelectHotbarSlot(slotIndex);
        }
    }
}
