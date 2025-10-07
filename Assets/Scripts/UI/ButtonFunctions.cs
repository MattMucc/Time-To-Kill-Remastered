using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    [Header("----- Custom Events -----")]
    public UnityEvent OnClickCustom;

    // Event Management

    public void InvokeCustom()
    {
        OnClickCustom?.Invoke();
    }

    // Scene Management

    public void LoadScene(string sceneName)
    {
        SceneLibrary.Load(sceneName);
    }

    public bool IsMainMenuScene()
    {
        return SceneLibrary.IsMainMenuScene;
    }

    // UI Management

    public void ShowMenu(string menuName)
    {
        if (UIManager.instance)
            UIManager.instance.ShowMenu(menuName);
        else
            Debug.LogWarning("UIManager is missing or can't be found in scene!");
    }

    public void HideMenu(string menuName)
    {
        if (UIManager.instance)
            UIManager.instance.HideMenu(menuName);
        else
            Debug.LogWarning("UIManager is missing or can't be found in scene!");
    }

    public void HideAllMenus()
    {
        if (UIManager.instance)
            UIManager.instance.HideAllMenus();
        else
            Debug.LogWarning("UIManager is missing or can't be found in scene!");
    }

    public bool IsMenuActive(string menuName)
    {
        if (UIManager.instance)
            return UIManager.instance.IsMenuActive(menuName);
        else
        {
            Debug.LogWarning("UIManager is missing or can't be found in scene!");
            return false;
        }
    }

    public void OpenMainMenu()
    {
        HideAllMenus();
        ShowMenu("Main");
    }

    public void OpenPause()
    {
        HideAllMenus();
        ShowMenu("Pause");
    }

    public void OpenSettings()
    {
        HideAllMenus();
        ShowMenu("Settings");
    }

    public void OpenCredits()
    {
        HideAllMenus();
        ShowMenu("Credits");
    }

    public void OpenControls()
    {
        HideAllMenus();
        ShowMenu("Controls");
    }

    public void Back()
    {
        HideAllMenus();
        if (IsMainMenuScene())
            ShowMenu("Main");
        else
            ShowMenu("Pause");
    }

    // Button Functionality

    public void Play()
    {
        if (SaveSystem.HasSaveData())
        {
            SaveData data = SaveSystem.LoadProgress();
            LoadScene(data.currentLevel);
        }
        else
            LoadScene(SceneLibrary.LevelOne);
    }

    public void NewGame()
    {
        SaveSystem.ClearData();
        LoadScene(SceneLibrary.LevelOne);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}