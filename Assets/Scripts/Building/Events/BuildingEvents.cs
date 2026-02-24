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
