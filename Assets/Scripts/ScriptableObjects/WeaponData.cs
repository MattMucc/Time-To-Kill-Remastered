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

    [Tooltip("If true, this weapon fires a projectile. If false, this weapon uses raycasts (hitscan).")]
    public bool useProjectile = false;

    [Header("Models")]
    [Tooltip("Used mainly to get the MeshFilter and MeshRenderer")]
    public GameObject model;
    public GameObject weaponPickupPrefab;
    public GameObject projectilePrefab;
    public GameObject muzzleFlash;
    public GameObject hitEffect;

    [Header("Stats")]
    public float damage = 10f;
    public float rateOfFire = 0.2f;
    public float reloadTime = 1f;
    public float adsTime = 0.2f;
    public float raycastRange = 100f;

    [Tooltip("Number of rays to shoot for shotgun-style weapons (default = 1).")]
    public int pelletCount = 1;

    [Tooltip("0 = unlimited (for melee)")]
    public float maxAmmo = 10f;
    public float currentAmmo = 10f; // For firearms

    [Header("Spread")]
    public float hipfireSpread = 2f;
    public float adsSpread = 0.5f;

    [Header("Recoil")]
    public float recoilVertical = 2f;
    public float recoilHorizontal = 1f;

    [Tooltip("A multiplier applied to the recoil while in ADS (lower = less recoil in ADS)")]
    public float adsRecoilVerticalMultiplier = 0.6f;

    [Tooltip("A multiplier applied to the recoil while in ADS (lower = less recoil in ADS)")]
    public float adsRecoilHorizontalMultiplier = 0.6f;

    [Tooltip("Force applied to the player when firing this weapon (0 = no force applied).")]
    public float recoilForce = 0f;

    [Tooltip("If true, recoil force will also apply upward force, useful for rocket jumping.")]
    public bool allowVerticalRecoilForce = false;

    [Header("FOV")]
    public float adsFOV = 40f;

    [Tooltip("Normal FOV (default = 60)")]
    public float hipFOV = 60f;
}