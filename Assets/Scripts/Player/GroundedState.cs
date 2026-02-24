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
        rb.linearDamping = 1f;
    }

    public void Exit(Rigidbody rb)
    {
        rb.useGravity = false;
        rb.linearDamping = 0f;
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
