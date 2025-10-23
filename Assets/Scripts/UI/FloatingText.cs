using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [Header("Text Settings")]
    public float lifetime;
    public float speed;
    public Color textColor;
    private TMP_Text text;
    private FloatingTextManager textManager;
    private Camera mainCam;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam)
            transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
    }

    public void Initialize(float damage, HitmarkerManager.HitType hitType, FloatingTextManager floatingTextManager)
    {
        textManager = floatingTextManager;
        lifetime = textManager.lifetime;
        speed = textManager.floatSpeed;
        switch(hitType)
        {
            case HitmarkerManager.HitType.Headshot:
                textColor = textManager.headshotColor;
                break;
            case HitmarkerManager.HitType.Shield:
                textColor = textManager.shieldColor;
                break;
            case HitmarkerManager.HitType.Kill:
                textColor = textManager.killColor;
                break;
            default:
                textColor = textManager.normalColor;
                break;
        }

        text.color = textColor;
        text.text = damage.ToString("0.##");
        text.fontMaterial.renderQueue = 4000; // Ensure text renders on top
        StopAllCoroutines();
        StartCoroutine(FadeAndRise());
    }

    private IEnumerator FadeAndRise()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Color startColor = text.color;
        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            transform.position = startPos + Vector3.up * speed * timer;

            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        textManager.ReturnToPool(this);
    }
}