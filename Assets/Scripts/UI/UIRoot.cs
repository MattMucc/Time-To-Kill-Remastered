using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public static UIRoot instance { get; private set; }

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }
}