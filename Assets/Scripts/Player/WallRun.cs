using System.Collections;
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
    private float currentTilt = 0f;

    [Header("Wall Jump Settings")]
    public float wallJumpCooldown = 0.2f;
    public float minJumpOffForce = 5f;
    public float jumpUpFactor = 0.75f;
    private bool wallJumpedRecently = false;

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
        if (movement.isGrounded)
            StopWallRun();

        DetectWall();
        WallRunInput();
        HandleWallRun();
        WallJumpInput();
        HandleCameraTilt();
    }

    private void DetectWall()
    {
        if (isWallRunning || wallJumpedRecently)
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
        if (wallJumpedRecently)
            return;

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

    private void WallJumpInput()
    {
        if (isWallRunning && InputManager.instance.JumpPressed)
        {
            Vector3 camForward = camController.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 upDir = Vector3.up * (minJumpOffForce * jumpUpFactor);
            Vector3 blended = Vector3.Slerp(wallNormal, camForward, 0.8f); // Heavy bias towards camera
            Vector3 jumpDir = (blended.normalized * minJumpOffForce) + upDir;
            Vector3 forwardBoost = camForward * wallRunSpeed * 0.3f;
            //Debug.DrawRay(transform.position, jumpDir.normalized * 3f, Color.cyan, 1f);
            movement.ApplyExternalForce(jumpDir, false);
            controller.Move(wallNormal * 0.3f); // Small wall nudge

            StopWallRun();
            StartCoroutine(WallJumpCooldown());
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

        ClearWallState();

        // Reset player body rotation
        transform.rotation = Quaternion.LookRotation(camController.transform.forward, Vector3.up);
    }

    private void ClearWallState()
    {
        idleTime = 0f;
        wallNormal = Vector3.zero;
        isWallRunning = false;
        movement.overrideSpeed = false;
        movement.overrideJump = false;
    }

    private void HandleCameraTilt()
    {
        float targetTilt = 0f;
        if (isWallRunning)
            targetTilt = isWallRight ? cameraTiltAngle : -cameraTiltAngle;

        // Smooth tilt interpolation
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, cameraTiltSpeed * Time.deltaTime);

        // Apply it cleanly without disturbing pitch/yaw
        cameraRoot.localRotation = Quaternion.Euler(cameraRoot.localEulerAngles.x, cameraRoot.localEulerAngles.y, currentTilt);
    }

    private void HandleWallRun()
    {
        if (!isWallRunning || wallJumpedRecently)
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

        // Apply modified gravity
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
    }

    private IEnumerator WallJumpCooldown()
    {
        wallJumpedRecently = true;
        yield return new WaitForSeconds(wallJumpCooldown);
        wallJumpedRecently = false;
    }

    public void StartWallRunFromController() => StartWallRun();
    public bool IsNearWall => wallNormal != Vector3.zero;
    public float CurrentTilt => currentTilt;
    private Vector3 DetectOrigin => transform.position + Vector3.up * Mathf.Max(1f, controller.height * 0.5f);
}