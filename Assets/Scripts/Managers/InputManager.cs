using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public PlayerControls controls;
    public Action<string> OnControlSchemeChanged;
    public string currentScheme = "Keyboard&Mouse";
    public string lastScheme = "Keyboard&Mouse";

    [Header("Inputs")]
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool SlideHeld { get; private set; }

    public bool AttackHeld { get; private set; }
    public bool ReloadPressed { get; private set; }
    public bool GrappleHeld { get; private set; }

    public bool NextPressed { get; private set; }
    public bool PreviousPressed { get; private set; }

    public bool InteractPressed { get; private set; }
    public bool PausePressed { get; private set; }

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        controls = new PlayerControls();
        controls.Player.Enable();
    }

    private void Update()
    {
        Move = controls.Player.Move.ReadValue<Vector2>();
        Look = controls.Player.Look.ReadValue<Vector2>();
        JumpPressed = controls.Player.Jump.WasPerformedThisFrame();
        SlideHeld = controls.Player.Slide.IsPressed();

        AttackHeld = controls.Player.Attack.IsPressed();
        ReloadPressed = controls.Player.Reload.WasPerformedThisFrame();
        GrappleHeld = controls.Player.Grapple.IsPressed();

        NextPressed = controls.Player.Next.WasPerformedThisFrame();
        PreviousPressed = controls.Player.Previous.WasPerformedThisFrame();

        InteractPressed = controls.Player.Interact.WasPerformedThisFrame();
        PausePressed = controls.Player.Pause.WasPerformedThisFrame();

        DetectCurrentScheme();
    }

    private void OnDestroy()
    {
        controls.Player.Disable();
        controls.Dispose();
    }

    public void EnablePlayerInput()
    {
        controls.Player.Enable();
    }

    public void DisablePlayerInput()
    {
        controls.Player.Disable();
    }

    public void EnableUIInput()
    {
        controls.UI.Enable();
    }

    public void DisableUIInput()
    {
        controls.UI.Disable();
    }

    private void DetectCurrentScheme()
    {
        string newScheme = currentScheme;
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            newScheme = "Gamepad";
        else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            newScheme = "Keyboard&Mouse";

        if (newScheme != lastScheme)
        {
            lastScheme = newScheme;
            currentScheme = newScheme;
            OnControlSchemeChanged?.Invoke(currentScheme);
        }
    }

    public string GetBindingDisplay(string actionName)
    {
        InputAction action = controls.asset.FindAction(actionName, throwIfNotFound:false);
        if (action == null)
        {
            Debug.LogWarning($"Action '{actionName} not found in InputManager");
            return "?";
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.groups.Contains(currentScheme))
                return action.GetBindingDisplayString(bindingIndex: i);
        }

        // Fallback
        return action.GetBindingDisplayString();
    }
}