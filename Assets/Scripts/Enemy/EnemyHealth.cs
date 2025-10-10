using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public bool usesShield = false;

    public void TakeDamage(float amount)
    {

    }
}