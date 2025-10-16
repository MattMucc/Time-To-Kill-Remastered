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

    [Header("Attack Settings")]
    public float slideAttackDamage = 10f;
    public bool enableContinuousDamage = false;
    public float continuousDamageInterval = 0.5f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Collider slideCollider;
    private PlayerController movement;
    private MultiplierSystem multiplier;
    private CameraEffects cameraEffects;
    private CharacterController controller;

    private bool canSlide = true;
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
    }

    private void Update()
    {
        if (!InputManager.instance)
            return;

        SlideInput();
        SlideMovement();
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
            float targetSpeed = movement.moveSpeed * multiplier.GetSpeedMultiplier();

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
            if (currentSpeed > movement.moveSpeed * 1.1f)
            {
                if (cameraEffects)
                {
                    float normalizedSpeed = Mathf.InverseLerp(movement.moveSpeed, slideSpeedBoost, currentSpeed);
                    cameraEffects.TriggerFOVBurst(normalizedSpeed * 0.5f); // Smaller continuous boost
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
            float normalizedSpeed = Mathf.InverseLerp(movement.moveSpeed, slideSpeedBoost, currentSpeed);
            cameraEffects.TriggerFOVBurst(normalizedSpeed);

        }
    }

    private void StopSlide()
    {
        if (!isSliding)
            return;

        isSliding = false;
        slideCollider.enabled = false;
        currentSpeed = 0f;
        StartCoroutine(SlideCooldown());
        damagedEnemies.Clear();
    }

    private IEnumerator SlideCooldown()
    {
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

    private void DealSlideDamage(EnemyHealth enemyHealth)
    {
        float totalDamage = slideAttackDamage * multiplier.GetDamageMultiplier();
        enemyHealth.TakeDamage(totalDamage);
        multiplier.AddSlideHit();
    }
}