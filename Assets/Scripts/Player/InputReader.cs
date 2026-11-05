using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset input;
    [SerializeField] private string gameplayMapName = "Gameplay";
    private InputActionMap inputMap;
    private InputActionAsset runtimeInput;
    private bool gameplayEnabled = true;
    private bool hasFocus = true;
    private bool jumpQueued;
    private bool slideQueued;
    public bool IsSlideHeld => GameplayActive && slideAction != null && slideAction.IsPressed();

    public bool GameplayActive => isActiveAndEnabled && gameplayEnabled && hasFocus &&
        inputMap != null && inputMap.enabled && Time.timeScale > 0f;
    public bool LookUsesGamepad => lookAction?.activeControl?.device is Gamepad;
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
    public event Action OnInputsCleared;

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
            Debug.LogError($"{nameof(InputReader)}: No InputActionAsset assigned!", this);
            enabled = false;
            return;
        }

        runtimeInput = Instantiate(input);
        inputMap = runtimeInput.FindActionMap(gameplayMapName, true);
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
        jumpAction.performed += QueueJump;
        slideAction.performed += QueueSlide;
    }

    private void OnEnable()
    {
        if (gameplayEnabled && hasFocus) inputMap?.Enable();
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        inputMap?.Disable();
        InputSystem.onActionChange -= OnActionChange;
        ClearInputs();
    }

    private void Update()
    {
        if (!GameplayActive)
        {
            ClearInputs();
            return;
        }

        Move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
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

    // Suspends player commands, not world physics or existing momentum.
    public void SetGameplayEnabled(bool value)
    {
        gameplayEnabled = value;
        RefreshGameplayState();
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        RefreshGameplayState();
    }

    private void RefreshGameplayState()
    {
        ClearInputs();
        if (isActiveAndEnabled && gameplayEnabled && hasFocus) inputMap?.Enable();
        else inputMap?.Disable();
    }

    private void QueueJump(InputAction.CallbackContext context)
    {
        if (GameplayActive) jumpQueued = true;
    }

    // Retain a short press until physics runs, then consume it exactly once.
    public bool ConsumeJumpPressed()
    {
        bool pressed = GameplayActive && jumpQueued;
        jumpQueued = false;
        return pressed;
    }

    private void QueueSlide(InputAction.CallbackContext context)
    {
        if (GameplayActive) slideQueued = true;
    }

    public bool ConsumeSlidePressed()
    {
        bool pressed = GameplayActive && slideQueued;
        slideQueued = false;
        return pressed;
    }

    private void ClearInputs()
    {
        Move = Look = ScrollWeapon = Vector2.zero;
        slideQueued = false;
        jumpQueued = false;
        JumpPressed = false;
        SlidePressed = false;
        SlideHeld = false;
        MeleePressed = false;
        AimPressed = false;
        AimHeld = false;
        AttackPressed = false;
        AttackHeld = false;
        ReloadPressed = false;
        ReloadHeld = false;
        UtilityPressed = false;
        UtilityHeld = false;
        GrapplePressed = false;
        GrappleHeld = false;
        ReelPressed = false;
        ReelHeld = false;
        InteractPressed = false;
        InteractHeld = false;
        PrimarySlotPressed = false;
        SecondarySlotPressed = false;
        UtilitySlotPressed = false;
        CycleWeaponPressed = false;
        OnInputsCleared?.Invoke();
    }

    private void OnDestroy()
    {
        if (jumpAction != null) jumpAction.performed -= QueueJump;
        if (slideAction != null) slideAction.performed -= QueueSlide;
        if (runtimeInput) Destroy(runtimeInput);
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
