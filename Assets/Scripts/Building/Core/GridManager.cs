using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 4f;
    [SerializeField] private BuildingEvents _events;
    [SerializeField] private GameObject _boardPrefab;

    private readonly Dictionary<GridEdge, BoardData> _edges = new();
    private readonly Dictionary<GridEdge, GameObject> _boardObjects = new();

    public float CellSize => _cellSize;
    public BuildingEvents Events => _events;
    public int BoardCount => _edges.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPosition.x / _cellSize),
            Mathf.RoundToInt(worldPosition.y / _cellSize),
            Mathf.RoundToInt(worldPosition.z / _cellSize)
        );
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(cell.x, cell.y, cell.z) * _cellSize;
    }

    public bool HasEdge(GridEdge edge)
    {
        return _edges.ContainsKey(edge);
    }

    public BoardData? GetBoardData(GridEdge edge)
    {
        return _edges.TryGetValue(edge, out BoardData data) ? data : null;
    }

    public IEnumerable<GridEdge> GetAllEdges()
    {
        return _edges.Keys;
    }

    public bool TryPlaceBoard(GridEdge edge, BoardData data)
    {
        if (HasEdge(edge))
            return false;

        _edges[edge] = data;
        SpawnBoardVisual(edge, data);
        _events?.RaiseBoardPlaced(edge, data);
        return true;
    }

    public void RemoveBoard(GridEdge edge)
    {
        if (!HasEdge(edge))
            return;

        _edges.Remove(edge);
        DestroyBoardVisual(edge);
        _events?.RaiseBoardRemoved(edge);
    }

    private void SpawnBoardVisual(GridEdge edge, BoardData data)
    {
        if (_boardPrefab == null) return;

        Vector3 worldPos = edge.GetWorldPosition(_cellSize);
        Quaternion rotation = GetBoardRotation(edge.Direction);

        GameObject board = Instantiate(_boardPrefab, worldPos, rotation, transform);
        board.name = $"Board_{edge.Cell}_{edge.Direction}";
        _boardObjects[edge] = board;
    }

    private void DestroyBoardVisual(GridEdge edge)
    {
        if (_boardObjects.TryGetValue(edge, out GameObject board))
        {
            Destroy(board);
            _boardObjects.Remove(edge);
        }
    }

    private Quaternion GetBoardRotation(EdgeDirection direction)
    {
        return direction switch
        {
            EdgeDirection.Up => Quaternion.Euler(0, 0, 0),
            EdgeDirection.Down => Quaternion.Euler(180, 0, 0),
            EdgeDirection.Left => Quaternion.Euler(0, 0, 90),
            EdgeDirection.Right => Quaternion.Euler(0, 0, -90),
            EdgeDirection.Forward => Quaternion.Euler(90, 0, 0),
            EdgeDirection.Back => Quaternion.Euler(-90, 0, 0),
            _ => Quaternion.LookRotation(GridEdge.GetDirectionOffset(direction))
        };
    }

    public bool HasAdjacentBoard(GridEdge edge)
    {
        if (_edges.Count == 0)
            return true;

        if (edge.IsDiagonal())
        {
            return CheckDiagonalAdjacency(edge);
        }

        GridEdge opposite = new GridEdge(edge.Cell, GridEdge.GetOpposite(edge.Direction));
        Vector3Int neighborCell = edge.Cell + Vector3Int.RoundToInt(GridEdge.GetDirectionOffset(edge.Direction));
        GridEdge neighborEdge = new GridEdge(neighborCell, GridEdge.GetOpposite(edge.Direction));

        if (HasEdge(opposite) || HasEdge(neighborEdge) || HasAnyBoardInCell(edge.Cell))
            return true;

        return HasAdjacentCell(edge.Cell);
    }

    private bool HasAdjacentCell(Vector3Int cell)
    {
        Vector3Int[] offsets = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.forward,
            Vector3Int.back
        };

        foreach (var offset in offsets)
        {
            if (HasAnyBoardInCell(cell + offset))
                return true;
        }
        return false;
    }

    private bool CheckDiagonalAdjacency(GridEdge edge)
    {
        return HasAnyBoardInCell(edge.Cell);
    }

    public bool HasAnyBoardInCell(Vector3Int cell)
    {
        foreach (var kvp in _edges)
        {
            if (kvp.Key.Cell == cell)
                return true;
        }
        return false;
    }

    public void ClearAll()
    {
        foreach (var edge in new List<GridEdge>(_edges.Keys))
        {
            RemoveBoard(edge);
        }
    }
}
