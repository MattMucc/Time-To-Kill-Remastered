using UnityEngine;
using UnityEngine.Events;

public class MultiplierSystem : MonoBehaviour
{
    [Header("General Settings")]
    [Range(1, 5)] public int maxMultiplier = 3;
    public float baseDecayDelay = 3f;
    public float baseDecayRate = 0.25f;

    [Tooltip("If true, the multiplier drops by 1 each time the meter is fully drained instead of resetting to 1 (easy mode pretty much)")]
    public bool gradualDecay = false;

    [Tooltip("If true, decay delay and rate will scale based on your current multiplier level (hard mode pretty much).")]
    public bool adaptiveDecay = false;

    [Header("Meter Gains")]
    [Range(0f, 1f)] public float fillPerKill = 0.25f;
    [Range(0f, 1f)] public float fillPerDamage = 0.01f;
    [Range(0f, 1f)] public float fillPerHeadshot = 0.05f;
    [Range(0f, 1f)] public float fillPerSlideHit = 0.1f;
    [Range(0f, 1f)] public float fillPerGrappleHit = 0.1f;

    [Header("Scaling Values (Adaptive Mode)")]
    [Tooltip("Decay delay multiplier applied per current multiplier level (higher multiplier = faster decay).")]
    public AnimationCurve decayDelayCurve = AnimationCurve.Linear(1, 1f, 3, 0.5f);

    [Tooltip("Decay rate multiplier applied per current multiplier level.)")]
    public AnimationCurve decayRateCurve = AnimationCurve.Linear(1, 1f, 3, 0.5f);

    [Tooltip("Dampens the effect of the multiplier affecting speed (higher = more affected by multiplier)")]
    [Range(0f, 1f)] public float speedDampenerMultiplier = 0.25f;

    [Tooltip("Stops reload time from exceeding past x% when affected by the multiplier (e.g. 0.5 = 50% reload time)")]
    [Range(0f, 1f)] public float reloadTimeLimiter = 0.5f;

    [Header("States (Read-Only)")]
    [Range(0f, 1f)] public float meter = 0f;
    [Range(1, 5)] public int multiplier = 1;
    [SerializeField] private bool isDecayPaused = false;
    private float decayTimer = 0f;

    [Header("Events")]
    public UnityEvent<int> OnMultiplierChanged;
    public UnityEvent<float> OnMeterChanged;
    public UnityEvent<bool> OnDecayPauseChanged;

    private void Start()
    {
        if (UIManager.instance)
            UIManager.instance.RegisterMultiplierSystem(this);
    }

    private void Update()
    {
        Decay();
    }

    public void PauseDecay()
    {
        isDecayPaused = true;
        OnDecayPauseChanged?.Invoke(isDecayPaused);
    }

    public void ResumeDecay()
    {
        isDecayPaused = false;
        decayTimer = GetCurrentDecayDelay();
        OnDecayPauseChanged?.Invoke(isDecayPaused);
    }

    private void Decay()
    {
        if (isDecayPaused)
            return;

        if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
            return;
        }

        float decayRate = baseDecayRate;
        if (adaptiveDecay)
            decayRate *= decayRateCurve.Evaluate(multiplier);

        if (meter > 0f)
        {
            meter -= decayRate * Time.deltaTime;
            if (meter <= 0f)
            {
                meter = 0f;
                if (gradualDecay && multiplier > 1)
                {
                    multiplier--;
                    meter = 1f;
                    OnMultiplierChanged?.Invoke(multiplier);
                }
                else if (!gradualDecay && multiplier > 1)
                {
                    multiplier = 1;
                    OnMultiplierChanged?.Invoke(multiplier);
                }
            }

            OnMeterChanged?.Invoke(meter);
        }
    }

    private void AddToMeter(float amount)
    {
        if (amount <= 0)
            return;

        decayTimer = GetCurrentDecayDelay();
        meter += amount;
        if (meter >= 1)
        {
            float overflow = meter - 1f;
            meter = overflow;
            if (multiplier < maxMultiplier)
            {
                multiplier++;
                OnMultiplierChanged?.Invoke(multiplier);
            }
        }

        OnMeterChanged?.Invoke(meter);
    }

    public void AddKill()
    {
        AddToMeter(fillPerKill);
    }

    public void AddDamage(float damageAmount)
    {
        if (damageAmount <= 0f)
            return;

        // Scales damage contribution to slightly reward heavier hits
        float scaled = Mathf.Clamp(damageAmount * fillPerDamage, 0.005f, 0.05f);
        AddToMeter(scaled);
    }

    public void AddHeadshot()
    {
        AddToMeter(fillPerHeadshot);
    }

    public void AddSlideHit()
    {
        AddToMeter(fillPerSlideHit);
    }

    public void AddGrappleHit()
    {
        AddToMeter(fillPerGrappleHit);
    }

    private float GetCurrentDecayDelay()
    {
        if (!adaptiveDecay)
            return baseDecayDelay;

        return baseDecayDelay * decayDelayCurve.Evaluate(multiplier);
    }

    public float GetDamageMultiplier() => 1f + 0.25f * (multiplier - 1); // 1f + 0.25 * (2 - 1) = 1.25 -> damage (if 7) * 1.25 = 8.75
    public float GetSpeedMultiplier() => 1f + speedDampenerMultiplier * (multiplier - 1); // 1f + 0.25 * (2 - 1) = 1.25 -> movement speed (if 8) * 1.25 = 10
    public float GetReloadSpeedMultiplier()
    {
        float baseMultiplier = 1f / GetSpeedMultiplier();
        return Mathf.Max(baseMultiplier, reloadTimeLimiter); // Never exceeds x% of base reload speed when affected by the multiplier
    }
}