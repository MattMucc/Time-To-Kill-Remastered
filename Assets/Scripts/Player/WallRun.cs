using TMPro;
using UnityEngine;

public class WallRun : MonoBehaviour
{
    [Header("Wall Run Settings")]
    public float wallRunSpeed = 10f;
    public float wallRunGravityMultiplier = 0.25f;

    [Tooltip("How fast gravity ramps up to full force instead of being reduced when idle.")]
    public float idleGravityRamp = 3f;

    [Tooltip("How long it takes before the full force of gravity applies.")]
    public float idleGravityRampTime = 2f;
    public float cameraTiltAngle = 15f;
    public float cameraTiltSpeed = 6f;
    public float wallDetectionDistance = 1f;

    [Tooltip("SphereCast forgiveness.")]
    public float wallDetectionRadius = 0.3f;

    [Tooltip("A percent of minJumpOffForce used upwards.")]
    public LayerMask wallLayer;
    public bool requireJumpToStart = false;

    [Header("Wall Jump Settings")]
    public float minJumpOffForce = 5f;
    public float jumpUpFactor = 0.75f;
    private float lastJumpPressTime = -1f;
    private float jumpBufferTime = 0.2f; // Allow jump input up to 0.2 seconds early

    [Header("Wall Running State (Read-Only)")]
    public bool isWallRunning = false;
    public bool isWallRight = false;
    private Vector3 wallNormal;
    private float idleTime = 0f;

    [Header("References")]
    private PlayerController movement;
    private CameraController camController;
    private CharacterController controller;
    private Transform cameraRoot;

    [Header("Cache")]
    private RaycastHit lastHit;

    private void Start()
    {
        movement = GetComponent<PlayerController>();
        camController = Camera.main.GetComponent<CameraController>();
        controller = GetComponent<CharacterController>();
        cameraRoot = Camera.main.transform.parent;
    }

    private void Update()
    {
        if (InputManager.instance.JumpPressed)
            lastJumpPressTime = Time.time;

        DetectWall();
        WallRunInput();
        HandleWallRun();
    }

    private void DetectWall()
    {
        if (isWallRunning)
            return;

        bool rightHit = Physics.SphereCast(DetectOrigin, wallDetectionRadius, transform.right, out RaycastHit hitR, wallDetectionDistance, wallLayer, QueryTriggerInteraction.Ignore);
        bool leftHit = Physics.SphereCast(DetectOrigin, wallDetectionRadius, -transform.right, out RaycastHit hitL, wallDetectionDistance, wallLayer, QueryTriggerInteraction.Ignore);
        if (rightHit || leftHit)
        {
            isWallRight = rightHit;
            lastHit = rightHit ? hitR : hitL;
            wallNormal = lastHit.normal;
        }
        else
        {
            if (!isWallRunning)
                wallNormal = Vector3.zero;
        }
    }

    private void WallRunInput()
    {
        if (!isWallRunning && IsNearWall)
        {
            if (requireJumpToStart && InputManager.instance.JumpPressed)
            {
                StartWallRun();
            }
            else if (!movement.isGrounded)
                StartWallRun();
        }
    }

    private void StartWallRun()
    {
        movement.jumpCount = 0;
        isWallRunning = true;
        movement.overrideSpeed = true;
        movement.overrideJump = true;
        movement.VerticalVelocity = 0f;

        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;

        Quaternion targetRot = Quaternion.LookRotation(wallForward, Vector3.up);
        transform.rotation = targetRot;
        camController.SetWallRunYawAnchor();
    }

    private void StopWallRun()
    {
        if (!isWallRunning)
            return;

        isWallRunning = false;
        movement.overrideSpeed = false;
        movement.overrideJump = false;

        // reset camera roll
        Vector3 rot = cameraRoot.localEulerAngles;
        rot.z = 0f;
        cameraRoot.localEulerAngles = rot;
        idleTime = 0f;
    }

    private void HandleWallRun()
    {
        if (!isWallRunning)
            return;

        if (movement.isGrounded)
        {
            StopWallRun();
            return;
        }

        // Check if still near wall
        bool stillNearWall = Physics.SphereCast(DetectOrigin, wallDetectionRadius, isWallRight ? transform.right : -transform.right, out RaycastHit hit, wallDetectionDistance, wallLayer, QueryTriggerInteraction.Ignore);
        if (!stillNearWall)
        {
            StopWallRun();
            return;
        }

        // Get forward direction along wall
        wallNormal = hit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;

        // Allign player to wall
        Quaternion targetRot = Quaternion.LookRotation(wallForward, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 10f * Time.deltaTime);

        float dynamicMultiplier;
        bool moveForward = InputManager.instance.Move.y > 0f;
        if (moveForward)
        {
            idleTime = 0f;
            dynamicMultiplier = wallRunGravityMultiplier;
        }
        else
        {
            idleTime += Time.deltaTime;
            float t = Mathf.Clamp01(idleTime / idleGravityRampTime);
            dynamicMultiplier = Mathf.Lerp(wallRunGravityMultiplier, 1f, t);
        }

        movement.VerticalVelocity += movement.gravity * dynamicMultiplier * Time.deltaTime;

        Vector3 moveDir = Vector3.zero;
        if (moveForward)
            moveDir += wallForward * wallRunSpeed;

        moveDir.y = movement.VerticalVelocity;
        controller.Move(moveDir * Time.deltaTime);

        // Slight stick to wall
        controller.Move(-wallNormal * 5f * Time.deltaTime);

        // Camera tilt
        float targetTilt = isWallRight ? cameraTiltAngle : -cameraTiltAngle;
        float zRot = Mathf.LerpAngle(cameraRoot.localEulerAngles.z, targetTilt, cameraTiltSpeed * Time.deltaTime);
        cameraRoot.localRotation = Quaternion.Euler(cameraRoot.localEulerAngles.x, cameraRoot.localEulerAngles.y, zRot);

        if (InputManager.instance.JumpPressed && Time.time - lastJumpPressTime <= jumpBufferTime)
        {
            Vector3 forwardDir = transform.forward;
            Vector3 upDir = Vector3.up * (minJumpOffForce * jumpUpFactor);
            Vector3 awayFromWall = Vector3.Lerp(forwardDir, wallNormal, 0.3f).normalized;
            Vector3 jumpDir = (awayFromWall * minJumpOffForce) + upDir;
            Vector3 preserveMomentum = forwardDir * wallRunSpeed * 0.5f;
            movement.ApplyExternalForce(jumpDir + preserveMomentum);
            StopWallRun();
            idleTime = 0f;
            lastJumpPressTime = -1f;
        }
    }

    public void StartWallRunFromController() => StartWallRun();
    public bool IsNearWall => wallNormal != Vector3.zero;
    private Vector3 DetectOrigin => transform.position + Vector3.up * Mathf.Max(1f, controller.height * 0.5f);
}