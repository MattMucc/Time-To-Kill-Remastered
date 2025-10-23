using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    private bool isDead = false;

    [Header("Shield Settings")]
    public float maxShield = 0f;
    public float currentShield = 0f;
    public bool usesShield = false;

    [Tooltip("If true, overflow damage from a single hit does NOT affect health if the shield breaks.")]
    public bool enableShieldGating = false;

    [Tooltip("Optional passive shield regeneration (e.g. for bosses).")]
    public float shieldRegenRate = 0f;
    public float shieldRegenDelay = 3f;
    private float shieldRegenTimer;

    [Header("Events")]
    public UnityEvent OnShieldBroken;
    public UnityEvent OnEnemyDeath;
    public UnityEvent<float, float> OnHealthChanged; // Current, Max
    public UnityEvent<float, float> OnShieldChanged; // Current, Max

    [Header("References")]
    [SerializeField] private EnemyUI ui;
    private GameObject player;

    private void Start()
    {
        ui = GetComponentInChildren<EnemyUI>();
        if (ui)
            ui.Register(this);

        currentHealth = maxHealth;
        currentShield = maxShield;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnShieldChanged?.Invoke(currentShield, maxShield);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (isDead)
            return;

        HandleShieldRegen();
    }

    private void HandleShieldRegen()
    {
        if (!usesShield || shieldRegenRate <= 0f)
            return;

        shieldRegenTimer += Time.deltaTime;
        if (shieldRegenTimer >= shieldRegenDelay && currentShield < maxShield)
        {
            currentShield = Mathf.Min(maxShield, currentShield + shieldRegenRate * Time.deltaTime);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }
    }

    public void RestoreShield(float amount, bool resetDelay = false)
    {
        if (!usesShield || amount <= 0f)
            return;

        currentShield = Mathf.Min(maxShield, currentShield + amount);
        if (resetDelay)
            shieldRegenTimer = 0f;

        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    public void RefillShield(bool resetDelay = false)
    {
        if (!usesShield)
            return;

        currentShield = maxShield;
        if (resetDelay)
            shieldRegenTimer = 0f;

        OnShieldChanged?.Invoke(currentShield, maxShield);
    }

    public void TakeDamage(float amount, EnemyHurtbox.HurtboxType hurtboxType, Vector3 hitPoint)
    {
        if (isDead || amount <= 0f)
            return;

        shieldRegenTimer = 0f;
        float remainingDamage = amount;
        bool damagedShield = false;
        if (usesShield && currentShield > 0f)
        {
            damagedShield = true;
            float absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
            HitmarkerManager.instance.ShowHitmarker(HitmarkerManager.HitType.Shield);
            FloatingTextManager.instance.SpawnText(hitPoint, absorbed, HitmarkerManager.HitType.Shield);
            OnShieldChanged?.Invoke(currentShield, maxShield);
            if (currentShield <= 0f)
            {
                OnShieldBroken?.Invoke();
                if (enableShieldGating)
                    remainingDamage = 0f;
            }
        }

        if (remainingDamage > 0f)
        {
            currentHealth -= remainingDamage;
            if (!damagedShield && currentHealth > 0f)
            {
                switch (hurtboxType)
                {
                    case EnemyHurtbox.HurtboxType.Head:
                        HitmarkerManager.instance.ShowHitmarker(HitmarkerManager.HitType.Headshot);
                        FloatingTextManager.instance.SpawnText(hitPoint, remainingDamage, HitmarkerManager.HitType.Headshot);
                        break;
                    default:
                        HitmarkerManager.instance.ShowHitmarker(HitmarkerManager.HitType.Normal);
                        FloatingTextManager.instance.SpawnText(hitPoint, remainingDamage, HitmarkerManager.HitType.Normal);
                        break;
                }
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
            Die(remainingDamage);
    }

    private void Die(float damage = 0f)
    {
        if (isDead)
            return;

        // Play death animation, drop loot?, disable AI
        isDead = true;
        player.GetComponent<MultiplierSystem>()?.AddKill();
        HitmarkerManager.instance.ShowHitmarker(HitmarkerManager.HitType.Kill);
        FloatingTextManager.instance.SpawnText(transform.position, damage, HitmarkerManager.HitType.Kill);
        OnEnemyDeath?.Invoke();
        Destroy(gameObject);
    }

    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public float ShieldPercent => maxShield > 0f ? currentShield / maxShield : 0f;
}