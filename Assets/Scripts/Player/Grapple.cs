using System.Collections;
using UnityEngine;

public class Grapple : MonoBehaviour
{
    [Header("Grapple Settings")]
    public float maxGrappleDistance = 50f;
    public float pullSpeed = 25f;
    [Tooltip("Gives a speed boost after reaching the grappled point. Doesn't give a speed boost when releasing the grapple mid-air.")]
    [Range(1f, 2f)] public float speedBoost = 1.2f;
    public float stopDistance = 3f;
    public float grappleCooldown = 3f;

    [Tooltip("How close the crosshair must be to an enemy to lock onto them.")]
    public float aimAssistRange = 2f;
    public bool isGrappling = false;
    public bool canGrapple = true;
    public bool keyReleased = true;

    [Header("SphereCast Options")]
    [Tooltip("Use a SphereCast instead of a RayCast for more forgiving targeting (recommended for fast-paced play).")]
    public bool useSphereCast = true;
    public float sphereCastRadius = 0.25f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController controller;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask grappleLayer;

    private PlayerController movement;
    private MultiplierSystem multiplier;
    private CameraEffects camFX;

    private Vector3 grapplePoint;
    private Transform grappleTarget; // used if grappling onto an enemy

    private void Start()
    {
        movement = GetComponent<PlayerController>();
        multiplier = GetComponent<MultiplierSystem>();
        camFX = Camera.main.GetComponent<CameraEffects>();
        cameraTransform = Camera.main.transform;
        controller = GetComponent<CharacterController>();
        if (!lineRenderer)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
        }
    }

    private void Update()
    {
        if (!InputManager.instance)
            return;

        HandleGrappleUI();
        bool grappleHeld = InputManager.instance.GrappleHeld;
        if (grappleHeld && canGrapple && keyReleased && !isGrappling)
            StartGrapple();
        else if (!grappleHeld)
        {
            keyReleased = true;
            if (isGrappling)
                StopGrapple(false);
        }

        if (isGrappling)
            UpdateGrapple();
    }

    private void HandleGrappleUI()
    {
        if (isGrappling)
        {
            UIManager.instance.ToggleGrappleLockOn(false);
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == gameObject)
                return;

            if (hit.collider.CompareTag("Enemy"))
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hit.collider.transform.position);
                float distanceToCenter = Vector2.Distance(screenPos, new Vector2(Screen.width / 2f, Screen.height / 2f));
                if (distanceToCenter < aimAssistRange)
                    UIManager.instance.ToggleGrappleLockOn(true);
                else
                    UIManager.instance.ToggleGrappleLockOn(false);
            }
            else
                UIManager.instance.ToggleGrappleLockOn(false);
        }
        else
            UIManager.instance.ToggleGrappleLockOn(false);
    }

    private void StartGrapple()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        bool hitSomething = useSphereCast ? Physics.SphereCast(ray, sphereCastRadius, out hit, maxGrappleDistance, grappleLayer, QueryTriggerInteraction.Ignore) : Physics.Raycast(ray, out hit, maxGrappleDistance, grappleLayer, QueryTriggerInteraction.Ignore);
        if (!hitSomething || hit.collider.gameObject == gameObject)
            return;

        isGrappling = true;
        keyReleased = false;
        grapplePoint = hit.point;
        grappleTarget = hit.collider.CompareTag("Enemy") ? hit.collider.transform : null;
        lineRenderer.enabled = true;
        movement.overrideSpeed = true; // Disables normal Move input while grappling
        /*if (camFX)
            camFX.TriggerFOVBurst(0.5f);*/

        if (multiplier && grappleTarget)
        {
            HitmarkerManager.instance.ShowHitmarker(HitmarkerManager.HitType.Normal);
            multiplier.AddGrappleHit();
        }

        // Optional: Add small burst force for initial feel
        Vector3 direction = (grapplePoint - transform.position).normalized;
        movement.ApplyExternalForce(direction * 10f);
    }

    private void StopGrapple(bool reachedPoint)
    {
        if (!isGrappling)
            return;

        // Preserves momentum on release
        Vector3 targetPoint = grappleTarget ? grappleTarget.position : grapplePoint;
        Vector3 grappleDirection = (targetPoint - transform.position).normalized;
        Vector3 camDirection = cameraTransform.forward;
        camDirection.Normalize();

        // Blend direction based on how different the angle is towards the grapple point
        float angle = Vector3.Angle(grappleDirection, camDirection);
        float t = Mathf.InverseLerp(120f, 60f, angle); // 0-1 blend range
        Vector3 finalDirection = Vector3.Slerp(grappleDirection, camDirection, t);

        float momentumForce = reachedPoint ? pullSpeed * speedBoost : pullSpeed;
        movement.ApplyExternalForce(finalDirection * momentumForce);

        isGrappling = false;
        lineRenderer.enabled = false;
        movement.overrideSpeed = false;
        StartCoroutine(GrappleCooldown());
    }

    private void UpdateGrapple()
    {
        Vector3 targetPoint = grappleTarget ? grappleTarget.position : grapplePoint;
        Vector3 direction = (targetPoint - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPoint);
        if (distance <= stopDistance)
        {
            StopGrapple(true);
            return;
        }

        //float currentPullSpeed = Mathf.Lerp(pullSpeed, 8f, Mathf.InverseLerp(maxGrappleDistance, stopDistance, distance));
        Vector3 velocity = direction * pullSpeed;
        controller.Move(velocity * Time.deltaTime);

        // Optionally: Cancel gravity while grappling (prevents being dragged down)
        movement.VerticalVelocity = 0f;

        // Draw line
        lineRenderer.SetPosition(0, cameraTransform.position);
        lineRenderer.SetPosition(1, targetPoint);
    }

    private IEnumerator GrappleCooldown()
    {
        canGrapple = false;
        float timer = 0f;
        while (timer < grappleCooldown)
        {
            timer += Time.deltaTime;
            UIManager.instance.UpdateGrappleCooldown(timer / grappleCooldown);
            yield return null;
        }

        UIManager.instance.UpdateGrappleCooldown(0f);
        canGrapple = true;
    }
}