using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager instance;

    [Header("States")]
    public bool isPaused = false;

    [Header("Events")]
    public UnityEvent OnPause;
    public UnityEvent OnResume;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        PauseInput();
    }

    private void PauseInput()
    {
        if (InputManager.instance.PausePressed)
        {
            if (isPaused)
            {
                ResumeGame();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                PauseGame();
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        InputManager.instance.DisablePlayerInput();
        InputManager.instance.EnableUIInput();
        UIManager.instance.OpenPause();
        OnPause?.Invoke();
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;
        InputManager.instance.EnablePlayerInput();
        InputManager.instance.DisableUIInput();
        UIManager.instance.OpenHUD();
        OnResume?.Invoke();
    }

    public void RestartLevel()
    {
        ResumeGame();
        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }
}