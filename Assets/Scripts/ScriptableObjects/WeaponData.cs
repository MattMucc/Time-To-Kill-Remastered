using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General Info")]
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject weaponPickupPrefab;
    public GameObject weaponPrefab;
    public bool isMelee;
    public bool isThrowable;

    [Header("Stats")]
    public float damage = 10f;
    public float rateOfFire = 0.2f;
    public float maxAmmo = 10f; // 0 = unlimited (for melee)
    public float currentAmmo = 10f; // For firearms
}