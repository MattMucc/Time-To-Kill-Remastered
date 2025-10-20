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
        UpdateSpeedFOV();
    }

    /*private void UpdateFOV()
    {
        if (!movement)
            return;

        // ADS Blend
        adsWeight = Mathf.SmoothDamp(adsWeight, adsTargetWeight, ref adsWeightVel, 1f / adsBlendSpeed);
        float baseFOV = Mathf.Lerp(normalFOV, adsFOVTarget, adsWeight);

        // Speed-Based
        float baseSpeed = movement.moveSpeed * multiplier.GetSpeedMultiplier();
        float currentSpeed = movement.CurrentSpeedMagnitude;
        float speedBoost = 0f;
        if (currentSpeed > baseSpeed)
        {
            float speedRatio = Mathf.InverseLerp(baseSpeed, baseSpeed * 3f, currentSpeed);
            speedBoost = maxBurstIncrease * speedRatio;
        }

        // Burst Intensity
        float speedFactor = 1f;
        if (scaleBurstWithSpeed)
            speedFactor = Mathf.Clamp01(GetSpeed() / speedNormalizeMax);

        float burstAdd = maxBurstIncrease * burstIntensity * speedFactor;

        // Final FOV Target
        float targetFOV = baseFOV + speedBoost + burstAdd;

        // Lerp Smoothly
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
        cam.fieldOfView = currentFOV;

        // Decay Burst
        burstIntensity = Mathf.MoveTowards(burstIntensity, 0f, burstFadeSpeed * Time.deltaTime);
    }*/

    private void UpdateFOV()
    {
        //adsWeight = Mathf.MoveTowards(adsWeight, adsTargetWeight, adsBlendSpeed * Time.deltaTime);
        adsWeight = Mathf.SmoothDamp(adsWeight, adsTargetWeight, ref adsWeightVel, 1f / adsBlendSpeed);
        float speedFactor = 1f;
        if (scaleBurstWithSpeed)
            speedFactor = Mathf.Clamp01(movement.Speed / speedNormalizeMax);

        // Blend between ADS FOV and normal FOV
        float burstAdd = maxBurstIncrease * burstIntensity * speedFactor;
        float targetFOV = Mathf.Lerp(normalFOV + burstAdd, adsFOVTarget, adsWeight);

        // Smooth transition
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
        cam.fieldOfView = currentFOV;

        // Decay burst intensity over time
        burstIntensity = Mathf.MoveTowards(burstIntensity, 0f, burstFadeSpeed * Time.deltaTime);
    }

    public void UpdateSpeedFOV()
    {
        if (!movement)
            return;

        float baseSpeed = movement.Speed;
        float currentSpeed = movement.CurrentSpeedMagnitude;
        if (currentSpeed > baseSpeed)
        {
            float speedRatio = Mathf.InverseLerp(baseSpeed, baseSpeed * 3f, currentSpeed); // Tweak 3f for max effect
            float targetFOV = normalFOV + maxBurstIncrease * speedRatio;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
        }
        else
            currentFOV = Mathf.Lerp(currentFOV, normalFOV, fovChangeSpeed * Time.deltaTime);

        cam.fieldOfView = currentFOV;
    }

    /// <summary> Triggers a temporary FOV burst. </summary>
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

    /*private float GetSpeed()
    {
        if (!movement || !multiplier || !slide)
            return 0f;

        if (slide.isSliding)
            return slide.slideSpeedBoost * multiplier.GetSpeedMultiplier();

        return movement.moveSpeed * multiplier.GetSpeedMultiplier();
    }*/
}