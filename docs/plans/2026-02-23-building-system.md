# Building System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a raft-like edge-based building system where players place uniform boards on a 4m grid, decoupled from inventory/oxygen/style systems via events.

**Architecture:** GridManager singleton stores edge dictionary with GridEdge keys. BoardPlacer component handles player raycasting and input. BuildingEvents ScriptableObject provides decoupling via C# events.

**Tech Stack:** Unity 6, New Input System, Rigidbody physics

---

### Task 1: Folder Structure & Input Actions

**Files:**
- Create: `Assets/Scripts/Building/`
- Create: `Assets/Scripts/Building/Core/`
- Create: `Assets/Scripts/Building/Placement/`
- Create: `Assets/Scripts/Building/Events/`
- Create: `Assets/Scripts/Building/Visuals/`
- Modify: `Assets/InputSystem_Actions.inputactions` (add PlaceBoard, RemoveBoard actions)

**Step 1: Create folder structure**

In Unity Project window, create folders:
```
Assets/Scripts/Building/
Assets/Scripts/Building/Core/
Assets/Scripts/Building/Placement/
Assets/Scripts/Building/Events/
Assets/Scripts/Building/Visuals/
```

**Step 2: Add input actions**

In Unity Editor:
1. Open `Assets/InputSystem_Actions.inputactions`
2. In Player action map, add:
   - `PlaceBoard` (Button type) - suggested binding: Left Mouse Button
   - `RemoveBoard` (Button type) - suggested binding: Right Mouse Button
3. Click "Generate C# Class" to regenerate InputSystem_Actions.cs

**Step 3: Commit**

```bash
git add Assets/Scripts/Building/ Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.cs
git commit -m "chore: setup building system folder structure and input actions"
```

---

### Task 2: Core Data Structures - EdgeDirection

**Files:**
- Create: `Assets/Scripts/Building/Core/EdgeDirection.cs`

**Step 1: Write EdgeDirection enum**

```csharp
public enum EdgeDirection
{
    None = 0,
    
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Forward = 16,
    Back = 32,
    
    UpLeft = Up | Left,
    UpRight = Up | Right,
    UpForward = Up | Forward,
    UpBack = Up | Back,
    DownLeft = Down | Left,
    DownRight = Down | Right,
    DownForward = Down | Forward,
    DownBack = Down | Back,
    LeftForward = Left | Forward,
    LeftBack = Left | Back,
    RightForward = Right | Forward,
    RightBack = Right | Back,
    
    UpLeftForward = Up | Left | Forward,
    UpLeftBack = Up | Left | Back,
    UpRightForward = Up | Right | Forward,
    UpRightBack = Up | Right | Back,
    DownLeftForward = Down | Left | Forward,
    DownLeftBack = Down | Left | Back,
    DownRightForward = Down | Right | Forward,
    DownRightBack = Down | Right | Back
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Core/EdgeDirection.cs
git commit -m "feat: add EdgeDirection enum for grid edges"
```

---

### Task 3: Core Data Structures - GridEdge

**Files:**
- Create: `Assets/Scripts/Building/Core/GridEdge.cs`

**Step 1: Write GridEdge struct**

```csharp
using System;
using UnityEngine;

[System.Serializable]
public readonly struct GridEdge : IEquatable<GridEdge>
{
    public readonly Vector3Int Cell;
    public readonly EdgeDirection Direction;

    public GridEdge(Vector3Int cell, EdgeDirection direction)
    {
        Cell = cell;
        Direction = direction;
    }

    public GridEdge(int x, int y, int z, EdgeDirection direction)
        : this(new Vector3Int(x, y, z), direction) { }

    public bool Equals(GridEdge other) => Cell.Equals(other.Cell) && Direction == other.Direction;
    public override bool Equals(object obj) => obj is GridEdge other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Cell, Direction);
    public override string ToString() => $"GridEdge({Cell}, {Direction})";

    public static bool operator ==(GridEdge left, GridEdge right) => left.Equals(right);
    public static bool operator !=(GridEdge left, GridEdge right) => !left.Equals(right);

    public Vector3 GetWorldPosition(float cellSize)
    {
        Vector3 offset = GetDirectionOffset(Direction) * (cellSize * 0.5f);
        return new Vector3(Cell.x, Cell.y, Cell.z) * cellSize + offset;
    }

    public static Vector3 GetDirectionOffset(EdgeDirection direction)
    {
        return direction switch
        {
            EdgeDirection.Up => Vector3.up,
            EdgeDirection.Down => Vector3.down,
            EdgeDirection.Left => Vector3.left,
            EdgeDirection.Right => Vector3.right,
            EdgeDirection.Forward => Vector3.forward,
            EdgeDirection.Back => Vector3.back,
            EdgeDirection.UpLeft => (Vector3.up + Vector3.left).normalized,
            EdgeDirection.UpRight => (Vector3.up + Vector3.right).normalized,
            EdgeDirection.UpForward => (Vector3.up + Vector3.forward).normalized,
            EdgeDirection.UpBack => (Vector3.up + Vector3.back).normalized,
            EdgeDirection.DownLeft => (Vector3.down + Vector3.left).normalized,
            EdgeDirection.DownRight => (Vector3.down + Vector3.right).normalized,
            EdgeDirection.DownForward => (Vector3.down + Vector3.forward).normalized,
            EdgeDirection.DownBack => (Vector3.down + Vector3.back).normalized,
            EdgeDirection.LeftForward => (Vector3.left + Vector3.forward).normalized,
            EdgeDirection.LeftBack => (Vector3.left + Vector3.back).normalized,
            EdgeDirection.RightForward => (Vector3.right + Vector3.forward).normalized,
            EdgeDirection.RightBack => (Vector3.right + Vector3.back).normalized,
            _ => Vector3.zero
        };
    }

    public static EdgeDirection GetOpposite(EdgeDirection direction)
    {
        return direction switch
        {
            EdgeDirection.Up => EdgeDirection.Down,
            EdgeDirection.Down => EdgeDirection.Up,
            EdgeDirection.Left => EdgeDirection.Right,
            EdgeDirection.Right => EdgeDirection.Left,
            EdgeDirection.Forward => EdgeDirection.Back,
            EdgeDirection.Back => EdgeDirection.Forward,
            _ => EdgeDirection.None
        };
    }

    public bool IsDiagonal()
    {
        int count = 0;
        EdgeDirection d = Direction;
        if ((d & EdgeDirection.Up) != 0) count++;
        if ((d & EdgeDirection.Down) != 0) count++;
        if ((d & EdgeDirection.Left) != 0) count++;
        if ((d & EdgeDirection.Right) != 0) count++;
        if ((d & EdgeDirection.Forward) != 0) count++;
        if ((d & EdgeDirection.Back) != 0) count++;
        return count > 1;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Core/GridEdge.cs
git commit -m "feat: add GridEdge struct for edge identification"
```

---

### Task 4: Core Data Structures - BoardData

**Files:**
- Create: `Assets/Scripts/Building/Core/BoardData.cs`

**Step 1: Write BoardData struct**

```csharp
using System;
using UnityEngine;

[System.Serializable]
public readonly struct BoardData : IEquatable<BoardData>
{
    public readonly string StyleId;
    public readonly float PlacedTime;
    public readonly object CustomData;

    public BoardData(string styleId, float placedTime = 0f, object customData = null)
    {
        StyleId = styleId ?? "default";
        PlacedTime = placedTime;
        CustomData = customData;
    }

    public bool Equals(BoardData other) => StyleId == other.StyleId && 
                                           Mathf.Approximately(PlacedTime, other.PlacedTime);
    public override bool Equals(object obj) => obj is BoardData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(StyleId, PlacedTime);
    public override string ToString() => $"BoardData({StyleId})";

    public static BoardData Default => new BoardData("default");
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Core/BoardData.cs
git commit -m "feat: add BoardData struct for board state"
```

---

### Task 5: Events - BuildingEvents

**Files:**
- Create: `Assets/Scripts/Building/Events/BuildingEvents.cs`

**Step 1: Write BuildingEvents ScriptableObject**

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingEvents", menuName = "Building/BuildingEvents")]
public class BuildingEvents : ScriptableObject
{
    public event Action<GridEdge, BoardData> OnBoardPlaced;
    public event Action<GridEdge> OnBoardRemoved;
    public event Action<GridEdge?> OnPreviewChanged;

    public void RaiseBoardPlaced(GridEdge edge, BoardData data)
    {
        OnBoardPlaced?.Invoke(edge, data);
    }

    public void RaiseBoardRemoved(GridEdge edge)
    {
        OnBoardRemoved?.Invoke(edge);
    }

    public void RaisePreviewChanged(GridEdge? edge)
    {
        OnPreviewChanged?.Invoke(edge);
    }
}
```

**Step 2: Create BuildingEvents asset**

In Unity Editor:
1. Right-click in `Assets/Scripts/Building/Events/`
2. Create > Building > BuildingEvents
3. Name it "BuildingEvents"

**Step 3: Commit**

```bash
git add Assets/Scripts/Building/Events/
git commit -m "feat: add BuildingEvents ScriptableObject for decoupled events"
```

---

### Task 6: Core - GridManager

**Files:**
- Create: `Assets/Scripts/Building/Core/GridManager.cs`

**Step 1: Write GridManager**

```csharp
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
        if (edge.IsDiagonal())
        {
            return CheckDiagonalAdjacency(edge);
        }

        GridEdge opposite = new GridEdge(edge.Cell, GridEdge.GetOpposite(edge.Direction));
        Vector3Int neighborCell = edge.Cell + Vector3Int.RoundToInt(GridEdge.GetDirectionOffset(edge.Direction));
        GridEdge neighborEdge = new GridEdge(neighborCell, GridEdge.GetOpposite(edge.Direction));

        return HasEdge(opposite) || HasEdge(neighborEdge) || HasAnyBoardInCell(edge.Cell);
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
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Core/GridManager.cs
git commit -m "feat: add GridManager singleton for edge storage"
```

---

### Task 7: Placement - EdgeDetector

**Files:**
- Create: `Assets/Scripts/Building/Placement/EdgeDetector.cs`

**Step 1: Write EdgeDetector**

```csharp
using UnityEngine;

public readonly struct EdgeHit
{
    public readonly GridEdge Edge;
    public readonly bool IsValid;
    public readonly Vector3 WorldPosition;

    public EdgeHit(GridEdge edge, bool isValid, Vector3 worldPosition)
    {
        Edge = edge;
        IsValid = isValid;
        WorldPosition = worldPosition;
    }

    public static EdgeHit Invalid => new EdgeHit(default, false, Vector3.zero);
}

public static class EdgeDetector
{
    public static EdgeHit DetectEdge(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        LayerMask layerMask,
        GridManager gridManager)
    {
        if (gridManager == null)
            return EdgeHit.Invalid;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
        {
            return DetectFromHit(hit, gridManager);
        }

        return EdgeHit.Invalid;
    }

    private static EdgeHit DetectFromHit(RaycastHit hit, GridManager gridManager)
    {
        Vector3 hitPoint = hit.point;
        Vector3 normal = hit.normal;

        Vector3Int hitCell = gridManager.WorldToCell(hitPoint);
        Vector3 cellCenter = gridManager.CellToWorld(hitCell);

        Vector3 localHitPoint = hitPoint - cellCenter;
        float cellHalfSize = gridManager.CellSize * 0.5f;

        EdgeDirection direction = DetermineEdgeDirection(localHitPoint, cellHalfSize);

        GridEdge edge = new GridEdge(hitCell, direction);
        Vector3 worldPos = edge.GetWorldPosition(gridManager.CellSize);
        bool isValid = gridManager.HasAdjacentBoard(edge);

        return new EdgeHit(edge, isValid, worldPos);
    }

    private static EdgeDirection DetermineEdgeDirection(Vector3 localPos, float halfSize)
    {
        float threshold = halfSize * 0.3f;

        float ax = Mathf.Abs(localPos.x);
        float ay = Mathf.Abs(localPos.y);
        float az = Mathf.Abs(localPos.z);

        bool nearX = ax > halfSize - threshold;
        bool nearY = ay > halfSize - threshold;
        bool nearZ = az > halfSize - threshold;

        EdgeDirection dir = EdgeDirection.None;

        if (nearX)
            dir |= localPos.x > 0 ? EdgeDirection.Right : EdgeDirection.Left;
        else if (nearY)
            dir |= localPos.y > 0 ? EdgeDirection.Up : EdgeDirection.Down;
        else if (nearZ)
            dir |= localPos.z > 0 ? EdgeDirection.Forward : EdgeDirection.Back;

        if (dir == EdgeDirection.None)
        {
            if (ax >= ay && ax >= az)
                dir = localPos.x > 0 ? EdgeDirection.Right : EdgeDirection.Left;
            else if (ay >= az)
                dir = localPos.y > 0 ? EdgeDirection.Up : EdgeDirection.Down;
            else
                dir = localPos.z > 0 ? EdgeDirection.Forward : EdgeDirection.Back;
        }

        return dir;
    }

    public static GridEdge? DetectEdgeForRemoval(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        LayerMask layerMask,
        GridManager gridManager)
    {
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            return null;

        BoardVisual boardVisual = hit.collider.GetComponentInParent<BoardVisual>();
        if (boardVisual != null)
        {
            return boardVisual.Edge;
        }

        return null;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Placement/EdgeDetector.cs
git commit -m "feat: add EdgeDetector for raycast-to-edge conversion"
```

---

### Task 8: Visuals - BoardVisual

**Files:**
- Create: `Assets/Scripts/Building/Visuals/BoardVisual.cs`

**Step 1: Write BoardVisual**

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoardVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    
    private GridEdge _edge;
    private bool _isPreview;

    public GridEdge Edge => _edge;
    public bool IsPreview => _isPreview;

    public void Initialize(GridEdge edge)
    {
        _edge = edge;
        _isPreview = false;
        name = $"Board_{edge.Cell}_{edge.Direction}";
    }

    public void SetPreviewMode(bool isPreview)
    {
        _isPreview = isPreview;
        
        if (_meshRenderer != null)
        {
            foreach (var mat in _meshRenderer.materials)
            {
                if (isPreview)
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
                }
            }
        }
    }

    public void SetValidHighlight(bool isValid)
    {
        if (_meshRenderer == null) return;

        foreach (var mat in _meshRenderer.materials)
        {
            mat.color = isValid 
                ? new Color(0.2f, 1f, 0.2f, _isPreview ? 0.5f : 1f)
                : new Color(1f, 0.2f, 0.2f, _isPreview ? 0.5f : 1f);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Visuals/BoardVisual.cs
git commit -m "feat: add BoardVisual component for board GameObjects"
```

---

### Task 9: Placement - BoardPreview

**Files:**
- Create: `Assets/Scripts/Building/Placement/BoardPreview.cs`

**Step 1: Write BoardPreview**

```csharp
using UnityEngine;

public class BoardPreview : MonoBehaviour
{
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField] private Material _validMaterial;
    [SerializeField] private Material _invalidMaterial;

    private GameObject _previewObject;
    private MeshRenderer[] _previewRenderers;
    private bool _isVisible;

    public bool IsVisible => _isVisible;
    public GridEdge? CurrentEdge { get; private set; }

    private void Awake()
    {
        if (_previewPrefab != null)
        {
            _previewObject = Instantiate(_previewPrefab, transform);
            _previewObject.name = "BoardPreview";
            _previewRenderers = _previewObject.GetComponentsInChildren<MeshRenderer>();
            SetVisible(false);
        }
    }

    public void Show(GridEdge edge, Vector3 worldPosition, Quaternion rotation, bool isValid)
    {
        if (_previewObject == null) return;

        CurrentEdge = edge;
        _previewObject.transform.position = worldPosition;
        _previewObject.transform.rotation = rotation;

        ApplyMaterial(isValid ? _validMaterial : _invalidMaterial);
        SetVisible(true);
    }

    public void Hide()
    {
        CurrentEdge = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (_previewObject != null)
        {
            _previewObject.SetActive(visible);
        }
    }

    private void ApplyMaterial(Material material)
    {
        if (_previewRenderers == null || material == null) return;

        foreach (var renderer in _previewRenderers)
        {
            renderer.material = material;
        }
    }

    private void OnDestroy()
    {
        if (_previewObject != null)
        {
            Destroy(_previewObject);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Building/Placement/BoardPreview.cs
git commit -m "feat: add BoardPreview for ghost preview rendering"
```

---

### Task 10: Placement - BoardPlacer

**Files:**
- Create: `Assets/Scripts/Building/Placement/BoardPlacer.cs`

**Step 1: Update PlayerInput**

First, add the new input actions to `Assets/Scripts/Player/PlayerInput.cs`:

```csharp
public bool PlaceBoardPressed => _actions.Player.PlaceBoard.WasPressedThisFrame();
public bool RemoveBoardPressed => _actions.Player.RemoveBoard.WasPressedThisFrame();
public bool PlaceBoardHeld => _actions.Player.PlaceBoard.IsPressed();
```

**Step 2: Write BoardPlacer**

```csharp
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

        if (_currentEdgeHit.IsValid || _currentEdgeHit.Edge.Direction != EdgeDirection.None)
        {
            Quaternion rotation = GetBoardRotation(_currentEdgeHit.Edge.Direction);
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

        if (_gridManager.HasEdge(_currentEdgeHit.Edge))
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

    private Quaternion GetBoardRotation(EdgeDirection direction)
    {
        return direction switch
        {
            EdgeDirection.Up => Quaternion.Euler(-90, 0, 0),
            EdgeDirection.Down => Quaternion.Euler(90, 0, 0),
            EdgeDirection.Left => Quaternion.Euler(0, 0, 90),
            EdgeDirection.Right => Quaternion.Euler(0, 0, -90),
            EdgeDirection.Forward => Quaternion.Euler(0, 0, 0),
            EdgeDirection.Back => Quaternion.Euler(0, 180, 0),
            _ => Quaternion.LookRotation(GridEdge.GetDirectionOffset(direction))
        };
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
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Building/Placement/BoardPlacer.cs Assets/Scripts/Player/PlayerInput.cs
git commit -m "feat: add BoardPlacer component for player building interaction"
```

---

### Task 11: Create Board Prefab

**Files:**
- Create: `Assets/Prefabs/Building/Board.prefab`

**Step 1: Create board mesh**

In Unity Editor:
1. Create a new Cube GameObject
2. Scale to (4, 0.1, 4) for a 4m x 4m board
3. Add BoxCollider component
4. Add BoardVisual component
5. Create material for the board (metal/panel look)
6. Save as prefab at `Assets/Prefabs/Building/Board.prefab`

**Step 2: Create preview material**

1. Create a transparent material at `Assets/Materials/BoardPreview_Valid.mat`
   - Set to Transparent rendering mode
   - Color: green with 50% alpha
2. Create `Assets/Materials/BoardPreview_Invalid.mat`
   - Color: red with 50% alpha

**Step 3: Commit**

```bash
git add Assets/Prefabs/Building/ Assets/Materials/
git commit -m "feat: add board prefab and preview materials"
```

---

### Task 12: Scene Setup & Testing

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

**Step 1: Create GridManager GameObject**

In Unity Editor:
1. Create empty GameObject named "GridManager"
2. Add GridManager component
3. Assign BuildingEvents asset
4. Assign Board prefab
5. Set Cell Size to 4

**Step 2: Create starter platform**

1. Place a few boards manually to form a starter platform:
   - Board at (0, 0, 0) with Up direction (floor)
   - Or use a simple cube as initial ground

**Step 3: Add BoardPlacer to Player**

1. Select Player GameObject
2. Add BoardPlacer component
3. Assign camera transform
4. Assign GridManager reference
5. Assign BuildingEvents reference
6. Create child GameObject with BoardPreview component
   - Assign Board prefab as preview prefab
  - Assign preview materials

**Step 4: Configure layers**

1. Create/assign "Placeable" layer for board colliders
2. Set Place Layer on BoardPlacer to include Placeable layer

**Step 5: Play test**

1. Enter Play mode
2. Aim at existing board edges - should see preview
3. Left-click to place boards (should snap to edges)
4. Right-click on boards to remove them
5. Verify boards connect properly
6. Test diagonal edge placement

**Step 6: Commit**

```bash
git add Assets/Scenes/
git commit -m "feat: setup building system in scene"
```

---

### Task 13: Final Testing & Cleanup

**Step 1: Full manual test**

Run through all test cases:
- [ ] Preview shows when aiming at valid edges
- [ ] Preview color changes based on validity
- [ ] Left-click places board at preview location
- [ ] Cannot place on occupied edges
- [ ] Right-click removes boards
- [ ] Events fire correctly (check console or add debug logs)
- [ ] Grid coordinate conversion is accurate
- [ ] Diagonal edges work correctly
- [ ] No memory leaks when placing/removing many boards

**Step 2: Fix any issues found**

If bugs found, create fix commits.

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat: complete building system implementation"
```
