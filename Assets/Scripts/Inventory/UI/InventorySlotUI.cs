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
