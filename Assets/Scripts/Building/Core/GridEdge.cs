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
