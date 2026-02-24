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

    /// <summary>
    /// Returns the equivalent GridEdge from the adjacent cell that shares the same
    /// physical face. For example, cell (0,0,0) Up == cell (0,1,0) Down.
    /// Only works for cardinal directions; returns null for diagonals.
    /// </summary>
    public static GridEdge? GetOppositeFaceEdge(GridEdge edge)
    {
        EdgeDirection opposite = GridEdge.GetOpposite(edge.Direction);
        if (opposite == EdgeDirection.None)
            return null;

        Vector3Int neighborCell = edge.Cell + Vector3Int.RoundToInt(GridEdge.GetDirectionOffset(edge.Direction));
        return new GridEdge(neighborCell, opposite);
    }

    /// <summary>
    /// Checks whether a board already exists at the same physical location,
    /// either as the exact same GridEdge or as the equivalent opposite face
    /// on the neighboring cell.
    /// </summary>
    public bool HasBoardAtFace(GridEdge edge)
    {
        if (_edges.ContainsKey(edge))
            return true;

        GridEdge? opposite = GetOppositeFaceEdge(edge);
        if (opposite.HasValue && _edges.ContainsKey(opposite.Value))
            return true;

        return false;
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
        if (HasBoardAtFace(edge))
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

        BoardVisual visual = board.GetComponent<BoardVisual>();
        if (visual != null)
            visual.Initialize(edge);

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

    public static Quaternion GetBoardRotation(EdgeDirection direction)
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

        foreach (GridEdge neighbor in GetSideTouchingNeighbors(edge))
        {
            if (HasEdge(neighbor))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all board positions that share a physical side (edge of the rectangle)
    /// with the given board. For a cardinal face, the board rectangle has 4 sides.
    /// Each side is shared by two potential boards:
    ///   1. A same-direction board in the neighboring cell along that tangent axis
    ///   2. A perpendicular board on the same cell whose face is in that tangent direction
    /// This yields 8 potential side-touching neighbors for cardinal faces.
    /// </summary>
    private static List<GridEdge> GetSideTouchingNeighbors(GridEdge edge)
    {
        var neighbors = new List<GridEdge>();
        Vector3Int cell = edge.Cell;
        EdgeDirection dir = edge.Direction;

        // Get the two tangent axes of the face plane.
        // For a face with normal along axis N, the tangent axes are the other two cardinal axes.
        GetTangentAxes(dir, out Vector3Int tangent1Pos, out Vector3Int tangent1Neg,
                                 out EdgeDirection tangent1DirPos, out EdgeDirection tangent1DirNeg,
                                 out Vector3Int tangent2Pos, out Vector3Int tangent2Neg,
                                 out EdgeDirection tangent2DirPos, out EdgeDirection tangent2DirNeg);

        // For each tangent direction (4 total: +/- on each of 2 axes):
        // - Same face on neighboring cell along that tangent
        // - Perpendicular face on the same cell in that tangent direction
        neighbors.Add(new GridEdge(cell + tangent1Pos, dir));
        neighbors.Add(new GridEdge(cell, tangent1DirPos));

        neighbors.Add(new GridEdge(cell + tangent1Neg, dir));
        neighbors.Add(new GridEdge(cell, tangent1DirNeg));

        neighbors.Add(new GridEdge(cell + tangent2Pos, dir));
        neighbors.Add(new GridEdge(cell, tangent2DirPos));

        neighbors.Add(new GridEdge(cell + tangent2Neg, dir));
        neighbors.Add(new GridEdge(cell, tangent2DirNeg));

        return neighbors;
    }

    /// <summary>
    /// For a cardinal face direction, outputs the two tangent axes as cell offsets
    /// and their corresponding EdgeDirection values.
    /// </summary>
    private static void GetTangentAxes(EdgeDirection faceDir,
        out Vector3Int t1Pos, out Vector3Int t1Neg, out EdgeDirection t1DirPos, out EdgeDirection t1DirNeg,
        out Vector3Int t2Pos, out Vector3Int t2Neg, out EdgeDirection t2DirPos, out EdgeDirection t2DirNeg)
    {
        switch (faceDir)
        {
            case EdgeDirection.Up:
            case EdgeDirection.Down:
                // Normal is Y-axis, tangents are X and Z
                t1Pos = Vector3Int.right;   t1Neg = Vector3Int.left;
                t1DirPos = EdgeDirection.Right; t1DirNeg = EdgeDirection.Left;
                t2Pos = Vector3Int.forward; t2Neg = Vector3Int.back;
                t2DirPos = EdgeDirection.Forward; t2DirNeg = EdgeDirection.Back;
                break;
            case EdgeDirection.Left:
            case EdgeDirection.Right:
                // Normal is X-axis, tangents are Y and Z
                t1Pos = Vector3Int.up;      t1Neg = Vector3Int.down;
                t1DirPos = EdgeDirection.Up; t1DirNeg = EdgeDirection.Down;
                t2Pos = Vector3Int.forward; t2Neg = Vector3Int.back;
                t2DirPos = EdgeDirection.Forward; t2DirNeg = EdgeDirection.Back;
                break;
            case EdgeDirection.Forward:
            case EdgeDirection.Back:
                // Normal is Z-axis, tangents are X and Y
                t1Pos = Vector3Int.right;   t1Neg = Vector3Int.left;
                t1DirPos = EdgeDirection.Right; t1DirNeg = EdgeDirection.Left;
                t2Pos = Vector3Int.up;      t2Neg = Vector3Int.down;
                t2DirPos = EdgeDirection.Up; t2DirNeg = EdgeDirection.Down;
                break;
            default:
                t1Pos = t1Neg = t2Pos = t2Neg = Vector3Int.zero;
                t1DirPos = t1DirNeg = t2DirPos = t2DirNeg = EdgeDirection.None;
                break;
        }
    }

    private bool CheckDiagonalAdjacency(GridEdge edge)
    {
        // For diagonal edges, decompose into component cardinal directions
        // and check if any of those cardinal faces on the same cell exist.
        // A diagonal board shares a side with each cardinal face whose axis
        // is one of the diagonal's component axes.
        foreach (EdgeDirection component in GetCardinalComponents(edge.Direction))
        {
            if (HasEdge(new GridEdge(edge.Cell, component)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Decomposes a flags-based EdgeDirection into its individual cardinal components.
    /// </summary>
    private static List<EdgeDirection> GetCardinalComponents(EdgeDirection dir)
    {
        var components = new List<EdgeDirection>();
        if ((dir & EdgeDirection.Up) != 0) components.Add(EdgeDirection.Up);
        if ((dir & EdgeDirection.Down) != 0) components.Add(EdgeDirection.Down);
        if ((dir & EdgeDirection.Left) != 0) components.Add(EdgeDirection.Left);
        if ((dir & EdgeDirection.Right) != 0) components.Add(EdgeDirection.Right);
        if ((dir & EdgeDirection.Forward) != 0) components.Add(EdgeDirection.Forward);
        if ((dir & EdgeDirection.Back) != 0) components.Add(EdgeDirection.Back);
        return components;
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
