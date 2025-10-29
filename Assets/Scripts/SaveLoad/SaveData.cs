using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldStateData
{
    public List<string> collectedPickups = new List<string>();
    public List<string> defeatedEnemies = new List<string>();
}

[System.Serializable]
public class PlayerStateData
{
    public Vector3 position;
    public WeaponData currentWeapon;
    public int currentWeaponSlot;
    public List<WeaponSlot> weapons = new List<WeaponSlot>();
    public List<ThrowableSlot> throwables = new List<ThrowableSlot>();
}

[System.Serializable]
public class SceneStateData
{
    public string sceneName;
    public string checkpointName;
    public GameObject checkpoint;
    public List<string> checkpointsReached = new List<string>();
}

[System.Serializable]
public class RoomStateData
{
    public List<string> clearedRooms = new List<string>();
}

[System.Serializable]
public class SaveData
{
    public WorldStateData worldState = new WorldStateData();
    public PlayerStateData playerState = new PlayerStateData();
    public SceneStateData sceneState = new SceneStateData();
    public RoomStateData roomState = new RoomStateData();
}