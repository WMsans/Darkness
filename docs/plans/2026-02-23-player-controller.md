# Player Controller Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement an Outer Wilds-like player controller with gravity and zero-gravity modes using Unity's new Input System.

**Architecture:** State pattern with PlayerController delegating to IGravityState implementations (GroundedState, ZeroGravityState). TargetLock component handles lock-on logic. First-person camera controller.

**Tech Stack:** Unity 6, New Input System, Rigidbody physics

---

### Task 1: Project Setup & Folder Structure

**Files:**
- Create: `Assets/Scripts/Player/`
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Create: `Assets/Scripts/Player/PlayerInput.cs`
- Create: `Assets/Scripts/Player/IGravityState.cs`
- Create: `Assets/Scripts/Player/GroundedState.cs`
- Create: `Assets/Scripts/Player/ZeroGravityState.cs`
- Create: `Assets/Scripts/Player/TargetLock.cs`
- Create: `Assets/Scripts/Camera/`
- Create: `Assets/Scripts/Camera/PlayerCamera.cs`
- Modify: `Assets/InputSystem_Actions.inputactions` (add ToggleGravity, LockTarget actions via Unity GUI)

**Step 1: Create folder structure**

In Unity Project window, create folders:
- `Assets/Scripts/Player`
- `Assets/Scripts/Camera`

**Step 2: Add input actions**

In Unity Editor:
1. Open `Assets/InputSystem_Actions.inputactions`
2. In Player action map, add:
   - `ToggleGravity` (Button type)
   - `LockTarget` (Button type)
3. Add bindings via GUI (recommended: G for ToggleGravity, F for LockTarget)
4. Click "Generate C# Class" to regenerate InputSystem_Actions.cs

**Step 3: Commit**

```bash
git add Assets/Scripts/ Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.cs
git commit -m "chore: setup player controller folder structure and input actions"
```

---

### Task 2: PlayerInput Reader Class

**Files:**
- Create: `Assets/Scripts/Player/PlayerInput.cs`

**Step 1: Write PlayerInput class**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput
{
    private InputSystem_Actions _actions;

    public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => _actions.Player.Look.ReadValue<Vector2>();
    public bool JumpPressed => _actions.Player.Jump.WasPressedThisFrame();
    public bool SprintHeld => _actions.Player.Sprint.IsPressed();
    public bool SneakHeld => _actions.Player.Crouch.IsPressed();
    public bool ToggleGravityPressed => _actions.Player.ToggleGravity.WasPressedThisFrame();
    public bool LockTargetPressed => _actions.Player.LockTarget.WasPressedThisFrame();

    public PlayerInput()
    {
        _actions = new InputSystem_Actions();
        _actions.Enable();
    }

    public void Dispose()
    {
        _actions?.Dispose();
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/PlayerInput.cs
git commit -m "feat: add PlayerInput reader class"
```

---

### Task 3: IGravityState Interface

**Files:**
- Create: `Assets/Scripts/Player/IGravityState.cs`

**Step 1: Write interface**

```csharp
using UnityEngine;

public interface IGravityState
{
    void Enter(Rigidbody rb);
    void Exit(Rigidbody rb);
    void FixedUpdate(Rigidbody rb, PlayerInput input);
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/IGravityState.cs
git commit -m "feat: add IGravityState interface"
```

---

### Task 4: GroundedState Implementation

**Files:**
- Create: `Assets/Scripts/Player/GroundedState.cs`

**Step 1: Write GroundedState**

```csharp
using UnityEngine;

public class GroundedState : IGravityState
{
    private readonly Transform _cameraTransform;
    private readonly float _moveForce;
    private readonly float _jumpForce;
    private readonly float _sprintMultiplier;
    private readonly float _sneakMultiplier;
    private readonly float _groundCheckDistance;
    private readonly float _groundCheckRadius;
    private readonly LayerMask _groundLayer;

    public GroundedState(
        Transform cameraTransform,
        float moveForce,
        float jumpForce,
        float sprintMultiplier,
        float sneakMultiplier,
        float groundCheckDistance,
        float groundCheckRadius,
        LayerMask groundLayer)
    {
        _cameraTransform = cameraTransform;
        _moveForce = moveForce;
        _jumpForce = jumpForce;
        _sprintMultiplier = sprintMultiplier;
        _sneakMultiplier = sneakMultiplier;
        _groundCheckDistance = groundCheckDistance;
        _groundCheckRadius = groundCheckRadius;
        _groundLayer = groundLayer;
    }

    public void Enter(Rigidbody rb)
    {
        rb.useGravity = true;
        rb.drag = 1f;
    }

    public void Exit(Rigidbody rb)
    {
        rb.useGravity = false;
        rb.drag = 0f;
    }

    public void FixedUpdate(Rigidbody rb, PlayerInput input)
    {
        ApplyMovement(rb, input);
        ApplyJump(rb, input);
    }

    private void ApplyMovement(Rigidbody rb, PlayerInput input)
    {
        Vector2 moveInput = input.Move;
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
        Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

        float force = _moveForce;
        if (input.SprintHeld) force *= _sprintMultiplier;
        if (input.SneakHeld) force *= _sneakMultiplier;

        rb.AddForce(moveDir * force, ForceMode.Force);
    }

    private void ApplyJump(Rigidbody rb, PlayerInput input)
    {
        if (!input.JumpPressed) return;
        if (!IsGrounded(rb)) return;

        rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded(Rigidbody rb)
    {
        Vector3 origin = rb.position + Vector3.up * 0.1f;
        return Physics.SphereCast(origin, _groundCheckRadius, Vector3.down, out _, _groundCheckDistance, _groundLayer);
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/GroundedState.cs
git commit -m "feat: add GroundedState implementation"
```

---

### Task 5: TargetLock Component

**Files:**
- Create: `Assets/Scripts/Player/TargetLock.cs`

**Step 1: Write TargetLock**

```csharp
using UnityEngine;

public class TargetLock
{
    private readonly Transform _cameraTransform;
    private readonly float _lockRange;
    private readonly float _lockRadius;
    private readonly float _releaseRange;

    private Collider _lockedTarget;

    public bool IsLocked => _lockedTarget != null;
    public Collider Target => _lockedTarget;
    public Vector3 TargetVelocity => GetTargetVelocity();

    public TargetLock(Transform cameraTransform, float lockRange, float lockRadius, float releaseRange)
    {
        _cameraTransform = cameraTransform;
        _lockRange = lockRange;
        _lockRadius = lockRadius;
        _releaseRange = releaseRange;
    }

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
        }
    }

    public void Release()
    {
        _lockedTarget = null;
    }

    public void CheckRelease(Transform playerTransform)
    {
        if (_lockedTarget == null) return;

        if (_lockedTarget == null || !_lockedTarget.gameObject.activeInHierarchy)
        {
            Release();
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, _lockedTarget.transform.position);
        if (distance > _releaseRange)
        {
            Release();
        }
    }

    private Vector3 GetTargetVelocity()
    {
        if (_lockedTarget == null) return Vector3.zero;

        Rigidbody targetRb = _lockedTarget.attachedRigidbody;
        if (targetRb != null)
        {
            return targetRb.linearVelocity;
        }

        return Vector3.zero;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/TargetLock.cs
git commit -m "feat: add TargetLock component"
```

---

### Task 6: ZeroGravityState Implementation

**Files:**
- Create: `Assets/Scripts/Player/ZeroGravityState.cs`

**Step 1: Write ZeroGravityState**

```csharp
using UnityEngine;

public class ZeroGravityState : IGravityState
{
    private readonly Transform _cameraTransform;
    private readonly TargetLock _targetLock;
    private readonly float _accelerationForce;
    private readonly float _verticalForce;
    private readonly float _matchVelocityForce;
    private readonly float _offsetSpeed;

    public ZeroGravityState(
        Transform cameraTransform,
        TargetLock targetLock,
        float accelerationForce,
        float verticalForce,
        float matchVelocityForce,
        float offsetSpeed)
    {
        _cameraTransform = cameraTransform;
        _targetLock = targetLock;
        _accelerationForce = accelerationForce;
        _verticalForce = verticalForce;
        _matchVelocityForce = matchVelocityForce;
        _offsetSpeed = offsetSpeed;
    }

    public void Enter(Rigidbody rb)
    {
        rb.useGravity = false;
        rb.drag = 0.1f;
    }

    public void Exit(Rigidbody rb)
    {
        rb.useGravity = true;
        rb.drag = 1f;
        _targetLock.Release();
    }

    public void FixedUpdate(Rigidbody rb, PlayerInput input)
    {
        if (_targetLock.IsLocked)
        {
            ApplyLockedMovement(rb, input);
        }
        else
        {
            ApplyFreeMovement(rb, input);
        }
    }

    private void ApplyFreeMovement(Rigidbody rb, PlayerInput input)
    {
        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;
        Vector3 up = _cameraTransform.up;

        Vector2 moveInput = input.Move;
        Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.AddForce(moveDir * _accelerationForce, ForceMode.Force);
        }

        if (input.JumpPressed)
        {
            rb.AddForce(up * _verticalForce, ForceMode.Force);
        }

        if (input.SneakHeld)
        {
            rb.AddForce(-up * _verticalForce, ForceMode.Force);
        }
    }

    private void ApplyLockedMovement(Rigidbody rb, PlayerInput input)
    {
        Vector3 targetVelocity = _targetLock.TargetVelocity;
        Vector3 relativeVelocity = rb.linearVelocity - targetVelocity;

        Vector3 desiredRelativeVelocity = Vector3.zero;

        Vector2 moveInput = input.Move;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;
            desiredRelativeVelocity = (forward * moveInput.y + right * moveInput.x).normalized * _offsetSpeed;
        }

        if (input.JumpPressed)
        {
            desiredRelativeVelocity += _cameraTransform.up * _offsetSpeed;
        }

        if (input.SneakHeld)
        {
            desiredRelativeVelocity -= _cameraTransform.up * _offsetSpeed;
        }

        Vector3 velocityDiff = desiredRelativeVelocity - relativeVelocity;
        rb.AddForce(velocityDiff.normalized * _matchVelocityForce, ForceMode.Force);
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/ZeroGravityState.cs
git commit -m "feat: add ZeroGravityState implementation"
```

---

### Task 7: PlayerCamera Controller

**Files:**
- Create: `Assets/Scripts/Camera/PlayerCamera.cs`

**Step 1: Write PlayerCamera**

```csharp
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 2f;
    [SerializeField] private float _groundedVerticalClamp = 80f;

    private Transform _playerBody;
    private float _xRotation;
    private bool _isZeroGravity;

    public void Initialize(Transform playerBody)
    {
        _playerBody = playerBody;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetZeroGravityMode(bool isZeroGravity)
    {
        _isZeroGravity = isZeroGravity;
    }

    public void UpdateLook(Vector2 lookInput)
    {
        if (lookInput.sqrMagnitude < 0.01f) return;

        float mouseX = lookInput.x * _sensitivity;
        float mouseY = lookInput.y * _sensitivity;

        _xRotation -= mouseY;
        _xRotation = _isZeroGravity
            ? Mathf.Repeat(_xRotation + 180f, 360f) - 180f
            : Mathf.Clamp(_xRotation, -_groundedVerticalClamp, _groundedVerticalClamp);

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        _playerBody.Rotate(Vector3.up * mouseX);
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Camera/PlayerCamera.cs
git commit -m "feat: add PlayerCamera controller"
```

---

### Task 8: PlayerController Main Component

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`

**Step 1: Write PlayerController**

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveForce = 10f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _sprintMultiplier = 1.5f;
    [SerializeField] private float _sneakMultiplier = 0.5f;

    [Header("Zero-G Settings")]
    [SerializeField] private float _accelerationForce = 5f;
    [SerializeField] private float _verticalForce = 5f;
    [SerializeField] private float _matchVelocityForce = 5f;
    [SerializeField] private float _offsetSpeed = 2f;
    [SerializeField] private float _lockRange = 100f;
    [SerializeField] private float _lockRadius = 2f;
    [SerializeField] private float _releaseRange = 200f;

    [Header("Ground Check")]
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private float _groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private PlayerCamera _playerCamera;

    private Rigidbody _rb;
    private PlayerInput _input;
    private IGravityState _currentState;
    private GroundedState _groundedState;
    private ZeroGravityState _zeroGravityState;
    private TargetLock _targetLock;
    private bool _isGravityEnabled = true;
    private float _toggleCooldown;
    private const float TOGGLE_COOLDOWN_TIME = 0.2f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = new PlayerInput();

        _targetLock = new TargetLock(_cameraTransform, _lockRange, _lockRadius, _releaseRange);

        _groundedState = new GroundedState(
            _cameraTransform,
            _moveForce,
            _jumpForce,
            _sprintMultiplier,
            _sneakMultiplier,
            _groundCheckDistance,
            _groundCheckRadius,
            _groundLayer);

        _zeroGravityState = new ZeroGravityState(
            _cameraTransform,
            _targetLock,
            _accelerationForce,
            _verticalForce,
            _matchVelocityForce,
            _offsetSpeed);

        _currentState = _groundedState;
        _currentState.Enter(_rb);

        if (_playerCamera != null)
        {
            _playerCamera.Initialize(transform);
        }
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void Update()
    {
        _toggleCooldown -= Time.deltaTime;

        HandleToggleGravity();
        HandleLockTarget();
        UpdateCamera();

        if (!_isGravityEnabled)
        {
            _targetLock.CheckRelease(transform);
        }
    }

    private void FixedUpdate()
    {
        _currentState.FixedUpdate(_rb, _input);
    }

    private void HandleToggleGravity()
    {
        if (!_input.ToggleGravityPressed || _toggleCooldown > 0) return;

        _toggleCooldown = TOGGLE_COOLDOWN_TIME;
        _currentState.Exit(_rb);

        _isGravityEnabled = !_isGravityEnabled;
        _currentState = _isGravityEnabled ? (IGravityState)_groundedState : _zeroGravityState;

        _currentState.Enter(_rb);

        if (_playerCamera != null)
        {
            _playerCamera.SetZeroGravityMode(!_isGravityEnabled);
        }
    }

    private void HandleLockTarget()
    {
        if (_isGravityEnabled) return;

        if (_input.LockTargetPressed)
        {
            if (_targetLock.IsLocked)
            {
                _targetLock.Release();
            }
            else
            {
                _targetLock.TryLock();
            }
        }
    }

    private void UpdateCamera()
    {
        if (_playerCamera != null)
        {
            _playerCamera.UpdateLook(_input.Look);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs
git commit -m "feat: add PlayerController main component"
```

---

### Task 9: Scene Setup

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

**Step 1: Create Player GameObject**

In Unity Editor:
1. Create a Capsule GameObject named "Player" at position (0, 1, 0)
2. Add Rigidbody component (mass: 1, constraints: freeze rotation X/Z)
3. Add PlayerController component
4. Create child Camera GameObject, add PlayerCamera component
5. Assign camera references in PlayerController

**Step 2: Create Ground**

1. Create a Plane or Cube as ground
2. Assign to "Ground" layer
3. Update PlayerController's Ground Layer field

**Step 3: Create Test Objects for Lock**

1. Create several Cubes scattered in scene
2. Optional: Add Rigidbody to some for moving targets

**Step 4: Play Test**

1. Enter Play mode
2. Test WASD movement on ground
3. Test jump (Space)
4. Test sprint (Shift)
5. Test sneak (C)
6. Press G to toggle zero-gravity
7. Test 6DOF movement
8. Press F to lock onto objects
9. Test locked movement matching

**Step 4: Commit**

```bash
git add Assets/Scenes/SampleScene.unity
git commit -m "feat: setup player in scene with ground and test objects"
```

---

### Task 10: Final Testing & Cleanup

**Step 1: Full manual test**

Run through all test cases:
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

**Step 2: Fix any issues found**

If bugs found, create fix commits.

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat: complete player controller implementation"
```
