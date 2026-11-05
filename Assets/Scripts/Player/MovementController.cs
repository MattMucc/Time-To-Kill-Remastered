using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(InputReader))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.01f)] private float momentumThreshold = 12f;
    [Tooltip("Acceleration and direction-change rate below the momentum threshold, in units per second squared.")]
    [SerializeField, Min(0f)] private float lowSpeedGroundAcceleration = 60f;
    [Tooltip("Horizontal slowdown when releasing input below the ground momentum threshold.")]
    [SerializeField, Min(0f)] private float lowSpeedGroundDeceleration = 80f;
    [Tooltip("Horizontal acceleration for parkour corrections during low-speed jumps.")]
    [SerializeField, Min(0f)] private float lowSpeedAirAcceleration = 30f;
    [Tooltip("Horizontal slowdown on input release during low-speed jumps.")]
    [SerializeField, Min(0f)] private float lowSpeedAirDeceleration = 25f;
    [SerializeField, Min(0f)] private float groundSteering = 20f;
    [SerializeField, Min(0f)] private float airSteering = 3f;
    [SerializeField, Min(0f)] private float coastDeceleration = 3f;
    [SerializeField, Min(0f)] private float brakeDeceleration = 12f;
    private Quaternion previousHeading;
    private Quaternion airHeading;
    private bool wasGrounded;
    private bool momentumJump;

    [Header("Jump Settings")]
    [SerializeField, Min(0f)] private float jumpHeight = 2f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;
    private readonly JumpGrace jumpGrace = new JumpGrace();
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField, Min(0f)] private float groundCheckDistance = 0.1f;
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 60f;
    private bool isGrounded;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];

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
        if (!input || !rb || !capsule || capsule.direction != 1)
        {
            Debug.LogError("Movement requires input, a Rigidbody and a vertical capsule.", this);
            enabled = false;
            return;
        }
        // This controller owns gravity; do not apply Unity gravity a second time.
        rb.useGravity = false;
        previousHeading = airHeading = Heading();
    }

    private void OnEnable()
    {
        jumpGrace.Reset();
        if (input) input.OnInputsCleared += ClearJumpGrace;
    }

    private void OnDisable()
    {
        if (input) input.OnInputsCleared -= ClearJumpGrace;
        ClearJumpGrace();
        wasGrounded = false;
    }

    private void ClearJumpGrace()
    {
        jumpGrace.Reset();
    }

    private void FixedUpdate()
    {
        GroundCheck();
        Quaternion heading = Heading();
        if (wasGrounded && !isGrounded) BeginAirborne(previousHeading);
        bool jump = jumpGrace.Step(Time.fixedTimeAsDouble, isGrounded,
            input.ConsumeJumpPressed(), input.GameplayActive, coyoteTime, jumpBufferTime);
        if (input.GameplayActive) Move(heading);
        previousHeading = heading;
        if (jump)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            rb.linearVelocity = velocity;
            // A coyote jump keeps the heading captured when leaving the ledge.
            if (isGrounded) BeginAirborne(heading);
            isGrounded = false;
        }
        wasGrounded = isGrounded;
        // Contacts support the body on the floor. Gravity still applies during
        // ascent and while player commands are suspended.
        rb.AddForce(Vector3.down * Mathf.Abs(gravity), ForceMode.Acceleration);
    }

    private void GroundCheck()
    {
        isGrounded = false;
        Vector3 scale = capsule.transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(radius, capsule.height * Mathf.Abs(scale.y) * 0.5f);
        Vector3 bottomCenter = capsule.transform.TransformPoint(capsule.center)
            - Vector3.up * (halfHeight - radius);
        float skin = radius * 0.05f;
        const float lift = 0.05f;
        Vector3 origin = bottomCenter + Vector3.up * lift;
        float distance = lift + skin + groundCheckDistance;
        RaycastHit[] hits = groundHits;
        int count = Physics.SphereCastNonAlloc(origin, radius - skin, Vector3.down,
            hits, distance, groundLayer, QueryTriggerInteraction.Ignore);
        // Avoid allocations normally, without losing valid ground in crowded geometry.
        if (count == hits.Length)
        {
            hits = Physics.SphereCastAll(origin, radius - skin, Vector3.down,
                distance, groundLayer, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }
        float minimumNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.attachedRigidbody == rb || hit.collider.transform.IsChildOf(transform)) continue;
            Vector3 surfaceVelocity = hit.rigidbody ? hit.rigidbody.GetPointVelocity(hit.point) : Vector3.zero;
            // A jump separates from the surface; uphill travel along it does not.
            bool separating = Vector3.Dot(rb.linearVelocity - surfaceVelocity, hit.normal) > 0.1f;
            if (!separating && hit.normal.y >= minimumNormalY)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private Quaternion Heading()
    {
        return Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    private void BeginAirborne(Quaternion heading)
    {
        airHeading = heading;
        momentumJump = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude >= momentumThreshold;
    }

    private void Move(Quaternion heading)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        // External impulses can promote an airborne jump to momentum mode, but
        // slowing down mid-jump must not suddenly unlock instant reversals.
        if (!isGrounded && horizontal.magnitude >= momentumThreshold) momentumJump = true;
        // Low-speed parkour corrections follow the view; fast flight retains its
        // takeoff-relative controls so looking around cannot redirect momentum.
        Quaternion controlHeading = isGrounded || !momentumJump ? heading : airHeading;
        Vector3 localInput = new Vector3(input.Move.x, 0f, input.Move.y);
        Vector3 wish = controlHeading * Vector3.ClampMagnitude(localInput, 1f);
        float yawDelta = Mathf.DeltaAngle(previousHeading.eulerAngles.y, heading.eulerAngles.y);
        horizontal = CalculateHorizontalVelocity(horizontal, wish, input.Move.y,
            isGrounded, momentumJump, yawDelta, moveSpeed, momentumThreshold,
            groundSteering, airSteering, coastDeceleration, brakeDeceleration,
            lowSpeedGroundAcceleration, lowSpeedGroundDeceleration,
            lowSpeedAirAcceleration, lowSpeedAirDeceleration,
            Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }

    // Pure movement policy, kept separate from contacts/input so its behavioral
    // rules can be regression-tested without loading a scene.
    public static Vector3 CalculateHorizontalVelocity(Vector3 velocity, Vector3 wish,
        float forwardInput, bool grounded, bool fastJump, float yawDelta,
        float normalSpeed, float threshold, float groundControl, float airControl,
        float coast, float brake, float lowSpeedAcceleration, float lowSpeedDeceleration,
        float lowAirAcceleration, float lowAirDeceleration, float dt)
    {
        float speed = velocity.magnitude;
        float strength = Mathf.Clamp01(wish.magnitude);
        bool momentum = grounded ? speed >= threshold : fastJump || speed >= threshold;
        if (!momentum)
        {
            Vector3 target = wish.normalized * normalSpeed * strength;
            if (grounded)
            {
                float groundRate = strength > 0f ? lowSpeedAcceleration : lowSpeedDeceleration;
                return Vector3.MoveTowards(velocity, target, groundRate * dt);
            }
            // Minecraft-inspired parkour: carry into the jump, steer with input,
            // and bleed off horizontal movement on release instead of snapping.
            float acceleration = strength > 0f ? lowAirAcceleration : lowAirDeceleration;
            return Vector3.MoveTowards(velocity, target, acceleration * dt);
        }

        if (!grounded)
        {
            if (strength <= 0f) return velocity;
            // Steering is deliberately small and gets weaker as speed increases.
            Vector3 target = wish.normalized * speed;
            float control = airControl * Mathf.Min(1f, threshold / Mathf.Max(speed, 0.01f));
            return Vector3.MoveTowards(velocity, target, control * strength * dt);
        }

        // Turning the view while pushing forward rotates existing ground momentum.
        // This is deliberately absent from airborne movement.
        if (forwardInput > 0f) velocity = RotateHorizontal(velocity, yawDelta * Mathf.Deg2Rad);
        if (forwardInput < 0f)
        {
            // Brake first. Reverse movement becomes available below the threshold.
            speed = Mathf.Max(0f, speed - (coast + brake * -forwardInput) * dt);
            return velocity.normalized * speed;
        }
        if (forwardInput <= 0f) speed = Mathf.Max(0f, speed - coast * dt);
        velocity = velocity.normalized * speed;
        if (strength <= 0f) return velocity;
        float steering = groundControl * Mathf.Min(1f, threshold / Mathf.Max(speed, 0.01f));
        // Rotate gradually without manufacturing or discarding speed when strafing.
        float angle = (float)System.Math.Atan2(velocity.z * wish.x - velocity.x * wish.z,
            Vector3.Dot(velocity, wish));
        float maxTurn = steering * strength * dt / Mathf.Max(speed, 0.01f);
        return RotateHorizontal(velocity, Mathf.Clamp(angle, -maxTurn, maxTurn));
    }

    private static Vector3 RotateHorizontal(Vector3 velocity, float radians)
    {
        float sin = (float)System.Math.Sin(radians);
        float cos = (float)System.Math.Cos(radians);
        return new Vector3(velocity.x * cos + velocity.z * sin, 0f,
            velocity.z * cos - velocity.x * sin);
    }

    // Scaled simulation time makes both windows independent of render rate and
    // keeps them frozen during a world pause. One press can authorize one jump.
    public sealed class JumpGrace
    {
        private double lastGrounded = double.NegativeInfinity;
        private double bufferedUntil = double.NegativeInfinity;
        private bool pending;

        public void Reset()
        {
            lastGrounded = bufferedUntil = double.NegativeInfinity;
            pending = false;
        }

        public bool Step(double now, bool grounded, bool pressed, bool active,
            double coyoteSeconds, double bufferSeconds)
        {
            if (!active)
            {
                Reset();
                return false;
            }
            if (grounded) lastGrounded = now;
            if (pressed)
            {
                pending = true;
                bufferedUntil = now + System.Math.Max(0d, bufferSeconds);
            }
            if (now > bufferedUntil) pending = false;
            bool canJump = grounded || now - lastGrounded <= System.Math.Max(0d, coyoteSeconds);
            if (!pending || !canJump) return false;
            // Consume both the buffered press and the ledge grace period.
            Reset();
            return true;
        }
    }
}
