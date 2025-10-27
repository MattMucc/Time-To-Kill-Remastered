using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float sensitivity = 200f;
    public float wallRunRotationLimit = 89f;

    [Header("References")]
    public Transform playerTransform;
    public Transform cameraRoot;
    private CameraEffects cameraEffects;

    private InputManager input;
    private WallRun wallRun;
    private Vector2 lookInput;
    private float pitch;
    private float yaw;
    private float roll; // Tilt
    private float wallRunYawAnchor;

    private void Start()
    {
        input = InputManager.instance;
        wallRun = playerTransform.GetComponent<WallRun>();
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
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        WallRunClamp();
        if (wallRun && wallRun.isWallRunning)
            roll = wallRun.CurrentTilt;
        else if (wallRun && !wallRun.isWallRunning)
            roll = Mathf.Lerp(roll, 0f, 6 * Time.deltaTime);

        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
        playerTransform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public void AddRecoil(float pitchRecoil, float yawRecoil)
    {
        pitch -= pitchRecoil;
        yaw += yawRecoil;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
    }

    private void WallRunClamp()
    {
        if (!wallRun || !wallRun.isWallRunning)
            return;

        float maxYawOffset = wallRunRotationLimit;
        if (wallRun.isWallRight)
            yaw = Mathf.Clamp(yaw, wallRunYawAnchor - maxYawOffset, wallRunYawAnchor);
        else
            yaw = Mathf.Clamp(yaw, wallRunYawAnchor, wallRunYawAnchor + maxYawOffset);
    }

    public void SetWallRunYawAnchor()
    {
        wallRunYawAnchor = yaw;
    }
}