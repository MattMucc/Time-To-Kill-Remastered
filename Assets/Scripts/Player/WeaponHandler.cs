using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WeaponHandler : MonoBehaviour
{
    private WeaponData currentWeapon;
    private bool isAiming = false;
    private bool isReloading = false;
    private bool canShoot = true;
    private float nextFireTime = 0f;
    private float reloadTimer = 0f;

    [Header("References")]
    [SerializeField] private Transform gunTip;
    [SerializeField] private Animator weaponAnimator;
    private Camera mainCamera;
    private PlayerInventory inventory;
    private WeaponRecoilHandler recoilHandler;

    [Header("Events")]
    public UnityEvent OnReloadStart;
    public UnityEvent<float> OnReloadProgress;
    public UnityEvent OnReloadComplete;

    private void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        if (!inventory)
            return;

        mainCamera = Camera.main;
        if (!mainCamera)
            return;

        recoilHandler = mainCamera.gameObject.transform.GetChild(0).GetComponent<WeaponRecoilHandler>();
        if (!recoilHandler)
            return;

        UIManager.instance.RegisterWeaponHandler(this);
        inventory.OnWeaponSwitched.AddListener(OnWeaponSwitched);
        if (inventory.currentWeapon)
            OnWeaponSwitched(inventory.currentWeapon);
    }

    private void Update()
    {
        if (!InputManager.instance || !UIManager.instance)
            return;

        ADSInput();
        AttackInput();
        ReloadInput();
        ThrowInput();
    }

    private void ADSInput()
    {
        if (!currentWeapon || !recoilHandler)
            return;

        isAiming = InputManager.instance.AimHeld && !isReloading;
        recoilHandler.SetADS(isAiming, currentWeapon);
    }

    private void AttackInput()
    {
        if (!currentWeapon)
            return;

        if (InputManager.instance.AttackHeld && Time.time >= nextFireTime && currentWeapon.currentAmmo > 0f && !isReloading)
            TryFireWeapon();
        else if (InputManager.instance.AttackHeld && currentWeapon.currentAmmo <= 0f && !isReloading)
            ReloadStart();
    }

    private void ReloadInput()
    {
        if (!currentWeapon)
            return;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            float progress = 1f - (reloadTimer / currentWeapon.reloadTime);
            OnReloadProgress?.Invoke(progress);
            if (reloadTimer <= 0f)
                ReloadFinish();
        }

        if (InputManager.instance.ReloadPressed && !isReloading)
            ReloadStart();
    }

    private void ThrowInput()
    {
        if (!InputManager.instance.ThrowPressed || InputManager.instance.AttackHeld)
            return;

        if (inventory.ownedThrowables.Count <= 0)
        {
            Debug.LogWarning("You have no throwables to throw!");
            return;
        }

        ThrowableSlot slot = inventory.ownedThrowables[0];
        if (slot.quantity <= 0)
        {
            Debug.LogWarning("You have no throwables to throw!");
            return;
        }

        WeaponData throwable = slot.throwableData;
        if (!throwable)
        {
            Debug.LogWarning("Couldn't find WeaponData in ThrowableSlot!");
            return;
        }

        inventory.UseThrowable(throwable);
        Throw(throwable);
        UIManager.instance.RegisterPlayerInventory(inventory);
    }

    private void TryFireWeapon()
    {
        if (currentWeapon.isMelee)
        {
            SwingMeleeWeapon();
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

        // Multiple pellets for shotgun-style spread
        for (int i = 0; i < Mathf.Min(1, currentWeapon.pelletCount); i++)
        {
            Vector3 spreadDir = GetSpreadDirection();
            if (currentWeapon.useProjectile)
            {
                GameObject projectile = Instantiate(currentWeapon.projectilePrefab, gunTip.position, Quaternion.LookRotation(spreadDir));
                projectile.GetComponent<Projectile>().damage = currentWeapon.damage * GetComponent<MultiplierSystem>().GetDamageMultiplier();
            }
            else
            {
                if (Physics.Raycast(mainCamera.transform.position, spreadDir, out RaycastHit hitInfo, currentWeapon.raycastRange))
                {
                    if (hitInfo.collider.TryGetComponent(out EnemyHealth enemyHealth))
                    enemyHealth.TakeDamage(currentWeapon.damage * GetComponent<MultiplierSystem>().GetDamageMultiplier());

                    if (currentWeapon.hitEffect)
                        Instantiate(currentWeapon.hitEffect, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
                }
            }
        }

        // Finding where the player is aiming
        /*Ray ray = new Ray(mainCamera.transform.position + mainCamera.transform.forward * 0.1f, mainCamera.transform.forward);
        Vector3 targetPos;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            targetPos = hit.point;
        else
            targetPos = mainCamera.transform.position + mainCamera.transform.forward * 1000f;

        Vector3 direction = (targetPos - gunTip.position).normalized;
        if (currentWeapon.useProjectile)
        {
            GameObject projectile = Instantiate(currentWeapon.projectilePrefab, gunTip.position, Quaternion.LookRotation(direction));
            projectile.GetComponent<Projectile>().damage = currentWeapon.damage * GetComponent<MultiplierSystem>().GetDamageMultiplier();
        }
        else
        {
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hitInfo, currentWeapon.raycastRange))
            {
                if (hitInfo.collider.TryGetComponent(out EnemyHealth enemyHealth))
                    enemyHealth.TakeDamage(currentWeapon.damage * GetComponent<MultiplierSystem>().GetDamageMultiplier());

                // Spawn impact/hit effect
                if (currentWeapon.hitEffect)
                    Instantiate(currentWeapon.hitEffect, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }
        }*/

        // Spawn muzzle flash
        if (currentWeapon.muzzleFlash)
            Instantiate(currentWeapon.muzzleFlash, gunTip.position, gunTip.rotation);

        // Apply recoil
        if (recoilHandler)
            recoilHandler.ApplyRecoil(currentWeapon);
    }

    private Vector3 GetSpreadDirection()
    {
        float spreadAngle = isAiming ? currentWeapon.adsSpread : currentWeapon.hipfireSpread;
        Vector3 spread = Vector3.zero;
        spread += mainCamera.transform.up * Random.Range(-spreadAngle, spreadAngle);
        spread += mainCamera.transform.right * Random.Range(-spreadAngle, spreadAngle);

        Vector3 direction = (mainCamera.transform.forward + spread * 0.01f).normalized;
        return direction;
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

    private void ReloadStart()
    {
        if (!currentWeapon || isReloading)
            return;

        isReloading = true;
        reloadTimer = currentWeapon.reloadTime * GetComponent<MultiplierSystem>().GetReloadSpeedMultiplier();
        OnReloadStart?.Invoke();
    }

    private void ReloadFinish()
    {
        isReloading = false;
        currentWeapon.currentAmmo = currentWeapon.maxAmmo;
        OnReloadComplete?.Invoke();
    }

    private void Throw(WeaponData throwable)
    {
        if (!throwable.projectilePrefab)
        {
            Debug.LogWarning("Set throwable prefab in ProjectilePrefab field!");
            return;
        }

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
        GameObject grenade = Instantiate(throwable.projectilePrefab, gunTip.position, Quaternion.LookRotation(direction));
        grenade.GetComponent<Throwable>().damage = throwable.damage;
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (!rb)
            return;

        rb.AddForce(direction * 15f, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
    }

    private IEnumerator MeleeCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(currentWeapon.rateOfFire);
        canShoot = true;
    }

    private void OnWeaponSwitched(WeaponData weapon)
    {
        currentWeapon = weapon;
        isReloading = false;
        nextFireTime = 0f;
        OnReloadComplete?.Invoke(); // Hides reload UI and updates weapon UI
    }
}