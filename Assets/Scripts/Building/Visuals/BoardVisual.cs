using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoardVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private float _snapTriggerThickness = 0.1f;
    
    private GridEdge _edge;
    private bool _isPreview;
    private readonly List<BoardSnapZone> _snapZones = new();

    public GridEdge Edge => _edge;
    public bool IsPreview => _isPreview;

    public void Initialize(GridEdge edge)
    {
        _edge = edge;
        _isPreview = false;
        name = $"Board_{edge.Cell}_{edge.Direction}";
        
        CreateSnapZones();
    }
    
    public void CleanupSnapZones()
    {
        foreach (var zone in _snapZones)
        {
            if (zone != null)
                Destroy(zone.gameObject);
        }
        _snapZones.Clear();
    }
    
    private void OnDestroy()
    {
        CleanupSnapZones();
    }
    
    private void CreateSnapZones()
    {
        CleanupSnapZones();
        
        if (GridManager.Instance == null) return;
        
        float cellSize = GridManager.Instance.CellSize;
        List<GridEdge> adjacentEdges = GetAdjacentPlacementEdges(_edge);
        
        foreach (GridEdge adjacentEdge in adjacentEdges)
        {
            if (!GridManager.Instance.HasBoardAtFace(adjacentEdge))
            {
                BoardSnapZone zone = BoardSnapZone.Create(gameObject, adjacentEdge, cellSize, _snapTriggerThickness);
                _snapZones.Add(zone);
            }
        }
    }
    
    private static List<GridEdge> GetAdjacentPlacementEdges(GridEdge edge)
    {
        var edges = new List<GridEdge>();
        Vector3Int cell = edge.Cell;
        EdgeDirection dir = edge.Direction;
        
        GetTangentAxes(dir, 
            out Vector3Int t1Pos, out Vector3Int t1Neg, out EdgeDirection t1DirPos, out EdgeDirection t1DirNeg,
            out Vector3Int t2Pos, out Vector3Int t2Neg, out EdgeDirection t2DirPos, out EdgeDirection t2DirNeg);
        
        edges.Add(new GridEdge(cell + t1Pos, dir));
        edges.Add(new GridEdge(cell, t1DirPos));
        
        edges.Add(new GridEdge(cell + t1Neg, dir));
        edges.Add(new GridEdge(cell, t1DirNeg));
        
        edges.Add(new GridEdge(cell + t2Pos, dir));
        edges.Add(new GridEdge(cell, t2DirPos));
        
        edges.Add(new GridEdge(cell + t2Neg, dir));
        edges.Add(new GridEdge(cell, t2DirNeg));
        
        return edges;
    }
    
    private static void GetTangentAxes(EdgeDirection faceDir,
        out Vector3Int t1Pos, out Vector3Int t1Neg, out EdgeDirection t1DirPos, out EdgeDirection t1DirNeg,
        out Vector3Int t2Pos, out Vector3Int t2Neg, out EdgeDirection t2DirPos, out EdgeDirection t2DirNeg)
    {
        switch (faceDir)
        {
            case EdgeDirection.Up:
            case EdgeDirection.Down:
                t1Pos = Vector3Int.right; t1Neg = Vector3Int.left;
                t1DirPos = EdgeDirection.Right; t1DirNeg = EdgeDirection.Left;
                t2Pos = Vector3Int.forward; t2Neg = Vector3Int.back;
                t2DirPos = EdgeDirection.Forward; t2DirNeg = EdgeDirection.Back;
                break;
            case EdgeDirection.Left:
            case EdgeDirection.Right:
                t1Pos = Vector3Int.up; t1Neg = Vector3Int.down;
                t1DirPos = EdgeDirection.Up; t1DirNeg = EdgeDirection.Down;
                t2Pos = Vector3Int.forward; t2Neg = Vector3Int.back;
                t2DirPos = EdgeDirection.Forward; t2DirNeg = EdgeDirection.Back;
                break;
            case EdgeDirection.Forward:
            case EdgeDirection.Back:
                t1Pos = Vector3Int.right; t1Neg = Vector3Int.left;
                t1DirPos = EdgeDirection.Right; t1DirNeg = EdgeDirection.Left;
                t2Pos = Vector3Int.up; t2Neg = Vector3Int.down;
                t2DirPos = EdgeDirection.Up; t2DirNeg = EdgeDirection.Down;
                break;
            default:
                t1Pos = t1Neg = t2Pos = t2Neg = Vector3Int.zero;
                t1DirPos = t1DirNeg = t2DirPos = t2DirNeg = EdgeDirection.None;
                break;
        }
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
