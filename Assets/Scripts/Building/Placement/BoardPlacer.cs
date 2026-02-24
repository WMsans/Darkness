using UnityEngine;

public class BoardPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private BuildingEvents _events;
    [SerializeField] private BoardPreview _preview;

    [Header("Settings")]
    [SerializeField] private float _placeDistance = 10f;
    [SerializeField] private LayerMask _placeLayer;
    [SerializeField] private bool _enablePlacement = true;

    private PlayerInput _input;
    private EdgeHit _currentEdgeHit;

    private void Awake()
    {
        _input = new PlayerInput();
        
        if (_gridManager == null)
            _gridManager = GridManager.Instance;
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void Update()
    {
        if (!_enablePlacement || _gridManager == null)
        {
            _preview?.Hide();
            return;
        }

        _input.Update();
        DetectTargetEdge();
        UpdatePreview();
        HandleInput();
    }

    private void DetectTargetEdge()
    {
        _currentEdgeHit = EdgeDetector.DetectEdge(
            _cameraTransform.position,
            _cameraTransform.forward,
            _placeDistance,
            _placeLayer,
            _gridManager
        );
    }

    private void UpdatePreview()
    {
        if (_preview == null) return;

        bool hasDirection = _currentEdgeHit.IsValid || _currentEdgeHit.Edge.Direction != EdgeDirection.None;
        bool isOccupied = _gridManager.HasBoardAtFace(_currentEdgeHit.Edge);

        if (hasDirection && !isOccupied)
        {
            Quaternion rotation = GridManager.GetBoardRotation(_currentEdgeHit.Edge.Direction);
            _preview.Show(_currentEdgeHit.Edge, _currentEdgeHit.WorldPosition, rotation, _currentEdgeHit.IsValid);
            _events?.RaisePreviewChanged(_currentEdgeHit.Edge);
        }
        else
        {
            _preview.Hide();
            _events?.RaisePreviewChanged(null);
        }
    }

    private void HandleInput()
    {
        if (_input.PlaceBoardPressed)
        {
            TryPlaceBoard();
        }

        if (_input.RemoveBoardPressed)
        {
            TryRemoveBoard();
        }
    }

    private void TryPlaceBoard()
    {
        if (!_currentEdgeHit.IsValid)
            return;

        if (_gridManager.HasBoardAtFace(_currentEdgeHit.Edge))
            return;

        BoardData data = BoardData.Default;
        _gridManager.TryPlaceBoard(_currentEdgeHit.Edge, data);
    }

    private void TryRemoveBoard()
    {
        GridEdge? edgeToRemove = EdgeDetector.DetectEdgeForRemoval(
            _cameraTransform.position,
            _cameraTransform.forward,
            _placeDistance,
            _placeLayer,
            _gridManager
        );

        if (edgeToRemove.HasValue)
        {
            _gridManager.RemoveBoard(edgeToRemove.Value);
        }
    }

    public void SetPlacementEnabled(bool enabled)
    {
        _enablePlacement = enabled;
        if (!enabled)
        {
            _preview?.Hide();
        }
    }
}
