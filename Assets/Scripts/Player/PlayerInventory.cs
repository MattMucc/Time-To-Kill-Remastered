
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;
    public float currentAmmo;
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
    public int maxWeaponSlots = 5;
    public int maxThrowableSlots = 1;
    public int maxThrowableAmount = 5;

    [Header("Events")]
    public UnityEvent<WeaponData> OnWeaponPickUp;
    public UnityEvent<WeaponData> OnWeaponSwitched;
    public UnityEvent<WeaponData> OnWeaponDropped;
    public UnityEvent<WeaponData> OnThrowablePickUp;

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

        WeaponSlot newSlot = new WeaponSlot
        {
            weaponData = weapon,
            currentAmmo = weapon.currentAmmo
        };

        ownedWeapons.Add(newSlot);
        OnWeaponPickUp?.Invoke(weapon);
        Debug.Log($"Picked up {weapon.weaponName}");
        EquipWeapon(weapon); // Equip the new weapon upon picking it up
    }

    public void EquipWeapon(WeaponData weapon)
    {
        if (!weapon)
            return;

        currentWeapon = weapon;
        OnWeaponSwitched?.Invoke(weapon);
    }

    public void RemoveWeapon(WeaponData weapon)
    {
        if (!weapon)
            return;

        ownedWeapons.RemoveAll(w => w.weaponData == weapon);
        OnWeaponDropped?.Invoke(weapon);
        Debug.Log($"Dropped {weapon.weaponName}");
        if (currentWeapon == weapon && ownedWeapons.Count > 0)
            EquipWeapon(ownedWeapons[0].weaponData);
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

        ownedThrowables.Add(new ThrowableSlot
        {
            throwableData = throwable,
            quantity = amount
        });

        OnThrowablePickUp?.Invoke(throwable);
        Debug.Log($"Picked up {throwable.weaponName}");
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