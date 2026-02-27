using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryGridUI _inventoryGridUI;
    
    private PlayerInput _input;

    private void Awake()
    {
        _input = new PlayerInput();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void Update()
    {
        if (_inventory == null) return;

        _input.Update();

        // Hotbar slot selection (1-8 keys)
        int slotIndex = _input.HotbarSlotPressed;
        if (slotIndex >= 0)
        {
            _inventory.SelectHotbarSlot(slotIndex);
        }

        // Inventory toggle (I or Tab)
        if (_input.ToggleInventoryPressed)
        {
            if (_inventoryGridUI != null)
            {
                _inventoryGridUI.Toggle();
            }
        }
    }
}
