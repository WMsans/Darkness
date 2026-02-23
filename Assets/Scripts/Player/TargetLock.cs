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
