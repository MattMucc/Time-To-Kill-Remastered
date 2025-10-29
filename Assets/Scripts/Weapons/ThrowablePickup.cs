using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowablePickup : MonoBehaviour
{
    [Header("Pickup Info")]
    public WeaponData weaponData;
    public PersistenceID id;
    public int amount = 1;
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
        WeaponData currentThrowable = playerInventory.ownedThrowables.Count > 0 ? playerInventory.ownedThrowables[0].throwableData : null;
        if (!currentThrowable)
        {
            message = $"Press [{keyName}] to pick up {weaponData.weaponName}";
            canPickUp = true;
        }
        else if (currentThrowable == weaponData)
        {
            int currentCount = playerInventory.ownedThrowables[0].quantity;
            int maxCarry = playerInventory.maxThrowableAmount;
            if (currentCount >= maxCarry)
            {
                message = "Stack is full!";
                canPickUp = false;
            }
            else
            {
                int canCarry = Mathf.Max(0, maxCarry - currentCount);
                int willTake = Mathf.Min(canCarry, amount);
                int remainder = amount - willTake;
                message = remainder > 0 ? $"Press [{keyName}] to pick up {willTake} {weaponData.weaponName}(s) ({remainder} remaining)" : $"Press [{keyName}] to pick up {willTake} {weaponData.weaponName}(s)";
                canPickUp = true;
            }
        }
        else
        {
            message = $"Press [{keyName}] to replace [{currentThrowable.weaponName} with {weaponData.weaponName}]";
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

        if (playerInventory.ownedThrowables.Count == 0)
        {
            playerInventory.AddThrowable(weaponData, amount);
            uiManager.HidePrompt();
            Destroy(gameObject);
            return;
        }

        ThrowableSlot slot = playerInventory.ownedThrowables[0];
        WeaponData currentThrowable = slot.throwableData;
        if (currentThrowable == weaponData)
        {
            int currentCapacity = slot.quantity;
            int maxCarry = playerInventory.maxThrowableAmount;
            int canCarry = Mathf.Max(0, maxCarry - currentCapacity);
            if (canCarry <= 0)
            {
                //Already full
                return;
            }

            int amountTaken = Mathf.Min(canCarry, amount);
            slot.quantity += amountTaken;
            playerInventory.OnThrowablePickUp?.Invoke(weaponData);
            amount -= amountTaken;
            if (amount > 0)
            {
                uiManager.HidePrompt();
                return;
            }

            uiManager.HidePrompt();
            Destroy(gameObject);
            return;
        }

        DropThrowable(currentThrowable);
        playerInventory.ownedThrowables.Clear();
        playerInventory.AddThrowable(weaponData, amount);
        uiManager.HidePrompt();
        SaveData data = SaveSystem.LoadGame() ?? new SaveData();
        if (!data.worldState.collectedPickups.Contains(id.ID))
            data.worldState.collectedPickups.Add(id.ID);

        SaveSystem.SaveGame(data);
        Destroy(gameObject);
    }

    private void DropThrowable(WeaponData throwable)
    {
        if (!playerInventory || !throwable)
            return;

        GameObject dropPrefab = throwable.weaponPickupPrefab;
        if (!dropPrefab)
        {
            Debug.LogWarning($"No pickup prefab assigned for {throwable.weaponName}!");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRot = Quaternion.LookRotation(playerInventory.transform.forward, Vector3.up);
        GameObject droppedWeapon = Instantiate(dropPrefab, spawnPosition, spawnRot);
        WeaponPickup pickupComponent = droppedWeapon.GetComponent<WeaponPickup>();
        if (pickupComponent)
            pickupComponent.weaponData = throwable;

        Debug.Log($"Dropped {throwable.weaponName}");
    }
}