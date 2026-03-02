using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset input;
    [SerializeField] private string gameplayMapName = "Gameplay";
    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private InputAction meleeAction;
    private InputAction aimAction;
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction utilityAction;
    private InputAction grappleAction;
    private InputAction reelAction;
    private InputAction interactAction;
    private InputAction primarySlotAction;
    private InputAction secondarySlotAction;
    private InputAction utilitySlotAction;
    private InputAction scrollWeaponAction;
    private InputAction cycleWeaponAction;

    // Control Scheme
    public enum ControlScheme { Keyboard, Gamepad }
    public ControlScheme CurrentScheme { get; private set; } = ControlScheme.Keyboard;
    public event Action<ControlScheme> OnControlSchemeChanged;

    // Input Actions
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public Vector2 ScrollWeapon { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SlidePressed { get; private set; }
    public bool SlideHeld { get; private set; }
    public bool MeleePressed { get; private set; }
    public bool AimPressed { get; private set; }
    public bool AimHeld { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool ReloadPressed { get; private set; }
    public bool ReloadHeld { get; private set; }
    public bool UtilityPressed { get; private set; }
    public bool UtilityHeld { get; private set; }
    public bool GrapplePressed { get; private set; }
    public bool GrappleHeld { get; private set; }
    public bool ReelPressed { get; private set; }
    public bool ReelHeld { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InteractHeld { get; private set; }
    public bool PrimarySlotPressed { get; private set; }
    public bool SecondarySlotPressed { get; private set; }
    public bool UtilitySlotPressed { get; private set; }
    public bool CycleWeaponPressed { get; private set; }

    private void Awake()
    {
        if (!input)
        {
            Debug.LogError($"{nameof(PlayerInput)}: No InputActionAsset assigned!", this);
            enabled = false;
            return;
        }

        inputMap = input.FindActionMap(gameplayMapName, true);
        moveAction = inputMap.FindAction("Move", true);
        lookAction = inputMap.FindAction("Look", true);
        jumpAction = inputMap.FindAction("Jump", true);
        slideAction = inputMap.FindAction("Slide", true);
        meleeAction = inputMap.FindAction("Melee", true);
        aimAction = inputMap.FindAction("Aim", true);
        attackAction = inputMap.FindAction("Attack", true);
        reloadAction = inputMap.FindAction("Reload", true);
        utilityAction = inputMap.FindAction("Utility", true);
        grappleAction = inputMap.FindAction("Grapple", true);
        reelAction = inputMap.FindAction("Reel", true);
        interactAction = inputMap.FindAction("Interact", true);
        primarySlotAction = inputMap.FindAction("PrimarySlot", true);
        secondarySlotAction = inputMap.FindAction("SecondarySlot", true);
        utilitySlotAction = inputMap.FindAction("UtilitySlot", true);
        scrollWeaponAction = inputMap.FindAction("ScrollWeapon", true);
        cycleWeaponAction = inputMap.FindAction("CycleWeapon", true);
    }

    private void OnEnable()
    {
        inputMap?.Enable();
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        inputMap?.Disable();
        InputSystem.onActionChange -= OnActionChange;
    }

    private void Update()
    {
        Move = moveAction.ReadValue<Vector2>();
        Look = lookAction.ReadValue<Vector2>();
        ScrollWeapon = scrollWeaponAction.ReadValue<Vector2>();

        // Pressed Inputs
        JumpPressed = jumpAction.WasPressedThisFrame();
        SlidePressed = slideAction.WasPressedThisFrame();
        MeleePressed = meleeAction.WasPressedThisFrame();
        AimPressed = aimAction.WasPressedThisFrame();
        AttackPressed = attackAction.WasPressedThisFrame();
        ReloadPressed = reloadAction.WasPressedThisFrame();
        UtilityPressed = utilityAction.WasPressedThisFrame();
        GrapplePressed = grappleAction.WasPressedThisFrame();
        ReelPressed = reelAction.WasPressedThisFrame();
        InteractPressed = interactAction.WasPressedThisFrame();
        PrimarySlotPressed = primarySlotAction.WasPressedThisFrame();
        SecondarySlotPressed = secondarySlotAction.WasPressedThisFrame();
        UtilitySlotPressed = utilitySlotAction.WasPressedThisFrame();
        CycleWeaponPressed = cycleWeaponAction.WasPressedThisFrame();

        // Held Inputs
        SlideHeld = slideAction.IsPressed();
        AimHeld = aimAction.IsPressed();
        AttackHeld = attackAction.IsPressed();
        ReloadHeld = reloadAction.IsPressed();
        UtilityHeld = utilityAction.IsPressed();
        GrappleHeld = grappleAction.IsPressed();
        InteractHeld = interactAction.IsPressed();

        // Simultaneous Inputs
        ReelHeld = grappleAction.IsPressed() && reelAction.IsPressed();
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;

        InputAction action = obj as InputAction;
        if (action == null) return;

        // Only respond to GAMEPLAY actions, not UI actions
        if (action.actionMap != inputMap) return;

        InputDevice device = action.activeControl?.device;
        if (device == null) return;

        ControlScheme newScheme = device is Gamepad ? ControlScheme.Gamepad : (device is Keyboard || device is Mouse) ? ControlScheme.Keyboard : CurrentScheme;
        if (newScheme != CurrentScheme)
        {
            CurrentScheme = newScheme;
            OnControlSchemeChanged?.Invoke(CurrentScheme);
        }
    }
}