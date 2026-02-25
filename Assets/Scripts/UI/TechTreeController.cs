using UnityEngine;
using DG.Tweening; // 使用 DOTween

public class TechTreeController : MonoBehaviour
{
    public CanvasGroup techTreeCG; // 拖入 TechTreePanel
    public RectTransform techTreeContent; // 拖入 Scroll View 下的 Content
    public GameObject exitButton;
    
    public void ShowTechTree()
    {
        // 激活并显示
        techTreeCG.gameObject.SetActive(true);
        techTreeCG.DOFade(1f, 0.4f).SetUpdate(true);
        
        // 科技树内容稍微有一个从下往上浮现的效果
        techTreeContent.anchoredPosition = new Vector2(0, -50f);
        techTreeContent.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutQuint).SetUpdate(true);

        techTreeCG.interactable = true;
        techTreeCG.blocksRaycasts = true;
        
        exitButton.SetActive(true); 
    }

    public void HideTechTree()
    {
        techTreeCG.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() => {
            techTreeCG.gameObject.SetActive(false);
        });
        techTreeCG.interactable = false;
        techTreeCG.blocksRaycasts = false;
        
        exitButton.SetActive(false); 
    }
}