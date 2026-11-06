using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [Tooltip("Zero leaves a defeated target inactive; positive values reset practice targets.")]
    [SerializeField, Min(0f)] private float resetDelay = 3f;
    public float Health { get; private set; }
    private Renderer[] visuals;
    private Collider[] colliders;
    private double resetAt;
    public void ResetHealth()
    {
        Health = maxHealth;
        SetVisible(true);
    }
    private void Awake()
    {
        Health = maxHealth;
        visuals = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }
    public bool ApplyDamage(float amount)
    {
        if (Health <= 0f || amount <= 0f) return false;
        Health = Mathf.Max(0f, Health - amount);
        if (Health == 0f)
        {
            SetVisible(false);
            resetAt = Time.timeAsDouble + resetDelay;
        }
        return true;
    }
    private void Update()
    {
        if (Health <= 0f && resetDelay > 0f && Time.timeAsDouble >= resetAt)
        { Health = maxHealth; SetVisible(true); }
    }
    private void SetVisible(bool value)
    {
        foreach (Renderer visual in visuals) if (visual) visual.enabled = value;
        foreach (Collider shape in colliders) if (shape) shape.enabled = value;
    }
}
