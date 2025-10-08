using System.Collections;
using UnityEngine;

public class Throwable : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionDelay = 2.5f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public float damage = 50f;
    public GameObject explosionEfect;
    private bool hasExploded = false;

    private void Start()
    {
        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        if (explosionEfect)
            Instantiate(explosionEfect, transform.position, Quaternion.identity);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            // Check GetComponent<EnemyHealth>() to damage any enemies inside the blast radius
            /*EnemyHealth enemyHealth = collider.GetComponent<EnemyHealth>();
            if (enemyHealth)
                enemyHealth.TakeDamage(damage);*/
        }

        Destroy(gameObject);
    }
}