# Player Controller Design

Outer Wilds-like player controller with gravity and zero-gravity modes.

## Overview

First-person player controller using Unity's new Input System. Supports two distinct movement modes that the player can toggle between.

| Aspect | Choice |
|--------|--------|
| Camera | First-person |
| Physics | Rigidbody with forces |
| Gravity transition | Manual toggle (G key) |
| Gravity system | Single global direction |
| Architecture | State Pattern |

## Architecture

```
PlayerController (MonoBehaviour)
├── State: IGravityState (interface)
│   ├── GroundedState : IGravityState
│   └── ZeroGravityState : IGravityState
├── TargetLock (handles lock-on logic)
└── PlayerCamera (first-person camera)
```

**PlayerController** owns the Rigidbody, reads input, and delegates physics to the current state. States are switched when player presses G.

**IGravityState interface:**
```csharp
interface IGravityState {
    void Enter();
    void Exit();
    void FixedUpdate(Rigidbody rb, PlayerInput input);
}
```

**TargetLock** component handles finding lockable objects via spherecast/overlap, tracking current target, and providing relative velocity data.

## Grounded State (With Gravity)

Minecraft-like ground movement with jump and sneak.

**Movement:**
- WASD moves relative to camera Y-plane (forward/back/strafe)
- Applies force proportional to input magnitude
- Ground friction/drag limits speed naturally
- Sprint multiplies acceleration force

**Jump:**
- Ground check via spherecast at feet
- Jump applies upward impulse when grounded
- Single impulse (no variable jump height)

**Sneak (Crouch key):**
- Reduces movement speed by ~50%
- Optional: Slight camera height drop

**Ground Detection:**
- Spherecast downward from player center
- Layer mask excludes player layer
- Grace period (coyote time) of ~0.1s optional

**Orientation:**
- Player stays upright (identity rotation on Y only)
- Camera handles look rotation

## Zero-Gravity State

6DOF space movement with target locking.

**Movement (unlocked):**
- WASD accelerates camera-relative (forward/back/left/right)
- Jump (Space) accelerates along camera up
- Sneak (Shift/Crouch) accelerates along camera down
- No speed cap - velocity accumulates
- Small counter-thrust drag to prevent infinite drift (optional)

**Movement (locked onto target):**
- Movement accelerates until relative velocity to target is zero
- Relative velocity = player.velocity - target.velocity
- WASD offsets target relative velocity
- Effectively: player velocity approaches (target.velocity + input_direction × offset_speed)

**Lock System:**
- F key scans for colliders in front of camera (spherecast, ~100m range)
- Locks onto nearest valid collider
- F again releases lock
- Stores target's Rigidbody or Transform to track velocity
- If target destroyed/goes out of range, auto-release

**Orientation:**
- Player rotation free (camera determines facing)
- Rigidbody.rotation matches camera rotation

## Camera & Input

**First-Person Camera:**
- Camera is child of player GameObject
- Horizontal look (mouse X) rotates player body
- Vertical look (mouse Y) rotates camera only (clamped ~±80°)
- In zero-gravity: full 360° vertical rotation allowed (no clamp)
- Sensitivity exposed as serialized field

**Input Handling:**
- Use existing `InputSystem_Actions.inputactions`
- Add two new actions via Unity GUI: `ToggleGravity` (Button), `LockTarget` (Button)
- Generate C# class from input actions asset
- Read values in Update, apply physics in FixedUpdate

**Input Reader Pattern:**
```csharp
public class PlayerInput {
    public Vector2 Move { get; }
    public Vector2 Look { get; }
    public bool JumpPressed { get; }
    public bool SprintHeld { get; }
    public bool SneakHeld { get; }
    public bool ToggleGravityPressed { get; }
    public bool LockTargetPressed { get; }
}
```

## Edge Cases & Error Handling

**State Transitions:**
- Toggle debounced (no rapid switching)
- When entering zero-gravity: preserve current velocity
- When entering gravity: apply gentle reorientation force to upright

**Lock Target Edge Cases:**
- No target in range → do nothing on F press
- Target destroyed/null → auto-release lock, notify player (optional HUD feedback)
- Target out of range (>200m) → auto-release lock

**Ground Check Edge Cases:**
- No ground detected → state unchanged (player may have jumped off edge)
- If falling in gravity mode without ground, apply gravity but disable jump

**Physics Interactions:**
- Collisions with objects still work via Rigidbody
- External forces (explosions, impacts) affect player normally

**Performance:**
- Lock scan only on F press (not every frame)
- Ground check once per FixedUpdate

## Testing

**Manual Testing Checklist:**
- [ ] Grounded movement feels responsive (WASD)
- [ ] Jump works only when grounded
- [ ] Sprint increases speed
- [ ] Sneak slows movement
- [ ] Toggle G switches modes correctly
- [ ] Zero-G movement accelerates without cap
- [ ] Zero-G up/down (Space/Shift) work
- [ ] Lock targets nearby objects (F)
- [ ] Locked movement matches target velocity
- [ ] Camera look works in both modes
- [ ] State transitions preserve velocity appropriately

**Inspector Exposables:**
- Movement acceleration force
- Jump impulse force
- Sprint multiplier
- Sneak multiplier
- Look sensitivity
- Lock range
- Ground check distance/radius
