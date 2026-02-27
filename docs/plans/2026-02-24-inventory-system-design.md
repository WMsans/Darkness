# Inventory System Design

## Overview

A Raft-style inventory system with a persistent hotbar and expandable grid. Uses ScriptableObject-based item definitions with event-driven integration to the existing building system.

## Requirements Summary

- **UI**: Hotbar (8 slots, always visible) + Grid (24 slots, toggle with key)
- **Items**: Two item types - Board (consumed on build), Board Reinforcer (upgrades boards)
- **Stacking**: Items stack up to max quantity per slot
- **Integration**: Building system checks inventory before placement
- **Extensibility**: ScriptableObject pattern for easy item addition

## Architecture

### Approach: ScriptableObject Items + Event-Driven

Item definitions are ScriptableObjects (`ItemData`). Inventory holds references with quantities. The building system queries inventory and consumes items on successful actions. This matches the existing `BuildingEvents` pattern.

## Core Data Structures

### ItemData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string ItemId;
    public string DisplayName;
    public int MaxStack;
    public Sprite Icon;
    public GameObject PlaceablePrefab; // Optional: for placeable items
}
```

### InventorySlot

```csharp
[System.Serializable]
public struct InventorySlot
{
    public ItemData Item;
    public int Quantity;
    
    public bool IsEmpty => Item == null;
    public bool CanAdd(int amount) => !IsEmpty && Quantity + amount <= Item.MaxStack;
}
```

### Inventory (MonoBehaviour)

```csharp
public class Inventory : MonoBehaviour
{
    [SerializeField] private int gridSlotCount = 24;
    [SerializeField] private int hotbarSlotCount = 8;
    
    private InventorySlot[] _gridSlots;
    private InventorySlot[] _hotbarSlots;
    private int _selectedHotbarIndex;
    
    // Core operations
    public bool TryAddItem(ItemData item, int quantity);
    public bool TryRemoveItem(ItemData item, int quantity);
    public int GetQuantity(ItemData item);
    public bool HasItem(ItemData item, int quantity);
    
    // Slot management
    public void MoveSlot(int fromIndex, int toIndex);
    public void SplitStack(int index, int amount);
    
    // Hotbar
    public ItemData GetSelectedItem();
    public void SelectHotbarSlot(int index);
}
```

### InventoryEvents (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "InventoryEvents", menuName = "Inventory/Events")]
public class InventoryEvents : ScriptableObject
{
    public event Action<string, int> OnItemAdded;
    public event Action<string, int> OnItemRemoved;
    public event Action<int> OnSlotChanged;
    public event Action<int> OnHotbarSelectionChanged;
    
    public void RaiseItemAdded(string itemId, int quantity);
    public void RaiseItemRemoved(string itemId, int quantity);
    public void RaiseSlotChanged(int index);
    public void RaiseHotbarSelectionChanged(int index);
}
```

## Inventory Operations

### Adding Items

1. Find existing slots with same ItemData that can accept more
2. Fill existing stacks first
3. Find empty slots for remainder
4. Return false if not all items could be added

### Removing Items

1. Calculate total quantity across all slots
2. Return false if insufficient
3. Remove from slots, clearing emptied slots

### Slot Management

- **Move**: Swap contents of two slots
- **Split**: Move half of a stack to an empty slot
- **Hotbar Assignment**: Link a grid slot reference to a hotbar slot

## Building System Integration

### BoardPlacer Changes

```csharp
// Add references
[SerializeField] private Inventory _inventory;
[SerializeField] private ItemData _boardItem;

// In TryPlaceBoard()
private void TryPlaceBoard()
{
    if (!_currentEdgeHit.IsValid) return;
    if (_gridManager.HasBoardAtFace(_currentEdgeHit.Edge)) return;
    
    // Check inventory
    if (!_inventory.HasItem(_boardItem, 1)) return;
    
    // Place board
    if (_gridManager.TryPlaceBoard(_currentEdgeHit.Edge, BoardData.Default))
    {
        _inventory.TryRemoveItem(_boardItem, 1);
    }
}
```

### Reinforcer Usage Flow

1. Player selects reinforcer in hotbar
2. Player aims at existing board and clicks
3. Raycast detects board, get GridEdge
4. Check `inventory.HasItem(reinforcerItem, 1)`
5. Call `gridManager.ReinforceBoard(edge, reinforcedPrefab)`
6. Remove reinforcer from inventory

### GridManager Addition

```csharp
public void ReinforceBoard(GridEdge edge, GameObject reinforcedPrefab)
{
    if (!_edges.TryGetValue(edge, out var data)) return;
    
    // Destroy current visual
    Destroy(_boardVisuals[edge]);
    
    // Spawn reinforced visual
    var newVisual = Instantiate(reinforcedPrefab, ...);
    _boardVisuals[edge] = newVisual;
    
    // Update BoardData with new style
    _edges[edge] = new BoardData("reinforced", data.PlacedTime);
}
```

## UI Design

### HotbarUI (Always Visible)

- Horizontal bar at screen bottom center
- 8 slots with item icons + quantity badges
- Selected slot has highlight border
- Keybinding hints (1-8) shown below slots
- Updates when inventory changes (subscribe to InventoryEvents)

### InventoryGridUI (Toggle with 'I' or 'Tab')

- Semi-transparent overlay panel
- 4x6 grid (24 slots)
- Shows all items with quantity badges
- Supports drag/drop reordering
- Click grid item, then click hotbar slot to assign

### Slot Interactions

| Action | Behavior |
|--------|----------|
| Left-click slot | Select for moving |
| Left-click empty | Place selected item |
| Right-click | Split stack (half to empty slot) |
| Number key (1-8) | Select hotbar slot |
| I / Tab | Toggle inventory grid |

## File Structure

```
Assets/Scripts/Inventory/
├── Core/
│   ├── ItemData.cs           # ScriptableObject: item definition
│   ├── InventorySlot.cs      # Struct: item + quantity
│   ├── Inventory.cs          # Main inventory manager
│   └── InventoryEvents.cs    # ScriptableObject: events
├── UI/
│   ├── HotbarUI.cs           # Hotbar display component
│   ├── InventoryGridUI.cs    # Full grid overlay
│   ├── InventorySlotUI.cs    # Single slot widget
│   └── DragDropHandler.cs    # Drag/drop logic
└── Items/
    ├── BoardItem.asset       # Board ItemData instance
    └── ReinforcerItem.asset  # Reinforcer ItemData instance
```

**Modified Files**

| File | Changes |
|------|---------|
| `BoardPlacer.cs` | Add inventory checks and consumption |
| `GridManager.cs` | Add `ReinforceBoard()` method |
| `PlayerInput.cs` | Add inventory toggle, hotbar selection |

## Item Definitions

### Board Item
- **MaxStack**: 20
- **Usage**: Consumed when placing boards
- **PlaceablePrefab**: Standard board prefab

### Board Reinforcer Item
- **MaxStack**: 10
- **Usage**: Applied to existing boards to upgrade visuals
- **PlaceablePrefab**: Reinforced board prefab (used on upgrade)

## Scope for Initial Implementation

**Included:**
- Core inventory system with stacking
- Hotbar + grid UI with basic interactions
- Board and Reinforcer items
- Building system integration

**Not Included (Future):**
- Crafting system
- Loot tables / item drops
- Save/load persistence
- Additional item types
