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

        if (gridManager.BoardCount == 0)
        {
            return GetFreePlacementEdge(origin, direction, maxDistance, gridManager);
        }

        EdgeHit freePlacement = GetFreePlacementEdge(origin, direction, maxDistance, gridManager);
        if (gridManager.HasAdjacentBoard(freePlacement.Edge))
        {
            return new EdgeHit(freePlacement.Edge, true, freePlacement.WorldPosition);
        }

        return EdgeHit.Invalid;
    }

    private static EdgeHit GetFreePlacementEdge(Vector3 origin, Vector3 direction, float distance, GridManager gridManager)
    {
        Vector3 placePos = origin + direction * (distance * 0.5f);
        Vector3Int cell = gridManager.WorldToCell(placePos);
        
        EdgeDirection edgeDir = DetermineFreePlacementDirection(direction);
        GridEdge edge = new GridEdge(cell, edgeDir);
        Vector3 worldPos = edge.GetWorldPosition(gridManager.CellSize);
        
        return new EdgeHit(edge, true, worldPos);
    }

    private static EdgeDirection DetermineFreePlacementDirection(Vector3 lookDirection)
    {
        float ax = Mathf.Abs(lookDirection.x);
        float ay = Mathf.Abs(lookDirection.y);
        float az = Mathf.Abs(lookDirection.z);

        if (ay >= ax && ay >= az)
            return lookDirection.y > 0 ? EdgeDirection.Down : EdgeDirection.Up;
        if (ax >= az)
            return lookDirection.x > 0 ? EdgeDirection.Left : EdgeDirection.Right;
        return lookDirection.z > 0 ? EdgeDirection.Back : EdgeDirection.Forward;
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
