using UnityEngine;

public class ReinforcerUseHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
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
        if (_gridManager == null || _cameraTransform == null) return;

        var selectedItem = _inventory.GetSelectedItem();
        if (selectedItem != _reinforcerItem) return;

        if (!Input.GetMouseButtonDown(0)) return;

        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, 
            out RaycastHit hit, _useDistance, _boardLayer))
        {
            var edge = GetEdgeFromHit(hit);
            if (edge.HasValue)
            {
                TryReinforceBoard(edge.Value);
            }
        }
    }

    private GridEdge? GetEdgeFromHit(RaycastHit hit)
    {
        var boardVisual = hit.collider.GetComponentInParent<BoardVisual>();
        if (boardVisual != null)
        {
            return boardVisual.Edge;
        }
        return null;
    }

    private void TryReinforceBoard(GridEdge edge)
    {
        if (!_inventory.HasItem(_reinforcerItem, 1)) return;
        if (_reinforcedBoardPrefab == null) return;

        if (_gridManager.ReinforceBoard(edge, _reinforcedBoardPrefab))
        {
            _inventory.TryRemoveItem(_reinforcerItem, 1);
        }
    }
}
