using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyHurtbox : MonoBehaviour
{
    public enum HurtboxType { Body, Head }

    [Header("Hurtbox Settings")]
    public HurtboxType type = HurtboxType.Body;
    public float damageMultiplier = 1f;

    [Header("References")]
    public EnemyHealth health;
    public Collider col;

    private void Start()
    {
        health = transform.root.GetComponent<EnemyHealth>();
        col = GetComponent<Collider>();
    }

    public void TakeDamage(float amount, out HurtboxType hitType)
    {
        hitType = type;
        if (!health)
            return;

        float finalDamage = amount * damageMultiplier;
        health.TakeDamage(finalDamage);
    }

    private void OnGUI()
    {
        if (!DebugManager.instance || !DebugManager.instance.showEnemyHitboxes)
            return;

        // Convert 3D world pos to screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            string label = type.ToString();
            Color color = (type == HurtboxType.Head) ? Color.yellow : Color.red;
            GUI.color = color;
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 100, 20), label);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (type == HurtboxType.Head) ? Color.red : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawWireCube(box.center, box.size);
    }
}