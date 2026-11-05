using UnityEngine;

[CreateAssetMenu(menuName = "Time To Kill/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    public string weaponId = "pistol";
    public string displayName = "Pistol";
    public Sprite hudIcon;
    [Min(1)] public int maxMagazine = 12;
    [Min(0)] public int maxReserve = 96;
    public bool discardMagazineOnReload = true;
    [Min(0.01f)] public float reloadSeconds = 1.2f;
    [Min(0.01f)] public float shotInterval = 0.18f;
    [Min(0f)] public float damage = 25f;
    [Min(1f)] public float range = 100f;
    public bool automatic;
    [Min(1f)] public float bulletVisualSpeed = 900f;
    [Range(10f, 90f)] public float aimFieldOfView = 45f;
    public Vector3 aimedWeaponPosition = new Vector3(0f, -0.16f, 0.5f);
    [Min(0.1f)] public float aimTransitionSpeed = 14f;
    public WeaponInstance Create(int magazine, int reserve) => new WeaponInstance(
        weaponId, maxMagazine, maxReserve, discardMagazineOnReload, magazine, reserve);
}
