using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float mouseSensitivity = 100f;

    [Header("Clamp Settings")]
    [SerializeField] private float minPitch = -85;
    [SerializeField] private float maxPitch = 85;

    [Header("References")]
    [SerializeField] private InputReader input;
    [SerializeField] private Transform playerRoot;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Awake()
    {
        if (!input) input = GetComponent<InputReader>();
        if (!playerRoot) playerRoot = transform.root;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!input) return;
        if (!playerRoot) return;
        Look();
    }

    private void Look()
    {
        Vector2 lookInput = input.Look;
        yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // Clamp the pitch so the pitch GameObject can't flip over itself

        playerRoot.localRotation = Quaternion.Euler(0f, yaw, 0f); // Rotate player body for horizontal rotation
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f); // Rotate pitch GameObject for vertical rotation
    }
}