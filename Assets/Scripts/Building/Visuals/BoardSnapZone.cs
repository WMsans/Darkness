using UnityEngine;

public class BoardSnapZone : MonoBehaviour
{
    private static int _snapZoneLayer = -1;
    
    [SerializeField] private GridEdge _targetEdge;
    
    public GridEdge TargetEdge => _targetEdge;

    public void Initialize(GridEdge targetEdge, float cellSize)
    {
        _targetEdge = targetEdge;
        name = $"SnapZone_{targetEdge.Cell}_{targetEdge.Direction}";
        
        Vector3 worldPos = targetEdge.GetWorldPosition(cellSize);
        transform.position = worldPos;
        transform.rotation = GridManager.GetBoardRotation(targetEdge.Direction);
        
        if (_snapZoneLayer < 0)
        {
            _snapZoneLayer = LayerMask.NameToLayer("Default");
        }
        if (_snapZoneLayer >= 0)
        {
            gameObject.layer = _snapZoneLayer;
        }
    }

    public static BoardSnapZone Create(GameObject parent, GridEdge targetEdge, float cellSize, float triggerSize)
    {
        GameObject zoneObj = new GameObject("SnapZone");
        zoneObj.transform.SetParent(parent.transform);
        zoneObj.layer = parent.layer;
        
        BoxCollider trigger = zoneObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(cellSize, triggerSize, cellSize);
        
        BoardSnapZone zone = zoneObj.AddComponent<BoardSnapZone>();
        zone.Initialize(targetEdge, cellSize);
        
        return zone;
    }
}
