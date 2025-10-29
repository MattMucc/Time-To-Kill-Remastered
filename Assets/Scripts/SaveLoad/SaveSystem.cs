using UnityEngine;

public static class SaveSystem
{
    private const string saveKey = "GameSave";

    public static void Save()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SaveData data = LoadGame();
        if (HasSaveData())
        {
            // Scene
            data.sceneState.sceneName = data.sceneState.sceneName == null && SceneLibrary.GetCurrentSceneName() == SceneLibrary.MainMenu ? SceneLibrary.LevelOne : SceneLibrary.GetCurrentSceneName();
            if (data.sceneState.checkpointsReached.Count > 0)
                data.sceneState.checkpointName = data.sceneState.checkpointsReached[data.sceneState.checkpointsReached.Count - 1];

            if (data.sceneState.checkpointName != "")
                data.sceneState.checkpoint = GameObject.Find(data.sceneState.checkpointName);

            if (data.sceneState.checkpointName != "" && !data.sceneState.checkpointsReached.Contains(data.sceneState.checkpointName))
                data.sceneState.checkpointsReached.Add(data.sceneState.checkpointName);

            // Inventory
            if (player)
            {
                data.playerState.position = player.transform.position;
                data.playerState.currentWeapon = player.GetComponent<PlayerInventory>().currentWeapon;
                data.playerState.currentWeaponSlot = player.GetComponent<PlayerInventory>().currentWeaponSlot;
                data.playerState.weapons = player.GetComponent<PlayerInventory>().ownedWeapons;
                data.playerState.throwables = player.GetComponent<PlayerInventory>().ownedThrowables;
            }
        }
        else
        {
            data = new SaveData
            {
                worldState = new WorldStateData(),
                playerState = new PlayerStateData(),
                sceneState = new SceneStateData(),
                roomState = new RoomStateData(),
            };

            // Scene
            data.sceneState.sceneName = data.sceneState.sceneName == null && SceneLibrary.GetCurrentSceneName() == SceneLibrary.MainMenu ? SceneLibrary.LevelOne : SceneLibrary.GetCurrentSceneName();
            data.sceneState.checkpointName = "";
            data.sceneState.checkpoint = null;

            // Inventory
            if (player)
            {
                data.playerState.position = player.transform.position;
                data.playerState.currentWeapon = player.GetComponent<PlayerInventory>().currentWeapon;
                data.playerState.currentWeaponSlot = player.GetComponent<PlayerInventory>().currentWeaponSlot;
                data.playerState.weapons = player.GetComponent<PlayerInventory>().ownedWeapons;
                data.playerState.throwables = player.GetComponent<PlayerInventory>().ownedThrowables;
            }
        }

        SaveGame(data);
    }

    public static void SaveGame(SaveData data, bool displayGameSaved = true)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        UIManager.instance.DisplayGameSaved();
        Debug.Log(JsonUtility.ToJson(data, true));
    }

    public static SaveData LoadGame()
    {
        if (!PlayerPrefs.HasKey(saveKey))
            return null;

        string json = PlayerPrefs.GetString(saveKey);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        return data;
    }

    public static void ClearData()
    {
        PlayerPrefs.DeleteKey(saveKey);
        SaveGame(new SaveData());
    }

    public static bool HasSaveData() => PlayerPrefs.HasKey(saveKey);
}