using System.Collections.Generic;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;

    [Header("Floating Text Settings")]
    public GameObject floatingTextPrefab;
    public int poolSize = 10;
    public float floatSpeed = 1.5f;
    public float lifetime = 1f;
    public Color normalColor = Color.white;
    public Color headshotColor = Color.yellow;
    public Color shieldColor = Color.cyan;
    public Color killColor = Color.red;

    private Queue<FloatingText> pool = new();

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        CreatePool();
    }

    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(floatingTextPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj.GetComponent<FloatingText>());
        }
    }

    public void SpawnText(Vector3 worldPos, float damage, HitmarkerManager.HitType type)
    {
        if (pool.Count == 0)
            CreatePool();

        FloatingText text = pool.Dequeue();
        text.gameObject.SetActive(true);
        text.transform.position = worldPos;
        text.Initialize(damage, type, this);
    }

    public void ReturnToPool(FloatingText text)
    {
        text.gameObject.SetActive(false);
        pool.Enqueue(text);
    }
}