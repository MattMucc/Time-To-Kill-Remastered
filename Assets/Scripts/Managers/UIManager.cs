using NUnit.Framework;
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
    [SerializeField] private Image shieldbar;
    [SerializeField] private TMP_Text spText;
    [SerializeField] private Image healthbar;
    [SerializeField] private TMP_Text hpText;
    private PlayerHealth playerHealth;

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

    private void OnControlSchemeChanged(string scheme)
    {
        interactPrompt?.RefreshKeyDisplay();
    }

    public void ShowPrompt(string message) => interactPrompt?.ShowPrompt(message);
    public void HidePrompt() => interactPrompt?.HidePrompt();
}