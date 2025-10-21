using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager instance;

    [Header("Global Debug Settings")]
    public bool showEnemyHitboxes = false;
    public bool showEnemyRanges = false;
    public bool showPlayerCasts = true;
    public Color detectionRangeColor = Color.yellow;
    public Color attackRangeColor = Color.red;
    public Color playerCastColor = Color.cyan;
    private List<SphereCastDebugData> sphereCasts = new();

    private class SphereCastDebugData
    {
        public Vector3 origin;
        public Vector3 direction;
        public float distance;
        public float radius;
        public float lifetime;
        public Color color;
        public SphereCastDebugData(Vector3 origin, Vector3 direction, float distance, float radius, float lifetime, Color color)
        {
            this.origin = origin;
            this.direction = direction;
            this.distance = distance;
            this.radius = radius;
            this.lifetime = lifetime;
            this.color = color;
        }
    }

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (InputManager.instance.DebugPressed)
        {
            showEnemyHitboxes = !showEnemyHitboxes;
            showEnemyRanges = !showEnemyRanges;
            showPlayerCasts = !showPlayerCasts;
            Debug.Log($"[DEBUG] Enemy hurtbox visualization: {showEnemyHitboxes}");
            Debug.Log($"[DEBUG] Enemy ranges visualization: {showEnemyRanges}");
            Debug.Log($"[DEBUG] Player casts visualization: {showPlayerCasts}");
        }

        for (int i = sphereCasts.Count - 1; i >= 0; i--)
        {
            sphereCasts[i].lifetime -= Time.deltaTime;
            if (sphereCasts[i].lifetime <= 0f)
                sphereCasts.RemoveAt(i);
        }
    }

    public void RegisterSphereCasts(Vector3 origin, Vector3 direction, float distance, float radius, float lifetime = 0.5f)
    {
        if (!showPlayerCasts)
            return;

        sphereCasts.Add(new SphereCastDebugData(origin, direction, distance, radius, lifetime, playerCastColor));
        Debug.DrawRay(origin, direction * distance, playerCastColor, lifetime);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (showEnemyRanges)
        {
            foreach (BaseEnemy enemy in Object.FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None))
            {
                Gizmos.color = detectionRangeColor;
                Gizmos.DrawWireSphere(enemy.transform.position, enemy.detectionRange);

                Gizmos.color = attackRangeColor;
                Gizmos.DrawWireSphere(enemy.transform.position, enemy.attackRange);
            }
        }

        if (showPlayerCasts)
        {
            Gizmos.color = playerCastColor;
            foreach (SphereCastDebugData cast in sphereCasts)
            {
                Gizmos.DrawLine(cast.origin, cast.origin + cast.direction * cast.distance);
                Gizmos.DrawWireSphere(cast.origin, cast.radius);
                Gizmos.DrawWireSphere(cast.origin + cast.direction * cast.distance, cast.radius);
            }
        }
    }
}