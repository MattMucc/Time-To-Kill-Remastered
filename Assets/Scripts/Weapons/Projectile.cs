using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"Hit {other.name} for {damage} damage");
            Destroy(gameObject);
        }
        else
            Destroy(gameObject);
    }
}