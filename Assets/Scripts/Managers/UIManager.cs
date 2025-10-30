using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
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

    [Header("Save References")]
    [SerializeField] private TMP_Text saveText;

    [Header("Prompt References")]
    [SerializeField] private InteractPrompt interactPrompt;
    [SerializeField] private Image grappleLockOn;

    [Header("Reload References")]
    [SerializeField] private Image reloadCircle;
    [SerializeField] private TMP_Text reloadText;

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

    [Header("Room/Wave References")]
    [Tooltip("Optional text field to display the current room name.")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text currentWaveText;
    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private GameObject enemiesRemaining;

    // Component References
    private PlayerHealth playerHealth;
    private PlayerInventory playerInventory;
    private WeaponHandler weaponHandler;
    private MultiplierSystem multiplierSystem;

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

#if !UNITY_EDITOR

        if (menuLookup.TryGetValue("Pause", out GameObject pauseMenu))
        {
            if (pauseMenu)
            {
                GameObject clearDataButton = pauseMenu.transform.Find("Buttons/ClearData")?.gameObject;
                if (clearDataButton)
                    clearDataButton.SetActive(false);
            }
        }

        if (menuLookup.TryGetValue("Main", out GameObject mainMenu))
        {
            if (mainMenu)
            {
                GameObject clearDataButton = mainMenu.transform.Find("Buttons/ClearData")?.gameObject;
                if (clearDataButton)
                    clearDataButton.SetActive(false);
            }
        }
#endif

        multiplierMeter.fillAmount = 0;
        multiplierText.text = "x1";
        saveText.gameObject.SetActive(false);
        reloadCircle.gameObject.SetActive(false);
        reloadText.gameObject.SetActive(false);
        slideCooldownImage.fillAmount = 0;
        grappleCooldownImage.fillAmount = 0;
        roomNameText.gameObject.SetActive(false);
        currentWaveText.gameObject.SetActive(false);
        enemiesRemaining.SetActive(false);
    }

    private void Start()
    {
        if (SceneLibrary.IsMainMenuScene)
        {
            ShowMenu("Main");
            InputManager.instance.EnableUIInput();
        }
        else
        {
            InputManager.instance.EnablePlayerInput();
            OpenHUD();
        }
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

    public void OpenPause()
    {
        if (SceneLibrary.IsMainMenuScene)
            return;

        HideAllMenus();
        ShowMenu("Pause");
    }

    public void OpenHUD()
    {
        HideAllMenus();
        ShowMenu("HUD");
        ShowMenu("Prompt");
    }

    public void OpenPrompt()
    {
        HideAllMenus();
        ShowMenu("Prompt");
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

    public void RegisterWeaponHandler(WeaponHandler handler)
    {
        // Unsubscribe from old events
        if (weaponHandler)
        {
            weaponHandler.OnReloadStart.RemoveListener(ShowReload);
            weaponHandler.OnReloadProgress.RemoveListener(UpdateReload);
            weaponHandler.OnReloadComplete.RemoveListener(HideReload);
        }

        // Subscribe to new weapon handler
        weaponHandler = handler;
        if (weaponHandler)
        {
            weaponHandler.OnReloadStart.AddListener(ShowReload);
            weaponHandler.OnReloadProgress.AddListener(UpdateReload);
            weaponHandler.OnReloadComplete.AddListener(HideReload);
        }
    }

    public void RegisterMultiplierSystem(MultiplierSystem multiplier)
    {
        // Unsubscribe to old events
        if (multiplierSystem)
        {
            multiplierSystem.OnMeterChanged.RemoveListener(UpdateMultiplierMeter);
            multiplierSystem.OnMultiplierChanged.RemoveListener(UpdateMultiplierText);
        }

        // Subscribe to new multiplier system
        multiplierSystem = multiplier;
        if (multiplierSystem)
        {
            multiplierSystem.OnMeterChanged.AddListener(UpdateMultiplierMeter);
            multiplierSystem.OnMultiplierChanged.AddListener(UpdateMultiplierText);

            // Initialize UI
            UpdateMultiplierMeter(multiplierSystem.meter);
            UpdateMultiplierText(multiplierSystem.multiplier);
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
    }

    private void ShowReload()
    {
        reloadCircle.gameObject.SetActive(true);
        reloadCircle.fillAmount = 0f;
        reloadText.gameObject.SetActive(true);
        reloadText.text = "Reloading...";
    }

    private void UpdateReload(float progress)
    {
        reloadCircle.fillAmount = Mathf.Clamp01(progress);
    }

    private void HideReload()
    {
        if (playerInventory && playerInventory.currentWeapon)
            UpdateWeapon(playerInventory.currentWeapon);

        reloadCircle.gameObject.SetActive(false);
        reloadText.gameObject.SetActive(false);
    }

    private void UpdateMultiplierMeter(float meter)
    {
        if (!multiplierMeter)
            return;

        multiplierMeter.fillAmount = Mathf.Clamp01(meter);
    }

    public void UpdateMultiplierText(int multiplier)
    {
        if (!multiplierText)
            return;

        multiplierText.text = $"x{multiplier}";
    }

    public void UpdateSlideCooldown(float progress)
    {
        if (!slideCooldownImage)
            return;

        slideCooldownImage.fillAmount = Mathf.Clamp01(progress);
    }

    public void UpdateGrappleCooldown(float progress)
    {
        if (!grappleCooldownImage)
            return;

        grappleCooldownImage.fillAmount = Mathf.Clamp01(progress);
    }

    public void ToggleGrappleLockOn(bool value)
    {
        if (!grappleLockOn)
            return;

        grappleLockOn.enabled = value;
    }

    public void ShowRoomInfo(string roomName, int totalWaves)
    {
        if (!roomNameText || string.IsNullOrEmpty(roomName) || !currentWaveText || totalWaves <= 0)
            return;

        StopAllCoroutines();
        StartCoroutine(FadeRoomName(roomName));
        currentWaveText.text = $"Wave 1";
        StartCoroutine(FadeInWaveDisplay());
        HideEnemiesRemaining();
    }

    public void UpdateWaveDisplay(int currentWave)
    {
        if (!currentWaveText || currentWave <= 0)
            return;

        currentWaveText.text = $"Wave {currentWave}";
    }

    public void UpdateEnemiesRemaining(int count, bool roomActive)
    {
        if (!enemyCountText || !enemiesRemaining)
            return;

        enemyCountText.text = $"{count}";
        enemiesRemaining.SetActive(count > 0 && roomActive);
    }

    public void HideWaveDisplay()
    {
        if (!currentWaveText)
            return;

        currentWaveText.gameObject.SetActive(false);
    }

    public void HideEnemiesRemaining()
    {
        if (!enemiesRemaining)
            return;

        enemiesRemaining.SetActive(false);
    }

    public void DisplayGameSaved()
    {
        if (!saveText)
            return;

        saveText.gameObject.SetActive(true);
        StartCoroutine(FadeSaveGame());
    }

    private IEnumerator FadeRoomName(string name)
    {
        roomNameText.text = name.ToUpper();
        roomNameText.gameObject.SetActive(true);

        Color c = roomNameText.color;
        c.a = 0f;
        roomNameText.color = c;

        // Fade in
        float fadeInTime = 1.5f;
        float stayTime = 2.5f;
        float fadeOutTime = 1.5f;
        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            roomNameText.color = c;
            yield return null;
        }

        c.a = 1f;
        roomNameText.color = c;

        yield return new WaitForSeconds(stayTime);

        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            roomNameText.color = c;
            yield return null;
        }

        c.a = 0f;
        roomNameText.color = c;
        roomNameText.gameObject.SetActive(false);
    }

    private IEnumerator FadeInWaveDisplay()
    {
        currentWaveText.gameObject.SetActive(true);

        Color c = currentWaveText.color;
        c.a = 0f;
        currentWaveText.color = c;

        float fadeInTime = 1.5f;
        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            currentWaveText.color = c;
            yield return null;
        }

        c.a = 1f;
        currentWaveText.color = c;
    }

    private IEnumerator FadeSaveGame()
    {
        Color c = saveText.color;
        c.a = 1f;
        saveText.color = c;

        yield return new WaitForSecondsRealtime(2f);

        float fadeOutTime = 1.5f;
        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            saveText.color = c;
            yield return null;
        }

        c.a = 0f;
        saveText.color = c;
        saveText.gameObject.SetActive(false);
    }

    private void OnControlSchemeChanged(string scheme)
    {
        interactPrompt?.RefreshKeyDisplay();
    }

    public void ShowPrompt(string message) => interactPrompt?.ShowPrompt(message);
    public void HidePrompt() => interactPrompt?.HidePrompt();
}