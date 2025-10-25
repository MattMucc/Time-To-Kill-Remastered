using System.Collections;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Attack Settings")]
    public float detectionRange = 100f;
    public float attackRange = 20f;
    public float damage = 10f;
    public float attackCooldown = 1.5f;
    protected float lastAttackTime = -1f;

    [Header("Gravity Settings")]
    public float gravity = -30f;
    public bool useGravity = true;
    private float verticalVelocity = 0f;

    [Header("States (Read-Only)")]
    public bool isAttacking = false;
    public bool isChasing = false;
    public bool playerDetected = false;
    public float distanceFromPlayer = -999f;

    [Header("References")]
    protected CharacterController controller;
    protected Animator anim;
    protected Transform player;

    protected virtual void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    protected virtual void Update()
    {
        if (!player)
            return;

        DetectPlayer();
        ApplyGravity();
    }

    protected virtual void ApplyGravity()
    {
        if (!useGravity || !controller)
            return;

        if (controller.isGrounded)
            verticalVelocity = -1f; // Small downward force to keep grounded
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    protected virtual void DetectPlayer()
    {
        distanceFromPlayer = Vector3.Distance(transform.position, player.position);
        playerDetected = distanceFromPlayer <= detectionRange;
        if (playerDetected)
            Think();
        else
            Idle();
    }

    /// <summary> Called when the enemy detects the player and is making decisions. </summary>
    protected virtual void Think()
    {
        if (distanceFromPlayer <= attackRange)
        {
            if (!isAttacking)
                TryAttack();
            else if (distanceFromPlayer > attackRange)
                isAttacking = false;
        }
        else
            Chase();
    }

    protected virtual void Idle()
    {
        //anim?.SetBool("IsMoving", false);
    }

    protected virtual void Chase()
    {
        if (isAttacking)
            return;

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        controller.Move(dir.normalized * baseSpeed * Time.deltaTime);
        //anim?.SetBool("IsMoving", true);
    }

    protected virtual void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        StartCoroutine(Attack());
    }

    protected abstract IEnumerator Attack();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}