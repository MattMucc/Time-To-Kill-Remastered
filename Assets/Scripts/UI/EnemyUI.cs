using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [Header("References")]
    public GameObject shield;
    public Image healthbar;
    public Image shieldbar;
    public TMP_Text hpText;
    public TMP_Text spText;

    private EnemyHealth health;
    private Canvas canvas;
    private Camera cam;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        cam = Camera.main;
        canvas.worldCamera = cam;
    }

    private void LateUpdate()
    {
        if (!health || !cam)
            return;

        transform.LookAt(transform.position + cam.transform.forward);
    }

    public void Register(EnemyHealth enemyHealth)
    {
        // Unsubscribe from old events (just in case)
        if (health)
        {
            health.OnHealthChanged.RemoveListener(UpdateHealth);
            health.OnShieldChanged.RemoveListener(UpdateShield);
            health.OnEnemyDeath.RemoveListener(OnEnemyDeath);
        }

        // Subscribe to new health events
        health = enemyHealth;
        if (health)
        {
            health.OnHealthChanged.AddListener(UpdateHealth);
            health.OnShieldChanged.AddListener(UpdateShield);
            health.OnEnemyDeath.AddListener(OnEnemyDeath);

            // Initialize
            UpdateHealth(health.currentHealth, health.maxHealth);
            UpdateShield(health.currentShield, health.maxShield);
            if (!health.usesShield)
                shield.SetActive(false);
        }
    }

    private void UpdateHealth(float current, float max)
    {
        if (!healthbar || !hpText)
            return;

        float percent = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        healthbar.fillAmount = percent;
        hpText.text = $"{Mathf.RoundToInt(percent * 100)}";
    }

    private void UpdateShield(float current, float max)
    {
        if (!shieldbar || !spText)
            return;

        float percent = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        shieldbar.fillAmount = percent;
        spText.text = $"{Mathf.RoundToInt(percent * 100)}";
        if (shieldbar.fillAmount <= 0f)
            shield.SetActive(false);
    }

    private void OnEnemyDeath()
    {
        gameObject.SetActive(false);
    }
}