using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup Info")]
    public WeaponData weaponData;

    [Header("Prompt Settings")]
    public float interactRange = 5f;

    private string keyName;
    private bool isPlayerInRange = false;
    private PlayerInventory playerInventory;
    private UIManager uiManager => UIManager.instance;

    private void Start()
    {
        keyName = InputManager.instance.GetBindingDisplay("Player/Interact");
        if (GetComponent<Collider>())
            GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (!isPlayerInRange || !playerInventory)
            return;

        if (InputManager.instance.controls.Player.Interact.WasPerformedThisFrame())
            TryPickUp();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInventory = other.GetComponent<PlayerInventory>();
        if (!playerInventory)
            return;

        isPlayerInRange = true;
        UpdatePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        playerInventory = null;
        uiManager.HidePrompt();
    }

    private void UpdatePrompt()
    {
        if (!playerInventory || !uiManager)
            return;

        string message;
        if (playerInventory.ownedWeapons.Exists(w => w.weaponData == weaponData))
            message = $"Already Own {weaponData.weaponName}";
        else if (playerInventory.ownedWeapons.Count >= playerInventory.maxWeaponSlots)
        {
            WeaponData currentWeapon = playerInventory.currentWeapon;
            message = $"Press [{keyName}] to replace [{currentWeapon.weaponName}] with [{weaponData.weaponName}]";
        }
        else
            message = $"Press [{keyName}] to pick up {weaponData.weaponName}";

        uiManager.ShowPrompt(message);
    }

    private void TryPickUp()
    {
        if (!playerInventory)
            return;

        if (playerInventory.ownedWeapons.Count >= playerInventory.maxWeaponSlots)
        {
            WeaponData currentWeapon = playerInventory.currentWeapon;
            if (!currentWeapon)
                return;

            DropWeapon(currentWeapon);
            playerInventory.RemoveWeapon(currentWeapon);
        }

        playerInventory.AddWeapon(weaponData);
        uiManager.HidePrompt();
        Destroy(gameObject);
    }

    private void DropWeapon(WeaponData weapon)
    {
        if (!playerInventory)
            return;

        Debug.Log($"Dropped {weapon.weaponName}");
    }
}