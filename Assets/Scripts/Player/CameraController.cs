using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Tooltip("Degrees per pixel of pointer movement.")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [Tooltip("Degrees per second at full stick deflection.")]
    [SerializeField] private float controllerSensitivity = 180f;

    [Header("Clamp Settings")]
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("References")]
    [SerializeField] private InputReader input;
    [SerializeField] private Transform playerRoot;

    private float pitch;
    private float yaw;
    private bool cursorCaptured;

    private void Awake()
    {
        if (!input) input = GetComponentInParent<InputReader>();
        if (!playerRoot) playerRoot = transform.root;
    }

    private void OnEnable()
    {
        if (playerRoot) yaw = playerRoot.localEulerAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.localEulerAngles.x), minPitch, maxPitch);
    }

    private void Update()
    {
        bool active = input && playerRoot && input.GameplayActive;
        if (cursorCaptured != active)
        {
            cursorCaptured = active;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
        if (!active) return;

        float sensitivity = input.LookUsesGamepad
            ? controllerSensitivity * Time.deltaTime
            : mouseSensitivity;
        yaw += input.Look.x * sensitivity;
        pitch = Mathf.Clamp(pitch - input.Look.y * sensitivity, minPitch, maxPitch);
        playerRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void OnDisable()
    {
        if (!cursorCaptured) return;
        cursorCaptured = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
