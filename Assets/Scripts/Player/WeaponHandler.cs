using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WeaponHandler : MonoBehaviour
{
    [Header("ADS Settings")]
    public float adsSpeedLimitMultiplier = 1.1f; // Greater than base move speed
    public bool restrictADSAtHighSpeed = false;
    private float lastADSFOV = -1f;
    private bool lastAimState = false;

    [Header("Kickback Settings")]
    public float kickbackDistance = 0.05f;
    public float kickbackRotation = 5f; // Degress upwards
    public float kickbackReturnSpeed = 10f;
    public bool enableWeaponKickback = true;

    private Vector3 originalWeaponPos;
    private Quaternion originalWeaponRot;
    private Vector3 kickOffset;
    private Quaternion kickRot;

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
    private PlayerController movement;
    private PlayerInventory inventory;
    private MultiplierSystem multiplier;
    private CameraEffects cameraEffects;

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

        cameraEffects = mainCamera.GetComponent<CameraEffects>();
        originalWeaponPos = transform.localPosition;
        originalWeaponRot = transform.localRotation;

        UIManager.instance.RegisterWeaponHandler(this);
        inventory.OnWeaponSwitched.AddListener(OnWeaponSwitched);
        if (inventory.currentWeapon)
            OnWeaponSwitched(inventory.currentWeapon);

        movement = GetComponent<PlayerController>();
        multiplier = GetComponent<MultiplierSystem>();
    }

    private void Update()
    {
        if (!InputManager.instance || !UIManager.instance)
            return;

        ADSInput();
        AttackInput();
        ReloadInput();
        ThrowInput();
        UpdateWeaponKickback();
    }

    private void ADSInput()
    {
        if (!currentWeapon || !cameraEffects)
            return;

        bool wantAim = InputManager.instance.AimHeld && !isReloading;
        if (restrictADSAtHighSpeed)
        {
            float baseSpeed = movement.moveSpeed * multiplier.GetSpeedMultiplier();
            if (baseSpeed > movement.moveSpeed * adsSpeedLimitMultiplier)
                isAiming = false;
        }

        float target = wantAim ? currentWeapon.adsFOV : currentWeapon.hipFOV;
        if (wantAim != lastAimState || !Mathf.Approximately(target, lastADSFOV))
        {
            cameraEffects.SetADS(wantAim, target);
            lastAimState = wantAim;
            lastADSFOV = target;
        }

        isAiming = wantAim;
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

    private void UpdateWeaponKickback()
    {
        if (!enableWeaponKickback)
            return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalWeaponPos + kickOffset, kickbackReturnSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalWeaponRot * kickRot, kickbackReturnSpeed * Time.deltaTime);

        kickOffset = Vector3.Lerp(kickOffset, Vector3.zero, kickbackReturnSpeed * Time.deltaTime);
        kickRot = Quaternion.Slerp(kickRot, Quaternion.identity, kickbackReturnSpeed * Time.deltaTime);
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

        // Spawn muzzle flash
        if (currentWeapon.muzzleFlash)
            Instantiate(currentWeapon.muzzleFlash, gunTip.position, gunTip.rotation);

        // Apply camera recoil
        if (cameraEffects)
        {
            float vertical = currentWeapon.recoilVertical;
            float horizontal = currentWeapon.recoilHorizontal;
            cameraEffects.AddRecoil(vertical, horizontal);
        }

        // Apply weapon visual kickback
        if (enableWeaponKickback)
        {
            kickOffset = -transform.forward * kickbackDistance;
            kickRot = Quaternion.Euler(-kickbackRotation, 0f, 0f);
        }
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