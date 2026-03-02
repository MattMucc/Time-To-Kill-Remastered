using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float crouchSpeed = 4f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f; // In measurement of units
    [SerializeField] private LayerMask groundLayer = ~0;
    private bool isGrounded;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private InputReader input;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsule;

    private void Awake()
    {
        if (!input) input = GetComponent<InputReader>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!capsule) capsule = GetComponent<CapsuleCollider>();
    }

    private void Update()
    {
        if (!input) return;
        if (!rb) return;

        GroundCheck();
        Jump();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        Move();
    }

    private void GroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, (capsule.height / 2) + 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void ApplyGravity()
    {
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            Vector3 downwardForce = new Vector3(rb.linearVelocity.x, -2f, rb.linearVelocity.z);
            rb.linearVelocity = downwardForce;
        }
        else if (!isGrounded)
        {
            rb.AddForce(gravity * Vector3.up, ForceMode.Acceleration);
        }
    }

    private void Move()
    {
        Vector2 moveInput = input.Move;
        Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
    }

    private void Jump()
    {
        if (!isGrounded) return;
        if (input.JumpPressed)
        {
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight); // Using the formula velocity = sqrt(2 * |gravity| * height) to calculate the initial velocity needed to reach the target jump height under the influence of gravity
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            isGrounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (capsule)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 origin = transform.position;
            Gizmos.DrawLine(origin, origin + Vector3.down * ((capsule.height / 2) + 0.2f));
        }
    }
}