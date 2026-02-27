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
