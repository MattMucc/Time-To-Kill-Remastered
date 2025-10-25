using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Room/Wave Data", order = 1)]
public class WaveData : ScriptableObject
{
    [Header("Wave Settings")]
    [Tooltip("Optional display number for achievement tracking or title cards.")]
    public string waveNumber;

    [Header("Spawn Settings")]
    [Tooltip("Delay before the next wave starts after clearing the previous wave.")]
    public float startDelay = 2f;

    [Tooltip("Delay before the next wave starts (if auto-advancing).")]
    public float delayBeforeNextWave = 3f;

    [Tooltip("How many enemies must be killed before the next wave starts.")]
    [Min(0)] public int killsRequiredForNextWave = 0;

    [Tooltip("If true, this wave automatically progresses when all enemies are dead.")]
    public bool advanceWhenCleared = true;

    [Header("Enemy Settings")]
    [Range(0.5f, 5f)] public float healthMultiplier = 1f;
    [Range(0.5f, 5f)] public float damageMultiplier = 1f;
    public List<EnemySpawnInfo> enemiesToSpawn;
}

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public int count = 1;

    [Tooltip("Optional specific spawn point (if left null, RoomManager picks randomly).")]
    public Transform fixedSpawnPoint;
}