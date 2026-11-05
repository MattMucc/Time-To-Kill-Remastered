using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InputReader), typeof(WeaponHud))]
public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform view;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject weaponModel;
    [SerializeField] private Material shotEffectMaterial;
    [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
    [SerializeField, Min(0.1f)] private float ammoPickupDistance = 1.4f;
    [SerializeField, Min(0.1f)] private float swapHoldSeconds = 0.65f;
    public WeaponInventory Inventory { get; } = new WeaponInventory();
    private readonly Dictionary<string, WeaponDefinition> definitions = new Dictionary<string, WeaponDefinition>();
    private readonly PickupHold swapHold = new PickupHold();
    private InputReader input;
    private WeaponHud hud;
    private WeaponInstance reloading;
    private double reloadEnds, nextShot;
    private WeaponShotEffects shotEffects;
    private bool interactionLatched;
    public bool IsAiming { get; private set; }
    private Camera aimCamera;
    private GrappleController grapple;
    private Transform weaponHolder;
    private Vector3 hipPosition;
    private float hipFieldOfView;

    private void Awake()
    {
        input = GetComponent<InputReader>(); hud = GetComponent<WeaponHud>();
        aimCamera = GetComponentInChildren<Camera>();
        grapple = GetComponent<GrappleController>();
        if (aimCamera) hipFieldOfView = aimCamera.fieldOfView;
        if (weaponModel)
        {
            weaponHolder = weaponModel.transform.parent;
            hipPosition = weaponHolder.localPosition;
        }
        if (!view) { Camera camera = GetComponentInChildren<Camera>(); if (camera) view = camera.transform; }
        shotEffects = new WeaponShotEffects(muzzle,view,shotEffectMaterial);
        if (weaponModel) weaponModel.SetActive(false);
    }
    private void OnEnable()
    {
        if (!input) input = GetComponent<InputReader>();
        input.OnInputsCleared += Suspend;
    }
    private void OnDisable()
    {
        if (input) input.OnInputsCleared -= Suspend;
        Suspend();
        if (hud) hud.SetVisible(false);
        if (weaponModel) weaponModel.SetActive(false);
    }
    private void Suspend()
    {
        ResetAim();
        reloading = null; swapHold.Reset(); interactionLatched = true;
        shotEffects?.Clear();
    }
    // InputReader runs at -100; LateUpdate also sees this frame's updated camera aim.
    private void LateUpdate()
    {
        if (!input.GameplayActive || !view) { Suspend(); hud.SetVisible(false); return; }
        hud.SetVisible(true);
        if (!input.InteractHeld) interactionLatched = false;
        WeaponPickup candidate = FindPickups();
        bool pickupContext = candidate != null && Inventory.Find(candidate.State.Id) == null;
        int cycle = input.ScrollWeapon.y == 0f ? 0 : (input.ScrollWeapon.y > 0f ? 1 : -1);
        if (input.CycleWeaponPressed) cycle = 1;
        if (cycle != 0) { Inventory.Cycle(cycle); reloading = null; swapHold.Reset(); }
        bool heldSwap = swapHold.Step(input.InteractPressed && !interactionLatched,
            input.InteractHeld && !interactionLatched, pickupContext ? candidate.InteractionId : 0,
            Time.timeAsDouble, swapHoldSeconds);
        if (pickupContext && !interactionLatched &&
            ((Inventory.Count < WeaponInventory.Capacity && input.InteractPressed) ||
             (Inventory.Count == WeaponInventory.Capacity && heldSwap)))
        {
            definitions[candidate.State.Id] = candidate.Definition;
            if (Inventory.Pickup(candidate.State, Inventory.Count == WeaponInventory.Capacity, out WeaponInstance dropped))
            {
                if (dropped == null) candidate.Consume();
                else candidate.Replace(definitions[dropped.Id], dropped);
                reloading = null; interactionLatched = true; swapHold.Reset();
                candidate = null;
            }
        }
        WeaponInstance equipped = Inventory.Equipped;
        UpdateAim(equipped);
        shotEffects.Tick(Time.timeAsDouble);
        if (weaponModel) weaponModel.SetActive(equipped != null);
        if (reloading != null && Time.timeAsDouble >= reloadEnds)
        { reloading.Reload(); reloading = null; }
        if (equipped != null)
        {
            WeaponDefinition definition = definitions[equipped.Id];
            // West's reload action loses priority only to a nearby distinct pickup.
            if (input.ReloadPressed && !(pickupContext && input.ReloadUsesGamepad)) StartReload();
            if (reloading == null && (definition.automatic ? input.AttackHeld : input.AttackPressed)
                && Time.timeAsDouble >= nextShot)
            {
                if (equipped.Fire())
                {
                    Shoot(definition); nextShot = Time.timeAsDouble + definition.shotInterval;
                    if (equipped.Magazine == 0) StartReload();
                }
                else StartReload();
            }
            hud.ShowWeapon(definition, equipped, reloading != null);
        }
        else hud.ShowWeapon(null, null, false);
        string button = input.CurrentScheme == InputReader.ControlScheme.Gamepad ? "X / West" : "E";
        string prompt = candidate == null ? "" : Inventory.Find(candidate.State.Id) != null
            ? candidate.Definition.displayName + " ammo (walk over to collect)"
            : (Inventory.Count == WeaponInventory.Capacity ? "Hold " : "Press ") + button
                + (Inventory.Count == WeaponInventory.Capacity ? " to swap for " : " to pick up ") + candidate.Definition.displayName;
        hud.ShowPrompt(prompt);
    }

    private void StartReload()
    {
        if (reloading != null || Inventory.Equipped == null || !Inventory.Equipped.CanReload) return;
        reloading = Inventory.Equipped;
        reloadEnds = Time.timeAsDouble + definitions[reloading.Id].reloadSeconds;
    }

    public static bool CanAim(bool gameplayActive, bool equipped, bool aimHeld, bool grappling)
        => gameplayActive && equipped && aimHeld && !grappling;

    private void UpdateAim(WeaponInstance equipped)
    {
        bool grappleBusy = input.IsGrappleHeld || (grapple && grapple.IsAttached);
        IsAiming = CanAim(input.GameplayActive, equipped != null, input.AimHeld, grappleBusy);
        // Grapple owns the shared RMB input, including the frame before physics attaches.
        if (grappleBusy || equipped == null) { ResetAim(); return; }
        WeaponDefinition definition = definitions[equipped.Id];
        float blend = 1f - Mathf.Exp(-definition.aimTransitionSpeed * Time.deltaTime);
        if (aimCamera) aimCamera.fieldOfView = Mathf.Lerp(aimCamera.fieldOfView,
            IsAiming ? Mathf.Min(hipFieldOfView, definition.aimFieldOfView) : hipFieldOfView, blend);
        if (weaponHolder) weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition,
            IsAiming ? definition.aimedWeaponPosition : hipPosition, blend);
    }

    private void ResetAim()
    {
        IsAiming = false;
        if (aimCamera) aimCamera.fieldOfView = hipFieldOfView;
        if (weaponHolder) weaponHolder.localPosition = hipPosition;
    }

    private WeaponPickup FindPickups()
    {
        WeaponPickup nearest = null;
        float best = float.PositiveInfinity;
        foreach (WeaponPickup pickup in new List<WeaponPickup>(WeaponPickup.Active))
        {
            if (!pickup || pickup.State == null || !pickup.Definition) continue;
            Vector3 delta = pickup.transform.position - transform.position;
            float distance = delta.magnitude;
            if (distance > pickupDistance || !Visible(pickup)) continue;
            WeaponInstance owned = Inventory.Find(pickup.State.Id);
            if (owned != null && distance <= ammoPickupDistance)
            {
                owned.Absorb(pickup.State);
                if (pickup.State.Magazine + pickup.State.Reserve == 0) { pickup.Consume(); continue; }
            }
            Vector3 aimDelta = pickup.transform.position - view.position;
            if (Vector3.Dot(view.forward, aimDelta.normalized) < 0.5f || distance >= best) continue;
            best = distance; nearest = pickup;
        }
        return nearest;
    }
    private bool Visible(WeaponPickup pickup)
    {
        Vector3 delta = pickup.transform.position - view.position;
        foreach (RaycastHit hit in Physics.RaycastAll(view.position, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (!IsSelf(hit.collider) && !hit.collider.transform.IsChildOf(pickup.transform)
                && hit.distance < delta.magnitude - 0.05f) return false;
        return true;
    }
    private bool IsSelf(Collider collider) => collider.transform.IsChildOf(transform);
    private bool ClosestHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
    {
        closest = default; bool found = false;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider) || (found && hit.distance >= closest.distance)) continue;
            closest = hit; found = true;
        }
        return found;
    }
    private void Shoot(WeaponDefinition definition)
    {
        bool hitSomething = ClosestHit(view.position, view.forward, definition.range, out RaycastHit hit);
        Vector3 end = hitSomething ? hit.point : view.position + view.forward * definition.range;
        Vector3 start = muzzle ? muzzle.position : view.position;
        // Test from camera to muzzle as well, so an embedded barrel cannot shoot through cover.
        Vector3 barrel = start - view.position;
        if (barrel.sqrMagnitude > 0.0001f && ClosestHit(view.position, barrel.normalized, barrel.magnitude, out RaycastHit cover))
        { hit = cover; hitSomething = true; end = cover.point; start = view.position; }
        else
        {
            Vector3 travel = end - start;
            if (ClosestHit(start, travel.normalized, travel.magnitude, out RaycastHit muzzleHit))
            { hit = muzzleHit; hitSomething = true; end = hit.point; }
        }
        if (hitSomething)
        {
            Damageable damageable = hit.collider.GetComponentInParent<Damageable>();
            if (damageable && damageable.ApplyDamage(definition.damage)) hud.ShowHit(damageable.Health <= 0f);
        }
        // Suppress barrel visuals if cover forced the hit origin back to the camera.
        if (!muzzle || (start-muzzle.position).sqrMagnitude < .0001f)
            shotEffects.Play(start,end,definition.bulletVisualSpeed,Time.timeAsDouble);
    }
    private void OnDestroy() { shotEffects?.Dispose(); }
}
