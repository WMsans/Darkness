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
