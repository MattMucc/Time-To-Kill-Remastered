using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public static readonly HashSet<WeaponPickup> Active = new HashSet<WeaponPickup>();
    [SerializeField] private WeaponDefinition definition;
    [SerializeField, Min(0)] private int magazine = 12;
    [SerializeField, Min(0)] private int reserve = 36;
    public WeaponDefinition Definition => definition;
    public WeaponInstance State { get; private set; }
    private static int nextInteractionId;
    public int InteractionId { get; private set; }
    private void Awake()
    {
        InteractionId = ++nextInteractionId;
        if (definition) State = definition.Create(magazine, reserve);
    }
    private void OnEnable() { Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }
    public void Replace(WeaponDefinition next, WeaponInstance state)
    {
        definition = next; State = state;
        gameObject.name = next.displayName + " Pickup";
    }
    public void Consume() { gameObject.SetActive(false); Destroy(gameObject); }
}
