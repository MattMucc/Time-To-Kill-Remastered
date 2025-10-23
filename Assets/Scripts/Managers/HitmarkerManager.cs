using UnityEngine;
using UnityEngine.UI;

public class HitmarkerManager : MonoBehaviour
{
    public static HitmarkerManager instance;

    public enum HitType { Normal, Headshot, Shield, Kill };

    [Header("Hitmarker Settings")]
    public Image hitmarker;
    public float fadeSpeed = 4f;
    public Color normalColor = Color.white;
    public Color headshotColor = Color.yellow;
    public Color shieldColor = Color.cyan;
    public Color killColor = Color.red;
    private float alpha = 0f;
    private Color currentColor;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        currentColor = normalColor;
    }

    private void Update()
    {
        if (!hitmarker)
            return;

        alpha = Mathf.MoveTowards(alpha, 0f, fadeSpeed * Time.deltaTime);
        hitmarker.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }

    public void ShowHitmarker(HitType type)
    {
        switch (type)
        {
            case HitType.Headshot:
                currentColor = headshotColor;
                AudioManager.instance.PlayHeadshotHit();
                break;
            case HitType.Shield:
                currentColor = shieldColor;
                AudioManager.instance.PlayShieldHit();
                break;
            case HitType.Kill:
                currentColor = killColor;
                AudioManager.instance.PlayKillHit();
                break;
            default:
                currentColor = normalColor;
                AudioManager.instance.PlayNormalHit();
                break;
        }

        alpha = 1f;
        hitmarker.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }
}