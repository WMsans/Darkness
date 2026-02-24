using UnityEngine;

public class CameraHover : MonoBehaviour
{
    [Header("晃动幅度")]
    public float rangeX = 1.0f;
    public float rangeY = 1.0f;

    [Header("平滑速度")]
    public float smoothSpeed = 5.0f;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        float mouseX = Input.mousePosition.x / Screen.width;
        float mouseY = Input.mousePosition.y / Screen.height;
        
        float offsetX = (mouseX - 0.5f) * 2f;
        float offsetY = (mouseY - 0.5f) * 2f;
        
        Quaternion targetRotation = initialRotation * Quaternion.Euler(-offsetY * rangeY, offsetX * rangeX, 0);
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}