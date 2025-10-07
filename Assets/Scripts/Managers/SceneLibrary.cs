using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLibrary
{
    public const string TestLevel = "Test";
    public const string MainMenu = "MainMenu";
    public const string LevelOne = "Level1";

    public static void Load(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public static void ReloadCurrent()
    {
        Scene current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Load(current.name);
    }

    public static bool IsMainMenuScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == MainMenu;
}