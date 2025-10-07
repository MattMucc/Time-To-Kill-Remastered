
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;
}

[System.Serializable]
public class ThrowableSlot
{
    public WeaponData throwableData;
    public int quantity;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    public List<WeaponSlot> ownedWeapons = new List<WeaponSlot>();
    public List<ThrowableSlot> ownedThrowables = new List<ThrowableSlot>();
    public WeaponData currentWeapon;
    public int currentWeaponSlot;
    private float nextSwitchTime = 0f;

    [Header("Inventory Settings")]
    public float weaponSwitchCooldown = 0.25f;
    public int maxWeaponSlots = 5;
    public int maxThrowableSlots = 1;
    public int maxThrowableAmount = 5;

    [Header("References")]
    public GameObject weaponModel;
    public Transform gunTip;

    [Header("Events")]
    public UnityEvent<WeaponData> OnWeaponPickUp;
    public UnityEvent<WeaponData> OnWeaponSwitched;
    public UnityEvent<WeaponData> OnWeaponDropped;
    public UnityEvent<WeaponData> OnThrowablePickUp;

    private void Start()
    {
        if (UIManager.instance)
            UIManager.instance.RegisterPlayerInventory(this);
    }

    private void Update()
    {
        ChangeWeaponInput();
    }

    private void ChangeWeaponInput()
    {
        if (!InputManager.instance)
            return;

        if (Time.time < nextSwitchTime)
            return;

        if (InputManager.instance.NextPressed || Mouse.current.scroll.ReadValue().y > 0.1f)
        {
            nextSwitchTime = Time.time + weaponSwitchCooldown;
            ChangeWeapon(1);
        }
        else if (InputManager.instance.PreviousPressed || Mouse.current.scroll.ReadValue().y < -0.1f)
        {
            nextSwitchTime = Time.time + weaponSwitchCooldown;
            ChangeWeapon(-1);
        }
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (!weapon)
            return;

        // Duplicate weapon check
        foreach (WeaponSlot slot in ownedWeapons)
        {
            if (slot.weaponData == weapon)
                return;
        }

        if (ownedWeapons.Count >= maxWeaponSlots)
        {
            Debug.LogWarning("Cannot pick up more weapons - inventory full!");
            return;
        }

        WeaponSlot newSlot = new WeaponSlot
        {
            weaponData = weapon,
        };

        ownedWeapons.Add(newSlot);
        OnWeaponPickUp?.Invoke(weapon);
        EquipWeapon(weapon); // Equip the new weapon upon picking it up
    }

    public void EquipWeapon(WeaponData weapon)
    {
        if (!weapon)
            return;

        int index = ownedWeapons.FindIndex(w => w.weaponData == weapon);
        if (index < 0)
        {
            Debug.LogWarning($"Tried to equip {weapon.weaponName} that isn't in your inventory!");
            return;
        }

        currentWeapon = weapon;
        currentWeaponSlot = index;
        weaponModel.GetComponent<MeshFilter>().sharedMesh = weapon.model.GetComponent<MeshFilter>().sharedMesh;
        weaponModel.GetComponent<MeshRenderer>().sharedMaterial = weapon.model.GetComponent<MeshRenderer>().sharedMaterial;
        gunTip.localPosition = weapon.model.transform.Find("GunTip").localPosition;
        OnWeaponSwitched?.Invoke(weapon);
    }

    public void RemoveWeapon(WeaponData weapon)
    {
        if (!weapon)
            return;

        int index = ownedWeapons.FindIndex(w => w.weaponData == weapon);
        if (index < 0)
            return;

        ownedWeapons.RemoveAt(index);
        OnWeaponDropped?.Invoke(weapon);
        Debug.Log($"Dropped {weapon.weaponName}");
        if (ownedWeapons.Count <= 0)
        {
            currentWeapon = null;
            currentWeaponSlot = -1;
            return;
        }

        // Wrap slot index to stay within valid range
        currentWeaponSlot = Mathf.Clamp(currentWeaponSlot, 0, ownedWeapons.Count - 1);
        EquipWeapon(ownedWeapons[currentWeaponSlot].weaponData);
    }

    public void ChangeWeapon(int direction)
    {
        if (ownedWeapons.Count <= 0)
            return;

        // Direction: +1 = next, -1 = previous
        currentWeaponSlot += direction;
        if (currentWeaponSlot >= ownedWeapons.Count)
            currentWeaponSlot = 0;
        else if (currentWeaponSlot < 0)
            currentWeaponSlot = ownedWeapons.Count - 1;

        EquipWeapon(ownedWeapons[currentWeaponSlot].weaponData);
    }

    public void AddThrowable(WeaponData throwable, int amount = 1)
    {
        if (!throwable || amount <= 0)
            return;

        foreach (ThrowableSlot slot in ownedThrowables)
        {
            if (slot.throwableData == throwable)
            {
                slot.quantity += amount;
                OnThrowablePickUp?.Invoke(throwable);
                return;
            }
        }

        ThrowableSlot newSlot = new ThrowableSlot
        {
            throwableData = throwable,
            quantity = amount
        };

        ownedThrowables.Add(newSlot);
        OnThrowablePickUp?.Invoke(throwable);
    }

    public bool UseThrowable(WeaponData throwable)
    {
        if (!throwable)
            return false;

        ThrowableSlot slot = ownedThrowables.Find(t => t.throwableData == throwable);
        if (slot == null || slot.quantity <= 0)
            return false;

        Debug.Log($"Used {throwable.weaponName}");
        slot.quantity--;
        return true;
    }

    public WeaponSlot GetWeaponSlot(WeaponData weapon)
    {
        return ownedWeapons.Find(w => w.weaponData == weapon);
    }

    public ThrowableSlot GetThrowableSlot(WeaponData throwable)
    {
        return ownedThrowables.Find(t => t.throwableData == throwable);
    }

    public int GetThrowableCount(WeaponData throwable)
    {
        ThrowableSlot slot = GetThrowableSlot(throwable);
        return slot != null ? slot.quantity : 0;
    }
}