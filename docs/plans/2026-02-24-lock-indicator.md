# Lock Indicator Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a screen-space UI indicator that displays around locked targets in zero-gravity mode, showing distance, velocity, and object name with Outer Wilds-style visuals.

**Architecture:** Event-driven UI system. TargetLock emits events when locking/releasing/candidate changes. LockIndicatorUI subscribes and updates canvas indicators. Two indicator prefabs handle candidate (dim) and locked (full info) states.

**Tech Stack:** Unity UI (Canvas), TextMeshPro, DOTween

---

### Task 1: Create LockEvents static class

**Files:**
- Create: `Assets/Scripts/Player/LockEvents.cs`

**Step 1: Create LockEvents.cs**

```csharp
using System;
using UnityEngine;

public static class LockEvents
{
    public static event Action<Collider> OnTargetLocked;
    public static event Action OnTargetReleased;
    public static event Action<Collider> OnCandidateChanged;

    public static void TargetLocked(Collider target) => OnTargetLocked?.Invoke(target);
    public static void TargetReleased() => OnTargetReleased?.Invoke();
    public static void CandidateChanged(Collider candidate) => OnCandidateChanged?.Invoke(candidate);
}
```

**Step 2: Verify file compiles**

In Unity Editor, check Console for no errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/Player/LockEvents.cs
git commit -m "feat: add LockEvents static class for decoupled lock signaling"
```

---

### Task 2: Add candidate detection to TargetLock

**Files:**
- Modify: `Assets/Scripts/Player/TargetLock.cs`

**Step 1: Add candidate property and detection**

Add to TargetLock class:

```csharp
private Collider _currentCandidate;

public Collider CurrentCandidate => _currentCandidate;

public void UpdateCandidate()
{
    if (_lockedTarget != null)
    {
        _currentCandidate = null;
        return;
    }

    if (Physics.SphereCast(
        _cameraTransform.position,
        _lockRadius,
        _cameraTransform.forward,
        out RaycastHit hit,
        _lockRange))
    {
        if (_currentCandidate != hit.collider)
        {
            _currentCandidate = hit.collider;
            LockEvents.CandidateChanged(_currentCandidate);
        }
    }
    else if (_currentCandidate != null)
    {
        _currentCandidate = null;
        LockEvents.CandidateChanged(null);
    }
}
```

**Step 2: Add event invocations to TryLock and Release**

Modify TryLock:

```csharp
public void TryLock()
{
    if (Physics.SphereCast(
        _cameraTransform.position,
        _lockRadius,
        _cameraTransform.forward,
        out RaycastHit hit,
        _lockRange))
    {
        _lockedTarget = hit.collider;
        _currentCandidate = null;
        LockEvents.TargetLocked(_lockedTarget);
        LockEvents.CandidateChanged(null);
    }
}
```

Modify Release:

```csharp
public void Release()
{
    _lockedTarget = null;
    LockEvents.TargetReleased();
}
```

**Step 3: Verify file compiles**

In Unity Editor, check Console for no errors.

**Step 4: Commit**

```bash
git add Assets/Scripts/Player/TargetLock.cs
git commit -m "feat: add candidate detection and events to TargetLock"
```

---

### Task 3: Call UpdateCandidate from PlayerController

**Files:**
- Modify: `Assets/Scripts/Player/PlayerController.cs`

**Step 1: Add UpdateCandidate call in Update**

In the Update method, after `HandleLockTarget()`:

```csharp
private void Update()
{
    _input.Update();
    _toggleCooldown -= Time.deltaTime;

    HandleToggleGravity();
    HandleLockTarget();
    UpdateCamera();

    if (!_isGravityEnabled)
    {
        _targetLock.CheckRelease(transform);
        _targetLock.UpdateCandidate();
    }
}
```

**Step 2: Verify file compiles**

In Unity Editor, check Console for no errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs
git commit -m "feat: call UpdateCandidate in zero-gravity mode"
```

---

### Task 4: Create LockIndicator prefab component

**Files:**
- Create: `Assets/Scripts/Player/LockIndicator.cs`

**Step 1: Create LockIndicator.cs**

```csharp
using UnityEngine;
using TMPro;
using DG.Tweening;

public class LockIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _ring;
    [SerializeField] private TMP_Text _distanceText;
    [SerializeField] private TMP_Text _velocityText;
    [SerializeField] private TMP_Text _nameText;

    [Header("Settings")]
    [SerializeField] private float _padding = 1.2f;
    [SerializeField] private Color _lockedColor = Color.cyan;
    [SerializeField] private Color _candidateColor = new Color(1f, 1f, 1f, 0.5f);

    private CanvasGroup _canvasGroup;
    private Collider _target;
    private Camera _camera;
    private bool _isLocked;
    private Sequence _pulseSequence;
    private Vector3 _lastPosition;

    public Collider Target => _target;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(Camera camera)
    {
        _camera = camera;
        HideImmediate();
    }

    public void Show(Collider target, bool isLocked)
    {
        _target = target;
        _isLocked = isLocked;
        _lastPosition = target.transform.position;
        
        UpdateVisuals();
        AnimateIn();
    }

    public void Hide()
    {
        _target = null;
        AnimateOut();
    }

    public void HideImmediate()
    {
        _target = null;
        _canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
        _pulseSequence?.Kill();
    }

    private void Update()
    {
        if (_target == null) return;

        UpdatePosition();
        UpdateSize();
        
        if (_isLocked)
        {
            UpdateTextInfo();
        }
    }

    private void UpdatePosition()
    {
        Vector3 screenPos = _camera.WorldToScreenPoint(_target.bounds.center);

        if (screenPos.z < 0)
        {
            _canvasGroup.alpha = 0f;
            return;
        }

        _canvasGroup.alpha = _isLocked ? 1f : 0.5f;
        transform.position = screenPos;
    }

    private void UpdateSize()
    {
        Bounds bounds = _target.bounds;
        Vector3 min = _camera.WorldToScreenPoint(bounds.min);
        Vector3 max = _camera.WorldToScreenPoint(bounds.max);
        
        float size = Mathf.Max(max.x - min.x, max.y - min.y) * _padding;
        size = Mathf.Max(size, 50f);
        
        _ring.sizeDelta = new Vector2(size, size);
    }

    private void UpdateTextInfo()
    {
        if (_distanceText != null)
        {
            float distance = Vector3.Distance(_camera.transform.position, _target.transform.position);
            _distanceText.text = $"{distance:F1}m";
        }

        if (_velocityText != null)
        {
            Vector3 velocity = (_target.transform.position - _lastPosition) / Time.deltaTime;
            float speed = velocity.magnitude;
            _velocityText.text = speed > 0.1f ? $"{speed:F1} m/s" : "";
            _lastPosition = _target.transform.position;
        }

        if (_nameText != null)
        {
            _nameText.text = _target.name;
        }
    }

    private void UpdateVisuals()
    {
        Color color = _isLocked ? _lockedColor : _candidateColor;
        
        if (_ring != null)
        {
            var ringImage = _ring.GetComponent<UnityEngine.UI.Image>();
            if (ringImage != null)
                ringImage.color = color;
        }

        bool showText = _isLocked;
        if (_distanceText != null) _distanceText.gameObject.SetActive(showText);
        if (_velocityText != null) _velocityText.gameObject.SetActive(showText);
        if (_nameText != null) _nameText.gameObject.SetActive(showText);
    }

    private void AnimateIn()
    {
        _pulseSequence?.Kill();
        
        transform.localScale = Vector3.zero;
        _canvasGroup.alpha = _isLocked ? 1f : 0.5f;

        transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutQuad)
            .OnComplete(() => transform.DOScale(1f, 0.15f).SetEase(Ease.InOutQuad));

        StartPulse();
    }

    private void AnimateOut()
    {
        _pulseSequence?.Kill();
        
        transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
        _canvasGroup.DOFade(0f, 0.2f);
    }

    private void StartPulse()
    {
        _pulseSequence?.Kill();
        
        float targetAlpha = _isLocked ? 0.7f : 0.3f;
        float originalAlpha = _isLocked ? 1f : 0.5f;
        
        _pulseSequence = DOTween.Sequence();
        _pulseSequence.Append(_canvasGroup.DOFade(targetAlpha, 0.75f));
        _pulseSequence.Append(_canvasGroup.DOFade(originalAlpha, 0.75f));
        _pulseSequence.SetLoops(-1);
    }
}
```

**Step 2: Verify file compiles**

In Unity Editor, check Console for no errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/Player/LockIndicator.cs
git commit -m "feat: add LockIndicator component for individual indicator logic"
```

---

### Task 5: Create LockIndicatorUI controller

**Files:**
- Create: `Assets/Scripts/Player/LockIndicatorUI.cs`

**Step 1: Create LockIndicatorUI.cs**

```csharp
using UnityEngine;

public class LockIndicatorUI : MonoBehaviour
{
    [Header("Indicator Prefabs")]
    [SerializeField] private LockIndicator _candidateIndicator;
    [SerializeField] private LockIndicator _lockedIndicator;

    [Header("References")]
    [SerializeField] private Camera _camera;

    private void OnEnable()
    {
        LockEvents.OnTargetLocked += HandleTargetLocked;
        LockEvents.OnTargetReleased += HandleTargetReleased;
        LockEvents.OnCandidateChanged += HandleCandidateChanged;
    }

    private void OnDisable()
    {
        LockEvents.OnTargetLocked -= HandleTargetLocked;
        LockEvents.OnTargetReleased -= HandleTargetReleased;
        LockEvents.OnCandidateChanged -= HandleCandidateChanged;
    }

    private void Start()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_candidateIndicator != null)
            _candidateIndicator.Initialize(_camera);

        if (_lockedIndicator != null)
            _lockedIndicator.Initialize(_camera);

        HideAll();
    }

    private void HandleTargetLocked(Collider target)
    {
        HideCandidate();
        
        if (_lockedIndicator != null && target != null)
            _lockedIndicator.Show(target, true);
    }

    private void HandleTargetReleased()
    {
        HideLocked();
    }

    private void HandleCandidateChanged(Collider candidate)
    {
        if (candidate != null)
        {
            if (_candidateIndicator != null)
                _candidateIndicator.Show(candidate, false);
        }
        else
        {
            HideCandidate();
        }
    }

    private void HideCandidate()
    {
        if (_candidateIndicator != null)
            _candidateIndicator.Hide();
    }

    private void HideLocked()
    {
        if (_lockedIndicator != null)
            _lockedIndicator.Hide();
    }

    private void HideAll()
    {
        HideCandidate();
        HideLocked();
    }
}
```

**Step 2: Verify file compiles**

In Unity Editor, check Console for no errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/Player/LockIndicatorUI.cs
git commit -m "feat: add LockIndicatorUI canvas controller"
```

---

### Task 6: Create LockIndicator prefab

**Files:**
- Create: `Assets/Prefabs/UI/LockIndicator.prefab` (via Unity Editor)

**Step 1: Create UI Canvas (if not exists)**

1. In Unity: Right-click Hierarchy → UI → Canvas
2. Set Render Mode to "Screen Space - Overlay"
3. Add Canvas Scaler component with "Scale With Screen Size" (Reference: 1920x1080)

**Step 2: Create LockIndicator prefab structure**

1. Right-click Canvas → UI → Empty, name it "LockIndicator"
2. Add `LockIndicator` component to it
3. Add CanvasGroup component

Inside LockIndicator, create:

**Ring:**
- Right-click LockIndicator → UI → Image
- Name: "Ring"
- Source Image: Knob (or create circle sprite)
- Set Color: Cyan (for locked state)
- RectTransform: Center anchor, size 100x100

**DistanceText:**
- Right-click LockIndicator → UI → Text - TextMeshPro
- Name: "DistanceText"
- Text: "0.0m"
- Alignment: Center
- Position: Below ring (Y: -60)

**VelocityText:**
- Duplicate DistanceText
- Name: "VelocityText"
- Position: Y: -90

**NameText:**
- Duplicate DistanceText
- Name: "NameText"
- Font size smaller
- Position: Y: -120

**Step 3: Wire up references**

Select LockIndicator root:
- Drag Ring → Ring field
- Drag DistanceText → DistanceText field
- Drag VelocityText → VelocityText field
- Drag NameText → NameText field

**Step 4: Save as prefab**

Drag LockIndicator from Canvas to `Assets/Prefabs/UI/` folder.

**Step 5: Duplicate for candidate/locked**

Create two instances on the Canvas:
- `CandidateIndicator` (will be dimmer, no text)
- `LockedIndicator` (full info)

**Step 6: Add LockIndicatorUI to Canvas**

1. Select Canvas
2. Add `LockIndicatorUI` component
3. Drag CandidateIndicator → Candidate Indicator field
4. Drag LockedIndicator → Locked Indicator field
5. Drag Main Camera → Camera field

**Step 7: Test in Play Mode**

1. Enter Play Mode
2. Toggle gravity (press ToggleGravity key)
3. Aim at object with collider → should see dim candidate ring
4. Press Lock key → should see full locked indicator with info

**Step 8: Commit**

```bash
git add Assets/Prefabs/UI/
git commit -m "feat: add LockIndicator prefab and scene setup"
```

---

### Task 7: Final verification

**Step 1: Play test all scenarios**

- [ ] Candidate indicator appears when aiming at lockable object in zero-G
- [ ] Candidate indicator hides when looking away
- [ ] Locked indicator appears with full info when locking
- [ ] Distance updates in real-time
- [ ] Velocity displays when target moves
- [ ] Name displays correctly
- [ ] Both indicators hide when returning to grounded mode
- [ ] Indicators scale correctly for different object sizes
- [ ] Indicators hide when target goes off-screen/behind camera

**Step 2: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "fix: lock indicator polish and fixes"
```

---

## Summary

**Files created:**
- `Assets/Scripts/Player/LockEvents.cs`
- `Assets/Scripts/Player/LockIndicator.cs`
- `Assets/Scripts/Player/LockIndicatorUI.cs`
- `Assets/Prefabs/UI/LockIndicator.prefab`

**Files modified:**
- `Assets/Scripts/Player/TargetLock.cs`
- `Assets/Scripts/Player/PlayerController.cs`

**Dependencies:**
- DOTween (already imported)
- TextMeshPro (Unity built-in)
