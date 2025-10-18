using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public int maxJumps = 2;
    public int jumpCount = 0;

    [Tooltip("How fast the external force fades out (higher = faster, 5 feels snappier, 2 feels floatier).")]
    public float externalForceDecayRate = 5f;
    public bool isGrounded = true;
    public bool overrideSpeed = false;

    [Header("Ground Check Settings")]
    public Transform groundCheckOrigin;
    public float groundCheckRadius = 0.25f;

    [Tooltip("Make sure your distance is larger than the CharacterController's SkinWidth")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer = 3;

    [Header("References")]
    public Transform cameraTransform;

    private InputManager input;
    private CharacterController controller;
    private Vector3 moveInput;
    private Vector3 velocity;
    private Vector3 pendingForce = Vector3.zero;
    private Vector3 externalForce = Vector3.zero;
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

        GroundCheck();
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

    private void GroundCheck()
    {
        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
        isGrounded = Physics.CheckSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        // Prevents floating frames when falling at a fast speed
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (isGrounded && jumpCount > 0)
            jumpCount = 0;
    }

    private void Move()
    {
        if (overrideSpeed)
            return;

        float speed = moveSpeed * GetComponent<MultiplierSystem>().GetSpeedMultiplier();
        moveInput = input.Move;
        Vector3 move = cameraTransform.forward * moveInput.y + cameraTransform.right * moveInput.x;
        move.y = 0f;
        controller.Move(move * speed * Time.deltaTime);
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

        // Add any pending forces 
        if (pendingForce.sqrMagnitude > 0.01f)
        {
            externalForce += pendingForce;
            pendingForce = Vector3.zero;
        }

        // Apply gravity and external forces
        Vector3 totalVelocity = velocity + externalForce;
        controller.Move(totalVelocity * Time.deltaTime);

        // Gradually decay external force
        externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecayRate * Time.deltaTime);
    }

    /// <summary> Applies an external force impulse to the player, used for rocket jumping or explosions. </summary>
    public void ApplyExternalForce(Vector3 force)
    {
        externalForce += force;
    }
}