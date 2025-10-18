using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isDead = false;

    [Header("Shield Settings")]
    public float maxShield = 100f;
    public float currentShield = 0f;
    public float shieldRegenRate = 10f;
    public float shieldRegenDelay = 3f;

    [Tooltip("If true, one big hit won't damage any health if there is any remaining damage after the shield breaks (easy mode pretty much).")]
    public bool enableShieldGating = false;

    [Header("Events")]
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnShieldBroken;
    public UnityEvent<float, float> OnHealthChanged; // Current, Max
    public UnityEvent<float, float> OnShieldChanged; // Current, Max

    private float shieldRegenTimer;

    private void Start()
    {
        currentHealth = maxHealth;
        OnShieldChanged?.Invoke(currentShield, maxShield);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (UIManager.instance)
            UIManager.instance.RegisterPlayerHealth(this);
    }

    private void Update()
    {
        if (isDead) return;

        RegenerateShield();
    }

    private void RegenerateShield()
    {
        shieldRegenTimer += Time.deltaTime;
        if (shieldRegenTimer >= shieldRegenDelay && currentShield < maxShield && shieldRegenRate > 0)
        {
            currentShield = Mathf.Min(maxShield, currentShield + shieldRegenRate * Time.deltaTime);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0)
            return;

        shieldRegenTimer = 0; // Reset regen timer
        float remaining = amount;
        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
            OnShieldChanged?.Invoke(currentShield, maxShield);
            if (currentShield <= 0f)
                OnShieldBroken?.Invoke();
        }

        if (remaining > 0f)
        {
            // Shield gating check
            if (enableShieldGating && currentShield <= 0f)
                remaining = 0f;

            currentHealth = Mathf.Min(0f, currentHealth - remaining);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0f)
                Die();
        }
    }

    /// <summary> Restore shield points immediately (e.g. pickup). Resets regen delay if you want. </summary>
    public void RestoreShield(float amount, bool resetRegenDelay = false)
    {
        if (isDead || amount <= 0f)
            return;

        currentShield = Mathf.Min(maxShield, currentShield + amount);
        if (resetRegenDelay)
            shieldRegenTimer = 0f;

        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    /// <summary> Fully refills the shield (e.g. checkpoint or powerup). </summary>
    public void RefillShield(bool resetRegenDelay = false)
    {
        currentShield = maxShield;
        if (resetRegenDelay)
            shieldRegenTimer = 0f;

        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    /// <summary> Sets a new max value for health points (e.g. upgrade). Refills current health if you want. </summary>
    public void SetMaxHealth(float newMax, bool refill = false)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (refill)
            currentHealth = maxHealth;

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary> Sets a new max value for shield points (e.g. upgrade). Refills current shield if you want. </summary>
    public void SetMaxShield(float newMax, bool refill = false)
    {
        maxShield = Mathf.Max(1f, newMax);
        if (refill)
            currentShield = maxShield;

        currentShield = Mathf.Min(currentShield, maxShield);
        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnPlayerDeath?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        InputManager.instance.DisablePlayerInput();
        InputManager.instance.EnableUIInput();
    }

    public float ShieldPercent => maxShield > 0f ? currentShield / maxShield : 0f;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
}