using UnityEngine;

public class WeaponRecoilHandler : MonoBehaviour
{
    [Header("Recoil Settings")]
    [Tooltip("If true, kicks back the weapon's position along with the recoil.")]
    public bool enablePositionKickback = false;
    public float kickbackDistance = 0.05f;
    public float kickbackRecoverySpeed = 10f;

    [Tooltip("If true, slightly kicks the camera with recoil.")]
    public bool enableCameraRecoil = false;
    [Range(0f, 1f)] public float cameraKickPercent = 0.3f;

    private float recoilX;
    private float recoilY;

    private float recoilResetVelocityX;
    private float recoilResetVelocityY;

    private float targetFOV;
    private float currentFOV;

    private Vector3 originalLocalPos;
    private Vector3 currentKickOffset;

    private bool isADS = false;

    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    private WeaponData currentWeapon;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
            return;

        currentFOV = targetFOV = mainCamera.fieldOfView;
        originalLocalPos = transform.localPosition;
    }

    private void Update()
    {
        UpdateRecoil();
        UpdateCameraFOV();
        UpdateKickback();
    }

    public void SetADS(bool aiming, WeaponData weapon)
    {
        if (!weapon)
            return;

        isADS = aiming;
        currentWeapon = weapon;
        targetFOV = aiming ? weapon.adsFOV : weapon.hipFOV;
    }

    public void ApplyRecoil(WeaponData weapon)
    {
        if (!weapon)
            return;

        float vertical = weapon.recoilVertical;
        float horizontal = Random.Range(-weapon.recoilHorizontal, weapon.recoilHorizontal);
        recoilX += vertical;
        recoilY += horizontal;

        if (enablePositionKickback)
            currentKickOffset = -transform.forward * kickbackDistance;
    }

    private void UpdateRecoil()
    {
        if (!currentWeapon || !mainCamera)
            return;

        recoilX = Mathf.SmoothDamp(recoilX, 0f, ref recoilResetVelocityX, 1f / currentWeapon.recoilRecoveryRate);
        recoilY = Mathf.SmoothDamp(recoilY, 0f, ref recoilResetVelocityY, 1f / currentWeapon.recoilRecoveryRate);

        // Rotate the weapon holder (not the camera)
        if (enableCameraRecoil && cameraRoot)
            cameraRoot.transform.localRotation = Quaternion.Euler(-recoilX * cameraKickPercent, recoilY * cameraKickPercent, 0f); // Kicks the camera by a percentage of the recoil
    }

    private void UpdateCameraFOV()
    {
        if (!mainCamera || !currentWeapon)
            return;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime / currentWeapon.adsTime);
    }

    private void UpdateKickback()
    {
        if (!enablePositionKickback)
            return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPos + currentKickOffset, Time.deltaTime * kickbackRecoverySpeed);
        currentKickOffset = Vector3.Lerp(currentKickOffset, Vector3.zero, Time.deltaTime * kickbackRecoverySpeed);
    }
}