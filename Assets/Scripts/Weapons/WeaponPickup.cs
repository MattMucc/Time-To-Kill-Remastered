using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup Info")]
    public WeaponData weaponData;
    public PersistenceID id;
    public bool canPickUp = true;

    [Header("Prompt Settings")]
    public float interactRange = 5f;

    private string keyName;
    private bool isPlayerInRange = false;
    private PlayerInventory playerInventory;
    private UIManager uiManager => UIManager.instance;

    private void Awake()
    {
        id = GetComponent<PersistenceID>();
    }

    private void Start()
    {
        SaveData data = SaveSystem.LoadGame();
        if (data != null && data.worldState.collectedPickups.Contains(id.ID))
        {
            Destroy(gameObject);
            return;
        }

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
        {
            message = $"Already Own {weaponData.weaponName}";
            canPickUp = false;
        }
        else if (playerInventory.ownedWeapons.Count >= playerInventory.maxWeaponSlots)
        {
            WeaponData currentWeapon = playerInventory.currentWeapon;
            message = $"Press [{keyName}] to replace [{currentWeapon.weaponName}] with [{weaponData.weaponName}]";
            canPickUp = true;
        }
        else
        {
            message = $"Press [{keyName}] to pick up {weaponData.weaponName}";
            canPickUp = true;
        }

        uiManager.ShowPrompt(message);
    }

    private void TryPickUp()
    {
        if (!playerInventory)
            return;

        if (!canPickUp)
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
        SaveData data = SaveSystem.LoadGame() ?? new SaveData();
        if (!data.worldState.collectedPickups.Contains(id.ID))
            data.worldState.collectedPickups.Add(id.ID);

        SaveSystem.SaveGame(data);
        Destroy(gameObject);
    }

    private void DropWeapon(WeaponData weapon)
    {
        if (!playerInventory || !weapon)
            return;

        GameObject dropPrefab = weapon.weaponPickupPrefab;
        if (!dropPrefab)
        {
            Debug.LogWarning($"No pickup prefab assigned for {weapon.weaponName}!");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRot = Quaternion.LookRotation(playerInventory.transform.forward, Vector3.up);
        GameObject droppedWeapon = Instantiate(dropPrefab, spawnPosition, spawnRot);
        WeaponPickup pickupComponent = droppedWeapon.GetComponent<WeaponPickup>();
        if (pickupComponent)
            pickupComponent.weaponData = weapon;

        Debug.Log($"Dropped {weapon.weaponName}");
    }
}