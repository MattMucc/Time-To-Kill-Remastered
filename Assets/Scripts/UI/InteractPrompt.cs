using TMPro;
using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text prompt;
    private string currentMessage = "";

    private void Awake()
    {
        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        if (!prompt || !canvasGroup)
            return;

        currentMessage = message;
        prompt.text = message;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HidePrompt()
    {
        if (!canvasGroup)
            return;

        currentMessage = "";
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void RefreshKeyDisplay()
    {
        if (string.IsNullOrEmpty(currentMessage))
            return;

        string key = InputManager.instance.GetBindingDisplay("Player/Interact");
        string newMessage = System.Text.RegularExpressions.Regex.Replace(currentMessage, @"(?<=Press \[).+?(?=\])", key);
        currentMessage = newMessage;
        prompt.text = newMessage;
    }
}