using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector3 frameVelocity;
    public float baseSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public int maxJumps = 2;
    public int jumpCount = 0;
    public float jumpGraceDuration = 0.1f;

    [Tooltip("How fast the external force fades out (higher = faster, 5 feels snappier, 2 feels floatier).")]
    public float externalForceDecayRate = 5f;
    public bool isGrounded = true;
    public bool overrideSpeed = false;
    public bool overrideJump = false;
    private bool justJumped = false;
    private float jumpGraceTimer = 0f;

    [Header("Ground Check Settings")]
    public Transform groundCheckOrigin;
    public float groundCheckRadius = 0.25f;

    [Tooltip("Make sure your distance is larger than the CharacterController's SkinWidth")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer = 3;

    [Header("References")]
    public Transform cameraTransform;

    private InputManager input;
    private MultiplierSystem multiplier;
    private WallRun wallRun;
    private CharacterController controller;
    private Vector3 moveInput;
    private Vector3 lastPosition;
    private Vector3 pendingForce = Vector3.zero;
    private Vector3 externalForce = Vector3.zero;
    private float originalExternalForceDecayRate;
    private float verticalVelocity;
    private bool isSprinting;

    private void Start()
    {
        input = InputManager.instance;
        multiplier = GetComponent<MultiplierSystem>();
        wallRun = GetComponent<WallRun>();
        controller = GetComponent<CharacterController>();
        originalExternalForceDecayRate = externalForceDecayRate;
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
        UpdateFrameVelocity();
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
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        // Prevents floating frames when falling at a fast speed
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        // Prevents ground check from not counting first jump
        if (justJumped)
        {
            jumpGraceTimer -= Time.deltaTime;
            if (jumpGraceTimer <= 0f)
                justJumped = false;

            return; // Skip resetting jump counter while in grace period
        }

        // Resets jump count if we've truly landed
        if (isGrounded && jumpCount > 0)
            jumpCount = 0;
    }

    private void Move()
    {
        if (overrideSpeed)
            return;

        float speed = multiplier ? baseSpeed * multiplier.GetSpeedMultiplier() : baseSpeed;
        moveInput = input.Move;
        Vector3 move = cameraTransform.forward * moveInput.y + cameraTransform.right * moveInput.x;
        move.y = 0f;
        controller.Move(move * speed * Time.deltaTime);
    }

    private void Jump()
    {
        if (wallRun && wallRun.IsNearWall && InputManager.instance.JumpPressed && !wallRun.isWallRunning)
        {
            wallRun.StartWallRunFromController();
            return;
        }

        if (overrideJump)
            return;

        if (input.JumpPressed && jumpCount < maxJumps)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
            justJumped = true;
            jumpGraceTimer = jumpGraceDuration;
        }
    }

    private void ApplyGravity()
    {
        if (wallRun && wallRun.isWallRunning)
            return;

        verticalVelocity += gravity * Time.deltaTime;

        // Add any pending forces 
        if (pendingForce.sqrMagnitude > 0.01f)
        {
            externalForce += pendingForce;
            pendingForce = Vector3.zero;
        }

        // Apply gravity and external forces
        Vector3 totalVelocity = externalForce;
        totalVelocity.y += verticalVelocity;
        controller.Move(totalVelocity * Time.deltaTime);

        // Gradually decay external force
        externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecayRate * Time.deltaTime);
    }

    private void UpdateFrameVelocity()
    {
        Vector3 displacement = transform.position - lastPosition;
        frameVelocity = displacement / Time.deltaTime;
        lastPosition = transform.position;
    }

    public void SetExternalDecayRate(float amount)
    {
        externalForceDecayRate = amount;
    }

    public void ResetExternalDecayRate()
    {
        externalForceDecayRate = originalExternalForceDecayRate;
    }

    /// <summary> Applies an external force impulse to the player, used for rocket jumping or explosions. </summary>
    public void ApplyExternalForce(Vector3 force)
    {
        externalForce += force;
    }

    /// <summary> Returns baseSpeed * multiplier.GetSpeedMultiplier(). </summary>
    public float Speed { get { return multiplier ? baseSpeed * multiplier.GetSpeedMultiplier() : baseSpeed; } }
    public float CurrentSpeedMagnitude => frameVelocity.magnitude;
    public Vector3 CurrentVelocity => frameVelocity;
    public Vector3 HorizontalVelocity => new Vector3(frameVelocity.x, 0f, frameVelocity.z);
    public float VerticalVelocity { get { return verticalVelocity; } set { verticalVelocity = value; } }
}