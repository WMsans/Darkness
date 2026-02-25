using System;

[Serializable]
public struct InventorySlot
{
    private ItemData _item;
    private int _quantity;

    public ItemData Item => _item;
    public int Quantity => _quantity;

    public bool IsEmpty => _item == null;

    public InventorySlot(ItemData item, int quantity)
    {
        _item = item;
        _quantity = quantity;
    }

    public bool CanAdd(int amount) => !IsEmpty && _quantity + amount <= _item.MaxStack;

    public void Add(int amount)
    {
        if (IsEmpty) return;
        _quantity += amount;
    }

    public void Remove(int amount)
    {
        _quantity -= amount;
        if (_quantity <= 0)
        {
            _item = null;
            _quantity = 0;
        }
    }

    public void Clear()
    {
        _item = null;
        _quantity = 0;
    }

    public static InventorySlot Empty => new InventorySlot(null, 0);
}
