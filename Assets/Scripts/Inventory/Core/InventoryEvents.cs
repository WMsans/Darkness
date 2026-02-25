using System;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryEvents", menuName = "Inventory/Events")]
public class InventoryEvents : ScriptableObject
{
    public event Action<string, int> OnItemAdded;
    public event Action<string, int> OnItemRemoved;
    public event Action<int, bool> OnSlotChanged;
    public event Action<int> OnHotbarSelectionChanged;
    public event Action OnInventoryOpened;
    public event Action OnInventoryClosed;

    public void RaiseItemAdded(string itemId, int quantity)
    {
        OnItemAdded?.Invoke(itemId, quantity);
    }

    public void RaiseItemRemoved(string itemId, int quantity)
    {
        OnItemRemoved?.Invoke(itemId, quantity);
    }

    public void RaiseSlotChanged(int slotIndex, bool isHotbar)
    {
        OnSlotChanged?.Invoke(slotIndex, isHotbar);
    }

    public void RaiseHotbarSelectionChanged(int newIndex)
    {
        OnHotbarSelectionChanged?.Invoke(newIndex);
    }

    public void RaiseInventoryOpened()
    {
        OnInventoryOpened?.Invoke();
    }

    public void RaiseInventoryClosed()
    {
        OnInventoryClosed?.Invoke();
    }
}
