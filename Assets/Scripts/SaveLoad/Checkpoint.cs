using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public string checkpointName;
    public bool oneTimeUse = true;
    [SerializeField] private bool isActivated = false;

    private void Start()
    {
        SaveData data = SaveSystem.LoadGame();
        if (data != null)
        {
            if (data.sceneState.checkpointsReached.Contains(checkpointName))
                isActivated = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (!inventory)
            return;

        SaveCheckpoint(inventory, other.transform);
        isActivated = true;
        Debug.Log($"Checkpoint reached: {checkpointName}");
    }

    private void SaveCheckpoint(PlayerInventory inventory, Transform playerTransform)
    {
        SaveData data = SaveSystem.LoadGame();
        if (SaveSystem.HasSaveData())
        {
            // Scene
            data.sceneState.sceneName = SceneLibrary.GetCurrentSceneName() == SceneLibrary.MainMenu ? SceneLibrary.LevelOne : SceneLibrary.GetCurrentSceneName();
            data.sceneState.checkpointName = checkpointName;
            data.sceneState.checkpoint = gameObject;
            if (!data.sceneState.checkpointsReached.Contains(checkpointName))
                data.sceneState.checkpointsReached.Add(checkpointName);

            // Inventory
            data.playerState.position = playerTransform.position;
            data.playerState.currentWeaponSlot = inventory.currentWeaponSlot;
            data.playerState.currentWeapon = inventory.currentWeapon;
            data.playerState.weapons = inventory.ownedWeapons;
            data.playerState.throwables = inventory.ownedThrowables;
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
            data.sceneState.sceneName = SceneLibrary.GetCurrentSceneName() == SceneLibrary.MainMenu ? SceneLibrary.LevelOne : SceneLibrary.GetCurrentSceneName();
            data.sceneState.checkpointName = checkpointName;
            data.sceneState.checkpoint = gameObject;
            if (!data.sceneState.checkpointsReached.Contains(checkpointName))
                data.sceneState.checkpointsReached.Add(checkpointName);

            // Inventory
            data.playerState.position = playerTransform.position;
            data.playerState.currentWeaponSlot = inventory.currentWeaponSlot;
            data.playerState.currentWeapon = inventory.currentWeapon;
            data.playerState.weapons = inventory.ownedWeapons;
            data.playerState.throwables = inventory.ownedThrowables;
        }

        SaveSystem.SaveGame(data);
    }
}