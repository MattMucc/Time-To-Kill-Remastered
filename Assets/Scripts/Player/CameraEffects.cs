using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("FOV Settings")]
    public float normalFOV = 60f;
    public float fovChangeSpeed = 8f;
    private float currentFOV;

    [Header("Burst Settings")]
    public float maxBurstIncrease = 15f;
    public float burstFadeSpeed = 1.5f;

    [Tooltip("The lower this value, the easier it is to hit the max FOV burst")]
    public float speedNormalizeMax = 15f;
    public bool scaleBurstWithSpeed = true;

    private float burstIntensity = 0f;

    [Header("ADS Settings")]
    public float adsBlendSpeed = 10f;
    public bool isADS = false;

    private float adsFOVTarget;
    private float adsWeight = 0f;
    private float adsTargetWeight = 0f;
    private float adsWeightVel = 0f; // Optional: If you use SmoothDamp

    [Header("Recoil Settings")]
    public float recoilRecoverySpeed = 8f;
    public bool enableCameraRecoil = true;

    public float currentRecoilPitch = 0f;
    public float currentRecoilYaw = 0f;

    private float recoilX;
    private float recoilY;
    private float recoilVelocityX;
    private float recoilVelocityY;
    private float appliedYawOffset = 0f;

    [Header("References (Optional)")]
    public Transform playerTransform;
    public Transform cameraRoot;
    private PlayerController movement;
    private MultiplierSystem multiplier;
    private SlideAttack slide;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        normalFOV = cam.fieldOfView;
        currentFOV = cam.fieldOfView;

        playerTransform = transform.root;
        cameraRoot = transform.parent;
        movement = playerTransform.GetComponent<PlayerController>();
        multiplier = playerTransform.GetComponent<MultiplierSystem>();
        slide = playerTransform.GetComponent<SlideAttack>();
    }

    private void LateUpdate()
    {
        UpdateFOV();
        UpdateRecoil();
    }

    private void UpdateFOV()
    {
        //adsWeight = Mathf.MoveTowards(adsWeight, adsTargetWeight, adsBlendSpeed * Time.deltaTime);
        adsWeight = Mathf.SmoothDamp(adsWeight, adsTargetWeight, ref adsWeightVel, 1f / adsBlendSpeed);
        float speedFactor = 1f;
        if (scaleBurstWithSpeed)
            speedFactor = Mathf.Clamp01(GetSpeed() / speedNormalizeMax);

        // Blend between ADS FOV and normal FOV
        float burstAdd = maxBurstIncrease * burstIntensity * speedFactor;
        float targetFOV = Mathf.Lerp(normalFOV + burstAdd, adsFOVTarget, adsWeight);

        // Smooth transition
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
        cam.fieldOfView = currentFOV;

        // Decay burst intensity over time
        burstIntensity = Mathf.MoveTowards(burstIntensity, 0f, burstFadeSpeed * Time.deltaTime);
    }

    /// <summary> Triggers a temporary FOV burst (e.g. slide, dash, boost) </summary>
    public void TriggerFOVBurst(float intensity = 1f)
    {
        burstIntensity = Mathf.Clamp01(intensity);
    }

    public void SetADS(bool aiming, float targetFOV)
    {
        // Only update if state/target changes
        if (isADS == aiming && Mathf.Approximately(adsFOVTarget, targetFOV))
            return;

        isADS = aiming;
        adsFOVTarget = targetFOV;
        adsTargetWeight = aiming ? 1f : 0f;
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        if (!enableCameraRecoil)
            return;

        recoilX += vertical;
        recoilY += horizontal;
    }

    private void UpdateRecoil()
    {
        if (!enableCameraRecoil)
        {
            currentRecoilPitch = Mathf.MoveTowards(currentRecoilPitch, 0f, recoilRecoverySpeed * Time.deltaTime);
            currentRecoilYaw = Mathf.MoveTowards(currentRecoilYaw, 0f, recoilRecoverySpeed * Time.deltaTime);
            return;
        }

        recoilX = Mathf.SmoothDamp(recoilX, 0f, ref recoilVelocityX, 1f / recoilRecoverySpeed);
        recoilY = Mathf.SmoothDamp(recoilY, 0f, ref recoilVelocityY, 1f / recoilRecoverySpeed);
        currentRecoilPitch = recoilX;
        currentRecoilYaw = recoilY;

        /*if (!enableCameraRecoil || !cameraRoot)
            return;

        // Smooth decay back to nuetral
        recoilX = Mathf.SmoothDamp(recoilX, 0f, ref recoilVelocityX, 1f / recoilRecoverySpeed);
        recoilY = Mathf.SmoothDamp(recoilY, 0f, ref recoilVelocityY, 1f / recoilRecoverySpeed);

        float basePitch = cameraRoot.localEulerAngles.x;
        if (basePitch > 180f)
            basePitch -= 360f;

        float finalPitch = basePitch - recoilX; // Pushes view upwards
        cameraRoot.localRotation = Quaternion.Euler(finalPitch, 0f, 0f);

        float desiredYawOffset = recoilY;
        float deltaYaw = desiredYawOffset - appliedYawOffset;
        if (Mathf.Abs(deltaYaw) > 0.0001f)
        {
            playerTransform.Rotate(0f, deltaYaw, 0f, Space.World);
            appliedYawOffset = desiredYawOffset;
        }*/
    }

    private float GetSpeed()
    {
        if (!movement || !multiplier || !slide)
            return 0f;

        if (slide.isSliding)
            return slide.slideSpeedBoost * multiplier.GetSpeedMultiplier();

        return movement.moveSpeed * multiplier.GetSpeedMultiplier();
    }
}