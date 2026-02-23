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
