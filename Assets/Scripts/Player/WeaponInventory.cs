using System;
using System.Collections.Generic;

// Plain state: testable independently of Unity's scene and input timing.
public sealed class WeaponInstance
{
    public readonly string Id;
    public readonly int MaxMagazine, MaxReserve;
    public readonly bool DiscardMagazine;
    public int Magazine { get; private set; }
    public int Reserve { get; private set; }
    public WeaponInstance(string id, int maxMagazine, int maxReserve, bool discard, int magazine, int reserve)
    {
        Id = id; MaxMagazine = Math.Max(1, maxMagazine); MaxReserve = Math.Max(0, maxReserve);
        DiscardMagazine = discard;
        Magazine = Math.Clamp(magazine, 0, MaxMagazine); Reserve = Math.Clamp(reserve, 0, MaxReserve);
    }
    public bool CanReload => Magazine < MaxMagazine && Reserve > 0;
    public bool Fire() { if (Magazine == 0) return false; Magazine--; return true; }
    public int Reload()
    {
        if (!CanReload) return 0;
        int retained = DiscardMagazine ? 0 : Magazine;
        int used = Math.Min(MaxMagazine - retained, Reserve);
        Magazine = retained + used; Reserve -= used;
        return used;
    }
    public int Absorb(WeaponInstance source)
    {
        if (source == this || source.Id != Id) return 0;
        int moved = Math.Min(MaxReserve - Reserve, source.Magazine + source.Reserve);
        int fromReserve = Math.Min(moved, source.Reserve);
        source.Reserve -= fromReserve;
        source.Magazine -= moved - fromReserve;
        Reserve += moved;
        return moved;
    }
}

public sealed class WeaponInventory
{
    public const int Capacity = 3;
    private readonly List<WeaponInstance> slots = new List<WeaponInstance>();
    public int Count => slots.Count;
    public int EquippedIndex { get; private set; }
    public WeaponInstance Equipped => slots.Count == 0 ? null : slots[EquippedIndex];
    public WeaponInstance Find(string id) => slots.Find(w => w.Id == id);
    public bool Pickup(WeaponInstance weapon, bool allowSwap, out WeaponInstance dropped)
    {
        dropped = null;
        if (weapon == null || Find(weapon.Id) != null) return false;
        if (slots.Count < Capacity) { slots.Add(weapon); EquippedIndex = slots.Count - 1; return true; }
        if (!allowSwap) return false;
        dropped = slots[EquippedIndex]; slots[EquippedIndex] = weapon;
        return true;
    }
    public void Cycle(int direction)
    {
        if (slots.Count > 0) EquippedIndex = (EquippedIndex + Math.Sign(direction) + slots.Count) % slots.Count;
    }
}

public sealed class PickupHold
{
    private int target;
    private double started;
    private bool armed;
    public void Reset() { armed = false; target = 0; }
    public bool Step(bool pressed, bool held, int candidate, double now, float duration)
    {
        if (!held || candidate == 0) { Reset(); return false; }
        if (pressed) { target = candidate; started = now; armed = true; }
        if (!armed || target != candidate) { Reset(); return false; }
        if (now - started < duration) return false;
        Reset(); return true;
    }
}
