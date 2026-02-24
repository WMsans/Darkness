# Building System Design

## Overview

A raft-like edge-based building system where players place uniform boards on a 4m grid. The system is decoupled from inventory, oxygen, and style systems via events.

## Requirements Summary

- **Placement**: Snap-to-grid with diagonal support, edge-based positioning
- **Grid scale**: 4m per cell
- **Board types**: Single uniform type, orientation determined by edge position
- **Interaction**: First-person aiming, ghost preview before placement
- **Removal**: Destructive (boards destroyed, no refund)
- **Decoupling**: Events/callbacks for external system integration

## Architecture

### Approach: GridManager + EdgeData

Single `GridManager` holds a dictionary of edge states. Each edge position is encoded as a `GridEdge` struct. The `BoardPlacer` component handles player interaction and raycasting, firing events for external systems.

## Core Data Structures

### GridEdge

```csharp
public readonly struct GridEdge : IEquatable<GridEdge>
{
    public Vector3Int Cell { get; }
    public EdgeDirection Direction { get; }
}
```

- `Cell`: The grid cell this edge belongs to (Vector3Int)
- `Direction`: Which face/edge of the cell
- Edges are uniquely identified by cell + direction
- Adjacent cells share edges (same logical edge, different perspective)

### EdgeDirection

```csharp
public enum EdgeDirection
{
    // Orthogonal (6)
    Up, Down, Left, Right, Forward, Back,
    
    // Diagonal (12) - horizontal plane
    UpLeft, UpRight, UpForward, UpBack,
    DownLeft, DownRight, DownForward, DownBack,
    LeftForward, LeftBack, RightForward, RightBack,
    
    // 3D diagonals (8) - optional, if needed
    UpLeftForward, UpLeftBack, UpRightForward, UpRightBack,
    DownLeftForward, DownLeftBack, DownRightForward, DownRightBack
}
```

### BoardData

```csharp
public readonly struct BoardData
{
    public string StyleId { get; }
    public float PlacedTime { get; }
    public object CustomData { get; } // Extensible for future systems
}
```

### GridManager

Central singleton managing all placed boards.

```csharp
public class GridManager : MonoBehaviour
{
    private Dictionary<GridEdge, BoardData> _edges = new();
    
    public bool TryPlaceBoard(GridEdge edge, BoardData data);
    public void RemoveBoard(GridEdge edge);
    public bool HasEdge(GridEdge edge);
    public IEnumerable<GridEdge> GetAllEdges();
    public BoardData? GetBoardData(GridEdge edge);
}
```

## Player Interaction

### BoardPlacer Component

Attached to player, handles input and raycasting.

**Flow:**
1. Raycast from camera forward
2. `EdgeDetector` identifies target `GridEdge` from hit point
3. `BoardPreview` shows ghost at target edge
4. On place input: validate and call `GridManager.TryPlaceBoard()`
5. On remove input: call `GridManager.RemoveBoard()`

**Validation rules:**
- Must connect to at least one existing board (or starter platform)
- Cannot place on occupied edge
- Diagonal edges must have both endpoint cells occupied

### EdgeDetector Utility

Converts raycast hits to `GridEdge` candidates.

```csharp
public struct EdgeHit
{
    public GridEdge Edge;
    public bool IsValid;
    public Vector3 WorldPosition;
}

public static class EdgeDetector
{
    public static EdgeHit DetectEdge(Vector3 origin, Vector3 direction, float maxDistance);
}
```

### BoardPreview Component

Manages ghost preview GameObject.

- Enabled when valid edge detected
- Position/rotation matches target edge
- Material set to transparent/ghost shader
- Color changes based on validity (green valid, red invalid)

## Events & Decoupling

### BuildingEvents

ScriptableObject asset for event distribution.

```csharp
[CreateAssetMenu]
public class BuildingEvents : ScriptableObject
{
    public event Action<GridEdge, BoardData> OnBoardPlaced;
    public event Action<GridEdge> OnBoardRemoved;
    public event Action<GridEdge?> OnPreviewChanged;
}
```

### Integration Points

| System | Integration Method |
|--------|-------------------|
| Inventory | Subscribe to `OnBoardPlaced`, deduct resources in callback |
| Oxygen | Query `GetAllEdges()` to build enclosure graph, listen for changes |
| Style | `BoardData.StyleId` references Style system's visual definitions |

The building system does NOT implement these systems - it only provides the hooks.

## File Structure

```
Assets/Scripts/Building/
├── Core/
│   ├── GridCell.cs           # Vector3Int alias for clarity
│   ├── EdgeDirection.cs      # Enum for edge directions
│   ├── GridEdge.cs           # Struct: cell + direction
│   ├── BoardData.cs          # Struct: board state info
│   └── GridManager.cs        # Singleton, stores all edges
├── Placement/
│   ├── EdgeDetector.cs       # Raycast → GridEdge utility
│   ├── BoardPlacer.cs        # Player-facing placement component
│   └── BoardPreview.cs       # Ghost preview renderer
├── Events/
│   └── BuildingEvents.cs     # ScriptableObject with events
└── Visuals/
    └── BoardRenderer.cs      # Handles board mesh/style selection
```

## Grid Coordinate System

- **Cell size**: 4 Unity units (4m)
- **Cell center**: Grid coordinates × 4
- **Edge position**: Cell center + (direction × 2) for orthogonal edges
- **Diagonal edges**: Cell center + combined direction offset

Example: Cell (0, 0, 0), Direction.Up → Edge at world position (0, 2, 0)

## Placement Flow Sequence

```
Player Input → BoardPlacer.Raycast()
    → EdgeDetector.DetectEdge()
    → BoardPreview.Update(edge, isValid)
    
On Place Input:
    → GridManager.TryPlaceBoard(edge, data)
        → Validate adjacency
        → Add to dictionary
        → Instantiate visual
        → Fire OnBoardPlaced event
    → BoardPreview.Clear()

On Remove Input:
    → GridManager.RemoveBoard(edge)
        → Destroy visual
        → Remove from dictionary
        → Fire OnBoardRemoved event
```
