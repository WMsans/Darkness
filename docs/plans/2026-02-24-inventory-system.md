# Inventory System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a Raft-style inventory system with hotbar + grid UI, integrated with the existing building system.

**Architecture:** ScriptableObject-based item definitions with event-driven integration. Inventory manager handles slot storage and operations. UI components subscribe to inventory events for reactive updates.

**Tech Stack:** Unity 2022+, C#, Unity UI (uGUI)

---

## Task 1: Core Data Structures

**Files:**
- Create: `Assets/Scripts/Inventory/Core/ItemData.cs`
- Create: `Assets/Scripts/Inventory/Core/InventorySlot.cs`

**Step 1: Create ItemData ScriptableObject**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField] private int _maxStack = 99;
    [SerializeField] private Sprite _icon;
    [SerializeField] private GameObject _placeablePrefab;

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public int MaxStack => _maxStack;
    public Sprite Icon => _icon;
    public GameObject PlaceablePrefab => _placeablePrefab;
}
```

**Step 2: Create InventorySlot struct**

```csharp
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
```

**Step 3: Commit core data structures**

```bash
git add Assets/Scripts/Inventory/Core/
git commit -m "feat(inventory): add ItemData and InventorySlot core types"
```

---

## Task 2: Inventory Events

**Files:**
- Create: `Assets/Scripts/Inventory/Core/InventoryEvents.cs`

**Step 1: Create InventoryEvents ScriptableObject**

```csharp
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
```

**Step 2: Commit events**

```bash
git add Assets/Scripts/Inventory/Core/InventoryEvents.cs
git commit -m "feat(inventory): add InventoryEvents for reactive UI updates"
```

---

## Task 3: Inventory Manager

**Files:**
- Create: `Assets/Scripts/Inventory/Core/Inventory.cs`

**Step 1: Create Inventory MonoBehaviour**

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _gridSlotCount = 24;
    [SerializeField] private int _hotbarSlotCount = 8;

    [Header("References")]
    [SerializeField] private InventoryEvents _events;

    private InventorySlot[] _gridSlots;
    private InventorySlot[] _hotbarSlots;
    private int _selectedHotbarIndex;

    public int GridSlotCount => _gridSlotCount;
    public int HotbarSlotCount => _hotbarSlotCount;
    public int SelectedHotbarIndex => _selectedHotbarIndex;

    public event System.Action OnInventoryChanged;

    private void Awake()
    {
        _gridSlots = new InventorySlot[_gridSlotCount];
        _hotbarSlots = new InventorySlot[_hotbarSlotCount];
        
        for (int i = 0; i < _gridSlotCount; i++)
            _gridSlots[i] = InventorySlot.Empty;
        
        for (int i = 0; i < _hotbarSlotCount; i++)
            _hotbarSlots[i] = InventorySlot.Empty;
    }

    public InventorySlot GetGridSlot(int index)
    {
        if (index < 0 || index >= _gridSlotCount) return InventorySlot.Empty;
        return _gridSlots[index];
    }

    public InventorySlot GetHotbarSlot(int index)
    {
        if (index < 0 || index >= _hotbarSlotCount) return InventorySlot.Empty;
        return _hotbarSlots[index];
    }

    public ItemData GetSelectedItem()
    {
        return GetHotbarSlot(_selectedHotbarIndex).Item;
    }

    public int GetQuantity(ItemData item)
    {
        if (item == null) return 0;

        int total = 0;
        for (int i = 0; i < _gridSlotCount; i++)
        {
            if (_gridSlots[i].Item == item)
                total += _gridSlots[i].Quantity;
        }
        for (int i = 0; i < _hotbarSlotCount; i++)
        {
            if (_hotbarSlots[i].Item == item)
                total += _hotbarSlots[i].Quantity;
        }
        return total;
    }

    public bool HasItem(ItemData item, int quantity)
    {
        return GetQuantity(item) >= quantity;
    }

    public bool TryAddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        int remaining = quantity;

        for (int i = 0; i < _hotbarSlotCount && remaining > 0; i++)
        {
            if (!_hotbarSlots[i].IsEmpty && _hotbarSlots[i].Item == item)
            {
                int canAdd = Mathf.Min(remaining, item.MaxStack - _hotbarSlots[i].Quantity);
                if (canAdd > 0)
                {
                    _hotbarSlots[i].Add(canAdd);
                    remaining -= canAdd;
                    _events?.RaiseSlotChanged(i, true);
                }
            }
        }

        for (int i = 0; i < _gridSlotCount && remaining > 0; i++)
        {
            if (!_gridSlots[i].IsEmpty && _gridSlots[i].Item == item)
            {
                int canAdd = Mathf.Min(remaining, item.MaxStack - _gridSlots[i].Quantity);
                if (canAdd > 0)
                {
                    _gridSlots[i].Add(canAdd);
                    remaining -= canAdd;
                    _events?.RaiseSlotChanged(i, false);
                }
            }
        }

        for (int i = 0; i < _hotbarSlotCount && remaining > 0; i++)
        {
            if (_hotbarSlots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remaining, item.MaxStack);
                _hotbarSlots[i] = new InventorySlot(item, toAdd);
                remaining -= toAdd;
                _events?.RaiseSlotChanged(i, true);
            }
        }

        for (int i = 0; i < _gridSlotCount && remaining > 0; i++)
        {
            if (_gridSlots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remaining, item.MaxStack);
                _gridSlots[i] = new InventorySlot(item, toAdd);
                remaining -= toAdd;
                _events?.RaiseSlotChanged(i, false);
            }
        }

        if (remaining == 0)
        {
            _events?.RaiseItemAdded(item.ItemId, quantity);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryRemoveItem(ItemData item, int quantity)
    {
        if (!HasItem(item, quantity)) return false;

        int remaining = quantity;

        for (int i = _hotbarSlotCount - 1; i >= 0 && remaining > 0; i--)
        {
            if (_hotbarSlots[i].Item == item)
            {
                int toRemove = Mathf.Min(remaining, _hotbarSlots[i].Quantity);
                _hotbarSlots[i].Remove(toRemove);
                remaining -= toRemove;
                _events?.RaiseSlotChanged(i, true);
            }
        }

        for (int i = _gridSlotCount - 1; i >= 0 && remaining > 0; i--)
        {
            if (_gridSlots[i].Item == item)
            {
                int toRemove = Mathf.Min(remaining, _gridSlots[i].Quantity);
                _gridSlots[i].Remove(toRemove);
                remaining -= toRemove;
                _events?.RaiseSlotChanged(i, false);
            }
        }

        _events?.RaiseItemRemoved(item.ItemId, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= _hotbarSlotCount) return;
        _selectedHotbarIndex = index;
        _events?.RaiseHotbarSelectionChanged(index);
    }

    public void MoveGridSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _gridSlotCount) return;
        if (toIndex < 0 || toIndex >= _gridSlotCount) return;

        var temp = _gridSlots[fromIndex];
        _gridSlots[fromIndex] = _gridSlots[toIndex];
        _gridSlots[toIndex] = temp;

        _events?.RaiseSlotChanged(fromIndex, false);
        _events?.RaiseSlotChanged(toIndex, false);
        OnInventoryChanged?.Invoke();
    }

    public void MoveHotbarSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _hotbarSlotCount) return;
        if (toIndex < 0 || toIndex >= _hotbarSlotCount) return;

        var temp = _hotbarSlots[fromIndex];
        _hotbarSlots[fromIndex] = _hotbarSlots[toIndex];
        _hotbarSlots[toIndex] = temp;

        _events?.RaiseSlotChanged(fromIndex, true);
        _events?.RaiseSlotChanged(toIndex, true);
        OnInventoryChanged?.Invoke();
    }

    public void SplitGridStack(int index)
    {
        if (index < 0 || index >= _gridSlotCount) return;
        if (_gridSlots[index].IsEmpty) return;

        int half = _gridSlots[index].Quantity / 2;
        if (half == 0) return;

        int emptyIndex = FindEmptyGridSlot();
        if (emptyIndex == -1) return;

        var item = _gridSlots[index].Item;
        _gridSlots[index].Remove(half);
        _gridSlots[emptyIndex] = new InventorySlot(item, half);

        _events?.RaiseSlotChanged(index, false);
        _events?.RaiseSlotChanged(emptyIndex, false);
        OnInventoryChanged?.Invoke();
    }

    private int FindEmptyGridSlot()
    {
        for (int i = 0; i < _gridSlotCount; i++)
        {
            if (_gridSlots[i].IsEmpty) return i;
        }
        return -1;
    }

    public void DebugAddItem(ItemData item, int quantity)
    {
        TryAddItem(item, quantity);
    }
}
```

**Step 2: Commit inventory manager**

```bash
git add Assets/Scripts/Inventory/Core/Inventory.cs
git commit -m "feat(inventory): add Inventory manager with add/remove/slot operations"
```

---

## Task 4: Create Item Assets

**Files:**
- Create: `Assets/Scripts/Inventory/Items/` folder (no code, just folder structure)

**Step 1: Create Items folder**

Create the folder structure for item assets.

**Step 2: Note for manual Unity setup**

In Unity Editor:
1. Create `Assets/Scripts/Inventory/Items/` folder
2. Right-click → Create → Inventory → Item Data → Name it "Board"
   - ItemId: "board"
   - DisplayName: "Board"
   - MaxStack: 20
   - Icon: (assign board sprite)
3. Create another → Name it "BoardReinforcer"
   - ItemId: "board_reinforcer"
   - DisplayName: "Board Reinforcer"
   - MaxStack: 10
   - Icon: (assign reinforcer sprite)

**Step 3: Commit folder structure**

```bash
git add Assets/Scripts/Inventory/Items/.gitkeep
git commit -m "feat(inventory): add items folder structure"
```

---

## Task 5: UI Slot Widget

**Files:**
- Create: `Assets/Scripts/Inventory/UI/InventorySlotUI.cs`

**Step 1: Create InventorySlotUI component**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _selectionHighlight;
    [SerializeField] private Button _slotButton;

    private int _slotIndex;
    private bool _isHotbar;
    private Inventory _inventory;
    private InventoryEvents _events;

    public int SlotIndex => _slotIndex;
    public bool IsHotbar => _isHotbar;

    public void Initialize(int slotIndex, bool isHotbar, Inventory inventory, InventoryEvents events)
    {
        _slotIndex = slotIndex;
        _isHotbar = isHotbar;
        _inventory = inventory;
        _events = events;

        _events.OnSlotChanged += OnSlotChanged;
        _events.OnHotbarSelectionChanged += OnHotbarSelectionChanged;

        if (_slotButton != null)
            _slotButton.onClick.AddListener(OnSlotClicked);

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnSlotChanged -= OnSlotChanged;
            _events.OnHotbarSelectionChanged -= OnHotbarSelectionChanged;
        }
    }

    private void OnSlotChanged(int index, bool isHotbar)
    {
        if (index == _slotIndex && isHotbar == _isHotbar)
            UpdateDisplay();
    }

    private void OnHotbarSelectionChanged(int selectedIndex)
    {
        if (_isHotbar && _selectionHighlight != null)
            _selectionHighlight.enabled = (selectedIndex == _slotIndex);
    }

    public void UpdateDisplay()
    {
        if (_inventory == null) return;

        var slot = _isHotbar 
            ? _inventory.GetHotbarSlot(_slotIndex) 
            : _inventory.GetGridSlot(_slotIndex);

        if (slot.IsEmpty)
        {
            if (_iconImage != null) _iconImage.enabled = false;
            if (_quantityText != null) _quantityText.text = "";
        }
        else
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = slot.Item.Icon;
                _iconImage.enabled = true;
            }
            if (_quantityText != null)
            {
                _quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
            }
        }

        if (_selectionHighlight != null && _isHotbar)
            _selectionHighlight.enabled = (_inventory.SelectedHotbarIndex == _slotIndex);
    }

    private void OnSlotClicked()
    {
        // Handled by parent UI component for drag/drop
    }

    public void SetHighlight(bool highlighted)
    {
        if (_selectionHighlight != null)
            _selectionHighlight.enabled = highlighted;
    }
}
```

**Step 2: Commit slot UI**

```bash
git add Assets/Scripts/Inventory/UI/InventorySlotUI.cs
git commit -m "feat(inventory): add InventorySlotUI widget component"
```

---

## Task 6: Hotbar UI

**Files:**
- Create: `Assets/Scripts/Inventory/UI/HotbarUI.cs`

**Step 1: Create HotbarUI component**

```csharp
using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlotUI[] _slotUIs;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryEvents _events;

    private void Start()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            _slotUIs[i].Initialize(i, true, _inventory, _events);
        }
    }

    public void RefreshAllSlots()
    {
        foreach (var slotUI in _slotUIs)
        {
            slotUI.UpdateDisplay();
        }
    }
}
```

**Step 2: Commit hotbar UI**

```bash
git add Assets/Scripts/Inventory/UI/HotbarUI.cs
git commit -m "feat(inventory): add HotbarUI component"
```

---

## Task 7: Inventory Grid UI

**Files:**
- Create: `Assets/Scripts/Inventory/UI/InventoryGridUI.cs`

**Step 1: Create InventoryGridUI component**

```csharp
using UnityEngine;

public class InventoryGridUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlotUI[] _slotUIs;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryEvents _events;
    [SerializeField] private GameObject _panel;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        InitializeSlots();
        Close();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            _slotUIs[i].Initialize(i, false, _inventory, _events);
        }
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        _isOpen = true;
        if (_panel != null)
            _panel.SetActive(true);
        _events?.RaiseInventoryOpened();
    }

    public void Close()
    {
        _isOpen = false;
        if (_panel != null)
            _panel.SetActive(false);
        _events?.RaiseInventoryClosed();
    }

    public void RefreshAllSlots()
    {
        foreach (var slotUI in _slotUIs)
        {
            slotUI.UpdateDisplay();
        }
    }
}
```

**Step 2: Commit grid UI**

```bash
git add Assets/Scripts/Inventory/UI/InventoryGridUI.cs
git commit -m "feat(inventory): add InventoryGridUI component with toggle"
```

---

## Task 8: Player Input Extension

**Files:**
- Modify: `Assets/Scripts/Player/PlayerInput.cs`

**Step 1: Read current PlayerInput.cs**

Read the file to understand existing input structure.

**Step 2: Add inventory and hotbar inputs**

Add to PlayerInput class:
- `ToggleInventoryPressed` (I key or Tab)
- `HotbarSlotPressed` (returns -1 if none, 0-7 for slots 1-8)
- `HotbarScrollDelta` (scroll wheel for cycling)

**Step 3: Commit input changes**

```bash
git add Assets/Scripts/Player/PlayerInput.cs
git commit -m "feat(inventory): add inventory toggle and hotbar inputs to PlayerInput"
```

---

## Task 9: BoardPlacer Inventory Integration

**Files:**
- Modify: `Assets/Scripts/Building/Placement/BoardPlacer.cs`

**Step 1: Read current BoardPlacer.cs**

Already read - understand the current placement flow.

**Step 2: Add inventory references and checks**

```csharp
// Add to fields
[Header("Inventory")]
[SerializeField] private Inventory _inventory;
[SerializeField] private ItemData _boardItem;

// Modify TryPlaceBoard method
private void TryPlaceBoard()
{
    if (!_currentEdgeHit.IsValid) return;
    if (_gridManager.HasBoardAtFace(_currentEdgeHit.Edge)) return;

    // NEW: Check inventory
    if (_inventory != null && _boardItem != null)
    {
        if (!_inventory.HasItem(_boardItem, 1)) return;
    }

    BoardData data = BoardData.Default;
    if (_gridManager.TryPlaceBoard(_currentEdgeHit.Edge, data))
    {
        // NEW: Consume board from inventory
        if (_inventory != null && _boardItem != null)
        {
            _inventory.TryRemoveItem(_boardItem, 1);
        }
    }
}
```

**Step 3: Commit BoardPlacer changes**

```bash
git add Assets/Scripts/Building/Placement/BoardPlacer.cs
git commit -m "feat(inventory): integrate inventory checks into BoardPlacer"
```

---

## Task 10: GridManager Reinforce Method

**Files:**
- Modify: `Assets/Scripts/Building/Core/GridManager.cs`

**Step 1: Read current GridManager.cs**

Read to understand current board visual management.

**Step 2: Add ReinforceBoard method**

Add a method that replaces the visual prefab of an existing board while keeping its GridEdge position.

**Step 3: Commit GridManager changes**

```bash
git add Assets/Scripts/Building/Core/GridManager.cs
git commit -m "feat(inventory): add ReinforceBoard method to GridManager"
```

---

## Task 11: Reinforcer Usage Handler

**Files:**
- Create: `Assets/Scripts/Inventory/ReinforcerUseHandler.cs`

**Step 1: Create ReinforcerUseHandler**

```csharp
using UnityEngine;

public class ReinforcerUseHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryEvents _inventoryEvents;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private ItemData _reinforcerItem;
    [SerializeField] private GameObject _reinforcedBoardPrefab;

    [Header("Settings")]
    [SerializeField] private float _useDistance = 10f;
    [SerializeField] private LayerMask _boardLayer;

    private void Update()
    {
        HandleReinforcerUse();
    }

    private void HandleReinforcerUse()
    {
        if (_inventory == null || _reinforcerItem == null) return;

        // Check if reinforcer is selected
        var selectedItem = _inventory.GetSelectedItem();
        if (selectedItem != _reinforcerItem) return;

        // Check for left click
        if (!Input.GetMouseButtonDown(0)) return;

        // Raycast for board
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, 
            out RaycastHit hit, _useDistance, _boardLayer))
        {
            // Get the GridEdge from the hit board
            var edge = GetEdgeFromHit(hit);
            if (edge.HasValue)
            {
                TryReinforceBoard(edge.Value);
            }
        }
    }

    private GridEdge? GetEdgeFromHit(RaycastHit hit)
    {
        // Check if the hit object has a BoardVisual component or similar
        // that can tell us which GridEdge it belongs to
        var boardVisual = hit.collider.GetComponentInParent<BoardVisual>();
        if (boardVisual != null)
        {
            return boardVisual.GridEdge;
        }
        return null;
    }

    private void TryReinforceBoard(GridEdge edge)
    {
        if (!_inventory.HasItem(_reinforcerItem, 1)) return;

        if (_gridManager.ReinforceBoard(edge, _reinforcedBoardPrefab))
        {
            _inventory.TryRemoveItem(_reinforcerItem, 1);
        }
    }
}
```

**Step 2: Commit reinforcer handler**

```bash
git add Assets/Scripts/Inventory/ReinforcerUseHandler.cs
git commit -m "feat(inventory): add ReinforcerUseHandler for upgrading boards"
```

---

## Task 12: BoardVisual Edge Reference

**Files:**
- Modify: `Assets/Scripts/Building/Visuals/BoardVisual.cs`

**Step 1: Read current BoardVisual.cs**

Understand the current structure.

**Step 2: Add GridEdge reference**

Add a field to store which GridEdge this visual belongs to, so the reinforcer can identify it via raycast.

**Step 3: Commit BoardVisual changes**

```bash
git add Assets/Scripts/Building/Visuals/BoardVisual.cs
git commit -m "feat(inventory): add GridEdge reference to BoardVisual for reinforcer targeting"
```

---

## Task 13: Debug/Test Inventory

**Files:**
- Create: `Assets/Scripts/Inventory/Debug/InventoryDebug.cs`

**Step 1: Create debug component for testing**

```csharp
using UnityEngine;

public class InventoryDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    
    [Header("Test Items")]
    [SerializeField] private ItemData _testBoard;
    [SerializeField] private ItemData _testReinforcer;
    [SerializeField] private int _testQuantity = 5;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            _inventory?.DebugAddItem(_testBoard, _testQuantity);
            Debug.Log($"Added {_testQuantity} boards");
        }
        
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            _inventory?.DebugAddItem(_testReinforcer, _testQuantity);
            Debug.Log($"Added {_testQuantity} reinforcers");
        }
    }
}
```

**Step 2: Commit debug tool**

```bash
git add Assets/Scripts/Inventory/Debug/InventoryDebug.cs
git commit -m "feat(inventory): add debug component for testing inventory"
```

---

## Task 14: Assembly Definition

**Files:**
- Create: `Assets/Scripts/Inventory/Inventory.asmdef`

**Step 1: Create assembly definition**

Create an assembly definition for the Inventory scripts to properly reference Building system.

**Step 2: Commit assembly definition**

```bash
git add Assets/Scripts/Inventory/Inventory.asmdef
git commit -m "feat(inventory): add assembly definition"
```

---

## Summary

**Files Created:**
- `Assets/Scripts/Inventory/Core/ItemData.cs`
- `Assets/Scripts/Inventory/Core/InventorySlot.cs`
- `Assets/Scripts/Inventory/Core/InventoryEvents.cs`
- `Assets/Scripts/Inventory/Core/Inventory.cs`
- `Assets/Scripts/Inventory/UI/InventorySlotUI.cs`
- `Assets/Scripts/Inventory/UI/HotbarUI.cs`
- `Assets/Scripts/Inventory/UI/InventoryGridUI.cs`
- `Assets/Scripts/Inventory/ReinforcerUseHandler.cs`
- `Assets/Scripts/Inventory/Debug/InventoryDebug.cs`

**Files Modified:**
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Building/Placement/BoardPlacer.cs`
- `Assets/Scripts/Building/Core/GridManager.cs`
- `Assets/Scripts/Building/Visuals/BoardVisual.cs`

**Manual Unity Setup Required:**
- Create UI Canvas with Hotbar and Inventory Grid
- Create ItemData assets for Board and Reinforcer
- Wire up all references in Inspector
