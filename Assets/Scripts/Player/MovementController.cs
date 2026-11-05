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
    [SerializeField, Min(0f)] private float coastDeceleration = 12f;
    [SerializeField, Min(0f)] private float brakeDeceleration = 12f;
    private Quaternion previousHeading;
    private Quaternion airHeading;
    private bool wasGrounded;
    private bool momentumJump;

    public enum SlideControlMode { Hold, Toggle }

    [Header("Slide Settings")]
    [SerializeField] private SlideControlMode slideControlMode = SlideControlMode.Hold;
    [SerializeField, Min(0f)] private float slideStartSpeed = 4f;
    [SerializeField, Min(0f)] private float slideBoost = 3f;
    [Tooltip("Seconds before slide braking begins; slope gravity remains active.")]
    [SerializeField, Min(0f)] private float slideBoostDuration = 0.35f;
    [SerializeField, Min(0f)] private float slideDeceleration = 12f;
    [SerializeField, Min(0f)] private float slideRampDeceleration = 2f;
    [SerializeField, Min(0f)] private float slideSlopeGravity = 1f;
    [SerializeField, Min(0f)] private float slideEndSpeed = 2f;
    [SerializeField, Min(0f)] private float slideCooldown = 0.5f;
    [SerializeField, Min(0.1f)] private float slideHeight = 1.2f;
    [SerializeField, Min(0f)] private float crouchedMoveSpeed = 5f;
    [SerializeField] private Transform slideView;
    [SerializeField] private Transform slideModel;
    public bool IsSliding { get; private set; }
    private bool isLowered;
    private double nextSlideTime;
    private double slideStartedAt;
    private bool onSteepSurface;
    private bool hasSlideSupport;
    private readonly SlideStartRequest slideStartRequest = new SlideStartRequest();
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingViewPosition;
    private Vector3 standingModelPosition;
    private Vector3 standingModelScale;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 groundVelocity;
    private readonly Collider[] clearanceHits = new Collider[16];

    [Header("Jump Settings")]
    [SerializeField, Min(0f)] private float jumpHeight = 2f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;
    private readonly JumpGrace jumpGrace = new JumpGrace();
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField, Min(0f)] private float groundCheckDistance = 0.1f;
    [Tooltip("Maximum slope accepted for sliding support.")]
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 60f;
    [SerializeField, Range(0f, 89f)] private float maxWalkAngle = 40f;
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
        standingHeight = capsule.height;
        standingCenter = capsule.center;
        if (slideView) standingViewPosition = slideView.localPosition;
        if (slideModel)
        {
            standingModelPosition = slideModel.localPosition;
            standingModelScale = slideModel.localScale;
        }
    }

    private void OnEnable()
    {
        jumpGrace.Reset();
        slideStartRequest.Reset();
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
        slideStartRequest.Reset();
        EndSlide();
    }

    private void FixedUpdate()
    {
        GroundCheck();
        Quaternion heading = Heading();
        if (wasGrounded && !isGrounded) BeginAirborne(previousHeading);
        bool jump = jumpGrace.Step(Time.fixedTimeAsDouble, isGrounded,
            input.ConsumeJumpPressed(), input.GameplayActive, coyoteTime, jumpBufferTime);
        bool slidePressed = input.ConsumeSlidePressed();
        bool slideStep = false;
        bool jumpingFromSlide = jump && IsSliding;
        bool wasSliding = IsSliding;

        if (IsSliding && ShouldEndSlide(input.GameplayActive, jump,
            slideControlMode == SlideControlMode.Toggle, input.IsSlideHeld, slidePressed)) EndSlide();
        if (isLowered && !IsSliding) TryStand();
        // A late ledge press is retained while held (or toggled), then consumed
        // once on landing. It never grants a boost or bypasses cooldown in midair.
        Vector3 relative = rb.linearVelocity - groundVelocity;
        float speed = new Vector2(relative.x, relative.z).magnitude;
        bool startSlide = slideStartRequest.Step(slidePressed, input.IsSlideHeld,
            slideControlMode == SlideControlMode.Toggle, input.GameplayActive, jump,
            wasSliding, !isLowered && Time.fixedTimeAsDouble >= nextSlideTime,
            hasSlideSupport, speed >= slideStartSpeed);
        if (startSlide)
        {
            StartSlide(relative);
            // Landing on a slideable ramp need not be walkable (e.g. 45 degrees).
            isGrounded = true;
        }
        if (IsSliding)
        {
            // Airborne slides remain active and lowered, but have no ground drag,
            // ramp projection or attraction. Full gravity is applied below.
            if (isGrounded)
            {
                float braking = SlideBrakingFraction((float)(Time.fixedTimeAsDouble - slideStartedAt),
                    slideBoostDuration, Time.fixedDeltaTime);
                rb.linearVelocity = groundVelocity + CalculateSlideVelocity(
                    rb.linearVelocity - groundVelocity, groundNormal, Mathf.Abs(gravity),
                    slideSlopeGravity, slideDeceleration * braking, Time.fixedDeltaTime,
                    slideRampDeceleration * braking);
                slideStep = true;
                if (braking > 0f && (rb.linearVelocity - groundVelocity).magnitude <= slideEndSpeed) EndSlide();
            }
        }
        else if (input.GameplayActive && !jumpingFromSlide)
        {
            Move(heading);
            if (isLowered && isGrounded)
            {
                // Allow the player to escape a low ceiling after the slide ends.
                Vector3 velocity = rb.linearVelocity;
                Vector3 horizontal = Vector3.ClampMagnitude(new Vector3(velocity.x, 0f, velocity.z), crouchedMoveSpeed);
                rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            }
        }
        if (!IsSliding && onSteepSurface && !jump)
            rb.linearVelocity = PreventUphillWalking(rb.linearVelocity - groundVelocity, groundNormal) + groundVelocity;
        previousHeading = heading;
        if (jump)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            rb.linearVelocity = velocity;
            if (isGrounded) BeginAirborne(heading);
            isGrounded = false;
            slideStep = false;
        }
        wasGrounded = isGrounded;
        Vector3 gravityForce = Vector3.down * Mathf.Abs(gravity);
        // Slide integration already includes gravity along the ramp. Apply only
        // its normal component here, so downhill gravity is never counted twice.
        if (slideStep) gravityForce = groundNormal * Vector3.Dot(gravityForce, groundNormal);
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    private void GroundCheck()
    {
        isGrounded = false;
        onSteepSurface = false;
        hasSlideSupport = false;
        groundNormal = Vector3.up;
        groundVelocity = Vector3.zero;
        float closestGround = float.PositiveInfinity;
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
            if (!separating && hit.normal.y > 0.01f && hit.distance < closestGround)
            {
                closestGround = hit.distance;
                groundNormal = hit.normal;
                groundVelocity = surfaceVelocity;
                bool walkable = IsWalkableSurface(hit.normal, maxWalkAngle);
                onSteepSurface = !walkable;
                hasSlideSupport = hit.normal.y >= minimumNormalY;
                isGrounded = IsSliding ? hasSlideSupport : walkable;
            }
        }
    }

    private void StartSlide(Vector3 relativeVelocity)
    {
        IsSliding = true;
        slideStartedAt = Time.fixedTimeAsDouble;
        nextSlideTime = Time.fixedTimeAsDouble + slideCooldown;
        rb.linearVelocity = groundVelocity + CalculateSlideEntryVelocity(relativeVelocity, groundNormal, slideBoost);
        SetLowered(true);
    }

    private void EndSlide()
    {
        if (IsSliding) nextSlideTime = Time.fixedTimeAsDouble + slideCooldown;
        IsSliding = false;
        if (isLowered) TryStand();
    }

    private void SetLowered(bool lowered)
    {
        float height = lowered ? Mathf.Clamp(slideHeight, capsule.radius * 2f, standingHeight) : standingHeight;
        float drop = standingHeight - height;
        capsule.height = height;
        // Keep the feet at exactly the same location as the standing capsule.
        capsule.center = standingCenter - Vector3.up * (drop * 0.5f);
        if (slideView) slideView.localPosition = standingViewPosition - Vector3.up * drop;
        if (slideModel)
        {
            slideModel.localPosition = standingModelPosition - Vector3.up * (drop * 0.5f);
            slideModel.localScale = new Vector3(standingModelScale.x,
                standingModelScale.y * height / standingHeight, standingModelScale.z);
        }
        isLowered = lowered;
    }

    private void TryStand()
    {
        Vector3 scale = capsule.transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(radius, standingHeight * Mathf.Abs(scale.y) * 0.5f);
        Vector3 center = capsule.transform.TransformPoint(standingCenter);
        Vector3 offset = Vector3.up * (halfHeight - radius);
        // A tiny skin prevents floor contact from counting as an obstruction.
        float checkRadius = Mathf.Max(0.01f, radius - 0.02f);
        Collider[] hits = clearanceHits;
        int count = Physics.OverlapCapsuleNonAlloc(center - offset, center + offset,
            checkRadius, hits, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length)
        {
            hits = Physics.OverlapCapsule(center - offset, center + offset,
                checkRadius, ~0, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }
        for (int i = 0; i < count; i++)
        {
            if (hits[i].attachedRigidbody == rb || hits[i].transform.IsChildOf(transform)) continue;
            return;
        }
        SetLowered(false);
    }

    public static Vector3 CalculateSlideEntryVelocity(Vector3 velocity, Vector3 surfaceNormal, float boost)
    {
        Vector3 tangent = Vector3.ProjectOnPlane(velocity, surfaceNormal.normalized);
        return tangent + tangent.normalized * boost;
    }

    public static Vector3 CalculateSlideVelocity(Vector3 velocity, Vector3 surfaceNormal,
        float gravityMagnitude, float slopeMultiplier, float deceleration, float dt,
        float rampDeceleration = 2f)
    {
        Vector3 normal = surfaceNormal.normalized;
        Vector3 tangent = Vector3.ProjectOnPlane(velocity, normal);
        Vector3 downhillAcceleration = Vector3.ProjectOnPlane(Vector3.down * gravityMagnitude, normal);
        tangent += downhillAcceleration * slopeMultiplier * dt;
        // Keep strong braking on flat ground without cancelling the acceleration
        // on gentle test ramps. Blend to ramp friction by roughly 10 degrees.
        float slopeBlend = Mathf.Clamp01(new Vector2(normal.x, normal.z).magnitude / 0.17364818f);
        float friction = Mathf.Lerp(deceleration, rampDeceleration, slopeBlend);
        return Vector3.MoveTowards(tangent, Vector3.zero, friction * dt);
    }

    public static bool ShouldEndSlide(bool controlsActive, bool jump, bool toggle, bool held, bool pressed)
    {
        return !controlsActive || jump || (toggle ? pressed : !held);
    }

    public static float SlideBrakingFraction(float elapsed, float duration, float dt)
    {
        if (dt <= 0f) return 0f;
        return Mathf.Clamp01((elapsed + dt - Mathf.Max(0f, duration)) / dt);
    }

    public static bool IsWalkableSurface(Vector3 normal, float maxAngle)
    {
        return normal.normalized.y >= (float)System.Math.Cos(maxAngle * Mathf.Deg2Rad);
    }

    public static Vector3 PreventUphillWalking(Vector3 velocity, Vector3 normal)
    {
        // Only used against a nearby, non-separating steep surface. A jump moving
        // away from it is not constrained. Preserve vertical flight and sideways motion.
        Vector3 uphill = new Vector3(-normal.x, 0f, -normal.z).normalized;
        float uphillSpeed = Vector3.Dot(velocity, uphill);
        return uphillSpeed > 0f ? velocity - uphill * uphillSpeed : velocity;
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

    public sealed class SlideStartRequest
    {
        public bool Pending { get; private set; }

        public void Reset()
        {
            Pending = false;
        }

        public bool Step(bool pressed, bool held, bool toggle, bool controlsActive,
            bool jump, bool alreadySliding, bool canStart, bool grounded, bool enoughSpeed)
        {
            if (!controlsActive || jump || alreadySliding || (!toggle && !held))
            {
                Reset();
                return false;
            }
            if (pressed)
            {
                if (toggle && Pending)
                {
                    Reset();
                    return false;
                }
                // A press rejected during cooldown must not become a later boost.
                Pending = canStart;
            }
            if (!Pending || !grounded) return false;
            // Consume on the first landing even if speed is too low, so an old
            // request cannot unexpectedly activate after starting to walk again.
            Reset();
            return canStart && enoughSpeed;
        }
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
