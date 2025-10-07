using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General Info")]
    public string weaponName;
    public Sprite weaponIcon;
    public bool isMelee;
    public bool isThrowable;

    [Header("Models")]
    public GameObject weaponPickupPrefab;
    public GameObject model;
    public GameObject projectilePrefab;
    public GameObject muzzleFlash;

    [Header("Stats")]
    public float damage = 10f;
    public float rateOfFire = 0.2f;
    public float reloadTime = 1f;
    [Tooltip("0 = unlimited (for melee)")] public float maxAmmo = 10f;
    public float currentAmmo = 10f; // For firearms
}