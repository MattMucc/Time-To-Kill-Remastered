using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ButtonFunctions : MonoBehaviour
{
    [Header("----- Custom Events -----")]
    public UnityEvent OnClickCustom;

    public void InvokeCustom()
    {
        OnClickCustom?.Invoke();
    }

    public void Play()
    {
        if (SaveSystem.HasSaveData())
        {
            SaveData data = SaveSystem.LoadProgress();
            SceneLibrary.Load(data.currentLevel);
        }
        else
            SceneLibrary.Load(SceneLibrary.LevelOne);
    }

    public void NewGame()
    {
        SaveSystem.ClearData();
        SceneLibrary.Load(SceneLibrary.LevelOne);
    }

    public void LoadScene(string sceneName)
    {
        SceneLibrary.Load(sceneName);
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