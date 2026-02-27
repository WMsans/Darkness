# Lock Indicator Design

## Overview

Implement a screen-space UI indicator for the target locking system, inspired by Outer Wilds. The indicator displays on locked objects in zero-gravity mode, showing a ring with distance, velocity, and object name.

## Requirements

- Screen-space canvas-based UI
- Ring + distance + velocity + name display
- Smooth scale + pulse animation (DOTween)
- Auto-size from collider bounds
- Show candidates (pre-lock) in addition to locked state

## Architecture

```
PlayerController
    └── TargetLock (existing)
            │
            ▼ emits events
    ┌───────────────────┐
    │  LockEvents (new)  │  ← Static events
    └───────────────────┘
            │
            ▼ subscribes
    ┌───────────────────┐
    │ LockIndicatorUI   │  ← Canvas controller
    └───────────────────┘
            │
            ▼ controls
    ┌───────────────────┐
    │ LockIndicator     │  ← Prefab: ring + texts
    └───────────────────┘
```

## Components

### LockEvents (static class)

Decouples TargetLock from UI.

```csharp
public static event Action<Collider> OnTargetLocked;
public static event Action OnTargetReleased;
public static event Action<Collider> OnCandidateChanged;  // null = no candidate
```

### TargetLock modifications

- Add event invocations when locking/releasing
- Add `CurrentCandidate` property (collider in crosshair that can be locked)
- Poll for candidate in Update when not locked

### LockIndicatorUI

MonoBehaviour on canvas that:
- Subscribes to LockEvents
- Manages two indicator instances (candidate + locked)
- Updates positions via `Camera.WorldToScreenPoint`
- Handles show/hide with DOTween animations

### LockIndicator (prefab)

```
LockIndicator (RectTransform)
├── Ring (Image - circular outline)
├── DistanceText (TMP - "45.2m")
├── VelocityText (TMP - "↑ 12.5 m/s")
└── NameText (TMP - object name)
```

## Visual Specs

### Candidate indicator (pre-lock)

- Ring: 50% opacity white/gray
- Text: Hidden
- Animation: Gentle pulse (scale 0.95 ↔ 1.05, 1.5s cycle)

### Locked indicator

- Ring: 100% opacity, accent color (cyan/green)
- Text: Visible with live updates
- Animation: Scale in on lock (0 → 1.2 → 1.0 in 0.3s), subtle pulse

### Auto-sizing

Calculate screen-space bounds from collider:

```csharp
Bounds bounds = collider.bounds;
Vector3 center = Camera.WorldToScreenPoint(bounds.center);
Vector3 min = Camera.WorldToScreenPoint(bounds.min);
Vector3 max = Camera.WorldToScreenPoint(bounds.max);
float size = Mathf.Max(max.x - min.x, max.y - min.y) * padding;
indicator.Ring.sizeDelta = new Vector2(size, size);
```

### Off-screen handling

Hide indicator if target is behind camera or off-screen.

## Files

### New files

```
Assets/Scripts/Player/
├── LockEvents.cs           ← Static event class
├── LockIndicatorUI.cs      ← Canvas controller
└── LockIndicator.cs        ← Individual indicator logic

Assets/Prefabs/UI/
└── LockIndicator.prefab    ← UI prefab
```

### Modified files

- `TargetLock.cs` - Add event invocations, candidate detection
- `PlayerController.cs` - Expose TargetLock for candidate updates

### Scene setup

1. Create Canvas (Screen Space - Overlay)
2. Add LockIndicatorUI component
3. Assign LockIndicator prefab instances

## Dependencies

- DOTween (already imported)
