using UnityEngine;

public static class SaveSystem
{
    private const string levelKey = "CurrentLevel";
    private const string checkpointKey = "CurrentCheckpoint";

    public static void SaveProgress(string levelName, string checkpointName)
    {
        PlayerPrefs.SetString(levelKey, levelName);
        PlayerPrefs.SetString(checkpointKey, checkpointName);
        PlayerPrefs.Save();
    }

    public static SaveData LoadProgress()
    {
        SaveData data = new SaveData();
        data.currentLevel = PlayerPrefs.GetString(levelKey, "");
        data.currentCheckpoint = PlayerPrefs.GetString(checkpointKey, "");
        return data;
    }

    public static bool HasSaveData()
    {
        return PlayerPrefs.HasKey(levelKey);
    }

    public static void ClearData()
    {
        PlayerPrefs.DeleteKey(levelKey);
        PlayerPrefs.DeleteKey(checkpointKey);
    }
}