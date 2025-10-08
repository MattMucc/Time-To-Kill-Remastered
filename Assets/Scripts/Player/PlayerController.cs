using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public int maxJumps = 2;
    public int jumpCount = 0;

    [Header("References")]
    public Transform cameraTransform;

    private InputManager input;
    private CharacterController controller;
    private Vector3 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting;

    private void Start()
    {
        input = InputManager.instance;
        controller = GetComponent<CharacterController>();
        SpawnPlayer();
    }

    private void Update()
    {
        if (!input)
            return;

        IsGrounded();
        GroundedGravity();
        Move();
        Jump();
        ApplyGravity();
    }

    private void SpawnPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UIManager.instance.OpenHUD();
    }

    private bool IsGrounded()
    {
        isGrounded = controller.isGrounded;
        return isGrounded;
    }

    private void GroundedGravity()
    {
        if (IsGrounded() && velocity.y < 0)
            velocity.y = -2f;

        if (IsGrounded())
            jumpCount = 0;
    }

    private void Move()
    {
        moveInput = input.Move;
        Vector3 move = cameraTransform.forward * moveInput.y + cameraTransform.right * moveInput.x;
        move.y = 0f;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void Jump()
    {
        if (input.JumpPressed && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}