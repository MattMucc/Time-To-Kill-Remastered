using System.Collections;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    private WeaponData currentWeapon;
    private bool isReloading = false;
    private bool canShoot = true;
    private float nextFireTime = 0f;

    [Header("References")]
    [SerializeField] private Transform gunTip;
    [SerializeField] private Animator weaponAnimator;
    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        if (!inventory)
            return;

        inventory.OnWeaponSwitched.AddListener(OnWeaponSwitched);
        if (inventory.currentWeapon)
            OnWeaponSwitched(inventory.currentWeapon);
    }

    private void Update()
    {
        if (!currentWeapon || !InputManager.instance || isReloading)
            return;

        AttackInput();
        ReloadInput();
    }

    private void AttackInput()
    {
        if (InputManager.instance.AttackHeld && Time.time >= nextFireTime)
            TryFireWeapon();
    }

    private void ReloadInput()
    {
        if (InputManager.instance.ReloadPressed && !currentWeapon.isMelee && currentWeapon.currentAmmo < currentWeapon.maxAmmo)
            StartCoroutine(Reload());
    }

    private void TryFireWeapon()
    {
        if (currentWeapon.isMelee)
        {
            SwingMeleeWeapon();
            return;
        }

        if (currentWeapon.currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        currentWeapon.currentAmmo--;
        nextFireTime = Time.time + currentWeapon.rateOfFire;
        FireProjectile();
        if (weaponAnimator)
            weaponAnimator.SetTrigger("Fire");

        if (UIManager.instance)
            UIManager.instance?.RegisterPlayerInventory(inventory);
    }

    private void FireProjectile()
    {
        if (!currentWeapon.projectilePrefab || !gunTip)
            return;

        Camera cam = Camera.main;
        if (!cam)
            return;

        Ray ray = new Ray(cam.transform.position + cam.transform.forward * 0.1f, cam.transform.forward);
        Vector3 targetPos;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            targetPos = hit.point;
        else
            targetPos = cam.transform.position + cam.transform.forward * 1000f;

        Vector3 direction = (targetPos - gunTip.position).normalized;
        GameObject projectile = Instantiate(currentWeapon.projectilePrefab, gunTip.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<Projectile>().damage = currentWeapon.damage;
        if (currentWeapon.muzzleFlash)
            Instantiate(currentWeapon.muzzleFlash, gunTip.position, Quaternion.LookRotation(direction));
    }

    private void SwingMeleeWeapon()
    {
        if (!canShoot)
            return;

        canShoot = false;
        if (weaponAnimator)
            weaponAnimator.SetTrigger("Swing");

        StartCoroutine(MeleeCooldown());
    }

    private IEnumerator MeleeCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(currentWeapon.rateOfFire);
        canShoot = true;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        if (weaponAnimator)
            weaponAnimator.SetTrigger("Reload");

        yield return new WaitForSeconds(currentWeapon.reloadTime);
        currentWeapon.currentAmmo = currentWeapon.maxAmmo;
        isReloading = false;
        if (UIManager.instance)
            UIManager.instance?.RegisterPlayerInventory(inventory);
    }

    private void OnWeaponSwitched(WeaponData weapon)
    {
        currentWeapon = weapon;
        isReloading = false;
        nextFireTime = 0f;
    }
}