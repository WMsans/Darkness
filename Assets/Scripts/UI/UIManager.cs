using UnityEngine;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("UI 设置")]
    public CanvasGroup pauseMenuCanvasGroup;
    public RectTransform menuContent; // 建议将 UI 内容放入一个子节点，方便做位移动效
    public CanvasGroup techTreePanel;

    [Header("相机设置")]
    public float pauseFOV = 50f;     // 暂停时拉近视角
    private float defaultFOV;
    private Camera mainCam;

    [Header("控制脚本引用")]
    // 在此处拖入你的角色控制或相机控制脚本，如果没有，代码会尝试自动查找
    public MonoBehaviour cameraControlScript; 
    
    [Header("科技树配置")]
    // 科技树里面的 Content 物体
    public RectTransform techTreeContent;

    private bool isPaused = false;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) defaultFOV = mainCam.fieldOfView;
        
        // 初始化 UI 状态
        pauseMenuCanvasGroup.alpha = 0;
        pauseMenuCanvasGroup.interactable = false;
        pauseMenuCanvasGroup.blocksRaycasts = false;
        pauseMenuCanvasGroup.gameObject.SetActive(false);
        if (menuContent != null) menuContent.gameObject.SetActive(false);
    }

    void Update()
    {
        // 兼容新旧输入系统
        bool tabPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame) tabPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Tab)) tabPressed = true;
#endif

        if (tabPressed)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // 1. 处理鼠标状态：显示并解锁
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 2. 禁用视角旋转：如果手动拖入了脚本则禁用，否则尝试自动查找
        if (cameraControlScript != null) cameraControlScript.enabled = false;
        else TogglePlayerControl(false);

        // 3. UI 动画：模拟《终末地》那种微小位移+缩放
        pauseMenuCanvasGroup.gameObject.SetActive(true);
        if (menuContent != null) menuContent.gameObject.SetActive(true);
        pauseMenuCanvasGroup.DOKill();
        pauseMenuCanvasGroup.DOFade(1f, 0.4f).SetUpdate(true);
        
        // 增加视觉聚焦效果 (FOV)
        if (mainCam != null) mainCam.DOFieldOfView(pauseFOV, 0.5f).SetEase(Ease.OutQuint).SetUpdate(true);

        // 内容从下方轻微弹入
        if (menuContent != null)
        {
            menuContent.DOKill();
            menuContent.anchoredPosition = new Vector2(0, -20f);
            menuContent.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.OutQuint).SetUpdate(true);
            menuContent.DOScale(1f, 0.4f).From(0.98f).SetEase(Ease.OutQuint).SetUpdate(true);
        }

        pauseMenuCanvasGroup.interactable = true;
        pauseMenuCanvasGroup.blocksRaycasts = true;
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // 1. 处理鼠标状态：隐藏并锁定（适合第一人称生存游戏）
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 2. 恢复视角旋转
        if (cameraControlScript != null) cameraControlScript.enabled = true;
        else TogglePlayerControl(true);

        // 3. UI 退场动效
        pauseMenuCanvasGroup.DOKill();
        if (mainCam != null) mainCam.DOFieldOfView(defaultFOV, 0.3f).SetUpdate(true);

        pauseMenuCanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() => {
            pauseMenuCanvasGroup.gameObject.SetActive(false);
            if (menuContent != null) menuContent.gameObject.SetActive(false);
            isPaused = false;
        });

        pauseMenuCanvasGroup.interactable = false;
        pauseMenuCanvasGroup.blocksRaycasts = false;
    }

    // 自动寻找并禁用常见的控制脚本（如 Cinemachine 或 SimpleCameraController）
    void TogglePlayerControl(bool state)
    {
        var playerController = cameraControlScript as MonoBehaviour;
        if (playerController != null) playerController.enabled = state;
        var placerOnPlayer = playerController != null ? playerController.GetComponent<BoardPlacer>() : null;
        if (placerOnPlayer != null) placerOnPlayer.SetPlacementEnabled(state);
        else
        {
            var placers = FindObjectsOfType<BoardPlacer>(true);
            foreach (var p in placers) p.SetPlacementEnabled(state);
        }
    }
    public void BackToMenu()
    { 
        // 1. 让科技树面板淡出并禁用交互
        techTreePanel.interactable = false;
        techTreePanel.blocksRaycasts = false;
    }
}
