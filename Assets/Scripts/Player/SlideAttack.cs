using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class SlideAttack : MonoBehaviour
{
    [Header("Slide Settings")]
    [Tooltip("Not for editing, only to see value change on runtime")]
    [SerializeField] private float currentSpeed;
    public float slideSpeedBoost = 12f;
    public float heldSlideFriction = 3f;
    public float releasedSlideFriction = 6f;

    [Tooltip("Multiplies decay while in air (lower = less decay).")]
    public float airFrictionMultiplier = 0.25f;
    public float crouchSpeed = 3f;
    public float slideCooldown = 2f;
    public float minDamageSpeed = 10f;
    public bool isSliding = false;
    [SerializeField] private bool canSlide = true;
    [SerializeField] private bool slideOnCooldown = false;

    [Header("Crouch Settings")]
    public float standingHeight = 2f;
    public float slideHeight = 1f;
    public float heightChangeSpeed = 10f;
    private Vector3 standingCameraHeight;
    private Vector3 standingCenter;
    private Vector3 slideCenter;

    [Header("Attack Settings")]
    public float slideAttackDamage = 10f;
    public float continuousDamageInterval = 0.5f;
    public bool enableContinuousDamage = false;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraRootTransform;
    [SerializeField] private Collider slideCollider;
    private PlayerController movement;
    private MultiplierSystem multiplier;
    private CameraEffects cameraEffects;
    private CharacterController controller;

    private float continuousDamageTimer = 0f;
    private HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

    private void Start()
    {
        playerTransform = transform;
        movement = GetComponent<PlayerController>();
        multiplier = GetComponent<MultiplierSystem>();
        cameraEffects = Camera.main.gameObject.GetComponent<CameraEffects>();
        controller = GetComponent<CharacterController>();
        slideCollider.enabled = false;
        standingCameraHeight = cameraRootTransform.localPosition;
        standingHeight = controller.height;
        standingCenter = controller.center;
        slideCenter = new Vector3(0f, slideHeight / 2f, 0f);
    }

    private void Update()
    {
        if (!InputManager.instance)
            return;

        SlideInput();
        SlideMovement();
        HandleSlideHeight();
        HandleCameraSlideHeight();
        movement.overrideSpeed = isSliding;
    }

    private void SlideInput()
    {
        if (InputManager.instance.SlidePressed && canSlide && !isSliding)
            StartSlide();

        if (!InputManager.instance.SlideHeld && isSliding && movement.isGrounded)
            StopSlide();
    }

    private void SlideMovement()
    {
        // If not sliding, gradually decay momentum
        if (!isSliding)
        {
            float targetSpeed = movement.Speed;

            // When near the target speed, stop the movement force
            if (currentSpeed <= targetSpeed + 0.05f)
            {
                currentSpeed = targetSpeed;
                return;
            }

            // If airborne, preserve momentum (no decay until you hit the ground)
            if (!movement.isGrounded)
            {
                controller.Move(transform.forward * currentSpeed * Time.deltaTime);
                return;
            }

            // On ground, gradually lose extra momentum/speed
            if (currentSpeed > targetSpeed)
            {
                float deceleration = releasedSlideFriction * Time.deltaTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration);
            }

            // Apply preserved momentum
            if (currentSpeed > targetSpeed)
                controller.Move(transform.forward * currentSpeed * Time.deltaTime);

            return;
        }
        else
        {
            // Slightly re-trigger FOV burst when slidinmg downhill or movement spikes mid-slide
            if (currentSpeed > movement.Speed * 1.1f)
            {
                if (cameraEffects)
                {
                    float normalizedSpeed = Mathf.InverseLerp(movement.Speed, slideSpeedBoost, currentSpeed);
                    //cameraEffects.TriggerFOVBurst(normalizedSpeed * 0.5f); // Smaller continuous boost
                }
            }
        }

        // While sliding, gradually decay speed
        float targetSlideSpeed = crouchSpeed;
        float friction = (InputManager.instance.SlideHeld ? heldSlideFriction : releasedSlideFriction);
        if (!movement.isGrounded)
            friction *= airFrictionMultiplier; // Adjust if you want slower in-air decay

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSlideSpeed, friction * Time.deltaTime);

        // Apply movement velocity
        Vector3 movDir = transform.forward * currentSpeed * Time.deltaTime;
        controller.Move(movDir);

        // Stop sliding if we've slowed down to crouch speed, let go of the slide key, and are on the ground
        if (movement.isGrounded && currentSpeed <= crouchSpeed + 0.1f && !InputManager.instance.SlideHeld)
            StopSlide();
    }

    private void StartSlide()
    {
        isSliding = true;
        canSlide = false;
        currentSpeed = slideSpeedBoost * multiplier.GetSpeedMultiplier();
        slideCollider.enabled = true;
        if (cameraEffects)
        {
            float normalizedSpeed = Mathf.InverseLerp(movement.Speed, slideSpeedBoost, currentSpeed);
            //cameraEffects.TriggerFOVBurst(normalizedSpeed);
        }
    }

    private void StopSlide()
    {
        if (!isSliding)
            return;

        // Prevent standing up when obstructed
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, standingHeight - slideHeight + 0.2f))
            return;

        isSliding = false;
        slideCollider.enabled = false;
        currentSpeed = 0f;
        StartCoroutine(SlideCooldown());
        damagedEnemies.Clear();
    }

    private void HandleSlideHeight()
    {
        float targetHeight = InputManager.instance.SlideHeld && !slideOnCooldown ? slideHeight : standingHeight;
        Vector3 targetCenter = InputManager.instance.SlideHeld && !slideOnCooldown ? slideCenter : standingCenter;
        controller.height = Mathf.Lerp(controller.height, targetHeight, heightChangeSpeed * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, targetCenter, heightChangeSpeed * Time.deltaTime);
    }

    private void HandleCameraSlideHeight()
    {
        Vector3 camLocalPos = cameraRootTransform.localPosition;
        float targetY = InputManager.instance.SlideHeld && !slideOnCooldown ? slideCenter.y + 0.5f : standingCameraHeight.y;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetY, heightChangeSpeed * Time.deltaTime);
        cameraRootTransform.localPosition = camLocalPos;
    }

    private void DealSlideDamage(EnemyHealth enemyHealth)
    {
        float totalDamage = slideAttackDamage * multiplier.GetDamageMultiplier();
        enemyHealth.TakeDamage(totalDamage, EnemyHurtbox.HurtboxType.Body, enemyHealth.transform.position);
        multiplier.AddSlideHit();
    }

    private IEnumerator SlideCooldown()
    {
        slideOnCooldown = true;
        float cooldown = slideCooldown;
        float timer = 0f;
        while (timer < cooldown)
        {
            timer += Time.deltaTime;
            UIManager.instance.UpdateSlideCooldown(timer / cooldown);
            yield return null;
        }

        UIManager.instance.UpdateSlideCooldown(0f);
        canSlide = true;
        slideOnCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isSliding || currentSpeed < minDamageSpeed)
            return;

        if (other.TryGetComponent(out EnemyHealth enemyHealth))
        {
            if (!damagedEnemies.Contains(enemyHealth))
            {
                DealSlideDamage(enemyHealth);
                damagedEnemies.Add(enemyHealth);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isSliding || currentSpeed < minDamageSpeed || !enableContinuousDamage)
            return;

        continuousDamageTimer -= Time.deltaTime;
        if (continuousDamageTimer > 0f)
            return;

        if (other.TryGetComponent(out EnemyHealth enemyHealth))
            DealSlideDamage(enemyHealth);

        continuousDamageTimer = continuousDamageInterval;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out EnemyHealth enemyHealth))
        {
            if (damagedEnemies.Contains(enemyHealth))
                damagedEnemies.Remove(enemyHealth);
        }
    }
}