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
