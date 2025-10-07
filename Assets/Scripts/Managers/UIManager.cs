using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }

    [System.Serializable]
    public class Menu
    {
        public string name;
        public GameObject menu;
    }

    [Header("Registered Menus")]
    public List<Menu> menus = new List<Menu>();
    private Dictionary<string, GameObject> menuLookup;

    [Header("HUD References")]
    [SerializeField] private InteractPrompt interactPrompt;

    [Header("Vital References")]
    [SerializeField] private Image shieldbar;
    [SerializeField] private TMP_Text spText;
    [SerializeField] private Image healthbar;
    [SerializeField] private TMP_Text hpText;

    [Header("Weapon References")]
    [SerializeField] private Image weaponImage;
    [SerializeField] private Image throwableImage;
    [SerializeField] private TMP_Text ammo;
    [SerializeField] private TMP_Text quantity;

    [Header("Multiplier References")]
    [SerializeField] private Image multiplierMeter;
    [SerializeField] private TMP_Text multiplierText;

    [Header("Ability References")]
    [SerializeField] private Image slideIcon;
    [SerializeField] private Image slideCooldownImage;
    [SerializeField] private Image grappleIcon;
    [SerializeField] private Image grappleCooldownImage;

    // Component References
    private PlayerHealth playerHealth;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        menuLookup = new Dictionary<string, GameObject>();
        foreach (Menu menu in menus)
        {
            if (!menuLookup.ContainsKey(menu.name))
                menuLookup.Add(menu.name, menu.menu);
        }

        slideCooldownImage.fillAmount = 0;
        grappleCooldownImage.fillAmount = 0;
        multiplierMeter.fillAmount = 0;
        multiplierText.text = "x1";
    }

    private void OnEnable()
    {
        if (InputManager.instance)
            InputManager.instance.OnControlSchemeChanged += OnControlSchemeChanged;
    }

    private void OnDisable()
    {
        if (InputManager.instance)
            InputManager.instance.OnControlSchemeChanged -= OnControlSchemeChanged;
    }

    // Menu Functionality

    public void ShowMenu(string menuName)
    {
        if (menuLookup.TryGetValue(menuName, out GameObject menu))
            menu.SetActive(true);
    }

    public void HideMenu(string menuName)
    {
        if (menuLookup.TryGetValue(menuName, out GameObject menu))
            menu.SetActive(false);
    }

    public void HideAllMenus()
    {
        foreach (GameObject menu in menuLookup.Values)
            menu.SetActive(false);
    }

    public bool IsMenuActive(string menuName)
    {
        return menuLookup.ContainsKey(menuName) && menuLookup[menuName].activeSelf;
    }

    // HUD Functionality

    public void RegisterPlayerHealth(PlayerHealth health)
    {
        // Unsubscribing from old PlayerHealth
        if (playerHealth)
        {
            playerHealth.OnShieldChanged.RemoveListener(UpdateShieldbar);
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthbar);
            playerHealth.OnPlayerDeath.RemoveListener(OnPlayerDeath);
        }

        // Subscribing to new PlayerHealth
        playerHealth = health;
        if (playerHealth)
        {
            playerHealth.OnShieldChanged.AddListener(UpdateShieldbar);
            playerHealth.OnHealthChanged.AddListener(UpdateHealthbar);
            playerHealth.OnPlayerDeath.AddListener(OnPlayerDeath);

            // Initialize UI with current values
            UpdateShieldbar(playerHealth.currentShield, playerHealth.maxShield);
            UpdateHealthbar(playerHealth.currentHealth, playerHealth.maxHealth);
        }
    }

    public void RegisterPlayerInventory(PlayerInventory inventory)
    {
        // Unsubscribe from old events
        if (playerInventory)
        {
            playerInventory.OnWeaponSwitched.RemoveListener(UpdateWeapon);
            playerInventory.OnWeaponPickUp.RemoveListener(UpdateWeapon);
            playerInventory.OnThrowablePickUp.RemoveListener(UpdateThrowable);
        }

        // Subscribe to new inventory
        playerInventory = inventory;
        if (playerInventory)
        {
            playerInventory.OnWeaponSwitched.AddListener(UpdateWeapon);
            playerInventory.OnWeaponPickUp.AddListener(UpdateWeapon);
            playerInventory.OnThrowablePickUp.AddListener(UpdateThrowable);

            // Initialize UI with whatever the player starts with
            if (playerInventory.currentWeapon != null)
                UpdateWeapon(playerInventory.currentWeapon);
            else
                UpdateWeapon(null);

            if (playerInventory.ownedThrowables.Count > 0)
                UpdateThrowable(playerInventory.ownedThrowables[0].throwableData);
            else
                UpdateThrowable(null);
        }
    }

    private void UpdateShieldbar(float current, float max)
    {
        if (!shieldbar || !spText)
            return;

        float percent = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        shieldbar.fillAmount = percent;
        spText.text = $"{Mathf.RoundToInt(percent * 100)}";
    }

    private void UpdateHealthbar(float current, float max)
    {
        if (!healthbar || !hpText)
            return;

        float percent = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        healthbar.fillAmount = percent;
        hpText.text = $"{Mathf.RoundToInt(percent * 100)}";
    }

    private void OnPlayerDeath()
    {
        HideAllMenus();
        ShowMenu("Game Over");
    }

    private void UpdateWeapon(WeaponData weapon)
    {
        if (!weapon)
        {
            weaponImage.sprite = null;
            ammo.text = "0/0";
            return;
        }

        weaponImage.sprite = weapon.weaponIcon;
        WeaponSlot slot = playerInventory.GetWeaponSlot(weapon);
        if (slot != null && weapon.maxAmmo > 0)
            ammo.text = $"{Mathf.Floor(weapon.currentAmmo)}/{Mathf.Floor(weapon.maxAmmo)}";
        else
            ammo.text = "∞/∞"; // For melee
    }

    private void UpdateThrowable(WeaponData throwable)
    {
        if (!throwable)
        {
            throwableImage.sprite = null;
            quantity.text = "0";
            return;
        }

        ThrowableSlot slot = playerInventory.GetThrowableSlot(throwable);
        throwableImage.sprite = throwable.weaponIcon;
        quantity.text = $"{slot.quantity}";
        Debug.LogWarning("Don't forget to update the quantity of the throwable as well!");
    }

    private void OnControlSchemeChanged(string scheme)
    {
        interactPrompt?.RefreshKeyDisplay();
    }

    public void ShowPrompt(string message) => interactPrompt?.ShowPrompt(message);
    public void HidePrompt() => interactPrompt?.HidePrompt();
}