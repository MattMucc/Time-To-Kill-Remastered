using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float sensitivity = 200f;

    [Header("References")]
    public Transform playerTransform;
    public Transform cameraRoot;
    private CameraEffects cameraEffects;

    private InputManager input;
    private Vector2 lookInput;
    private float pitch;
    private float yaw;

    private void Start()
    {
        input = InputManager.instance;
        cameraRoot = transform.parent;
        cameraEffects = GetComponent<CameraEffects>();

        Vector3 camAngle = cameraRoot.localEulerAngles;
        pitch = (camAngle.x > 180f) ? camAngle.x - 360f : camAngle.x;

        Vector3 playerAngle = playerTransform.localEulerAngles;
        yaw = (playerAngle.y > 180f) ? playerAngle.y - 360f : playerAngle.y;
    }

    private void Update()
    {
        if (!input)
            return;

        lookInput = input.Look;
    }

    private void LateUpdate()
    {
        Look();
    }

    private void Look()
    {
        float fov = Camera.main ? Camera.main.fieldOfView : 60f;
        float fovScale = fov / 60f; // 60 = normal FOV baseline
        float mouseX = lookInput.x * sensitivity * fovScale * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * fovScale * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        float rPitch = cameraEffects ? cameraEffects.currentRecoilPitch : 0f;
        float rYaw = cameraEffects ? cameraEffects.currentRecoilYaw : 0f;

        cameraRoot.localRotation = Quaternion.Euler(pitch - rPitch, 0f, 0f);
        playerTransform.localRotation = Quaternion.Euler(0f, yaw + rYaw, 0f);
    }
}