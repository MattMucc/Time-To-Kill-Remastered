using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.Linq.Expressions;

public class RoomManager : MonoBehaviour
{
    [Header("Room Settings")]
    public string roomName;
    public bool showRoomName = true;
    public float roomNameDisplayTime = 3f;

    [Tooltip("Assign the waves that make up this room encounter.")]
    public List<WaveData> waves;
    public List<Transform> spawnPoints;

    [Header("Barrier Settings")]
    [Tooltip("Objects that activate during combat and decactivate when cleared (e.g. energy gates).")]
    public List<GameObject> barriers;

    public LayerMask groundLayer;

    [Header("States (Read-Only)")]
    [SerializeField] private WaveData currentWave;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int enemiesKilledThisWave = 0;
    [SerializeField] private bool roomActive = false;
    [SerializeField] private bool roomCleared = false;
    private List<GameObject> activeEnemies = new List<GameObject>();

    [Header("Events")]
    public UnityEvent OnRoomStarted;
    public UnityEvent OnWaveStarted;
    public UnityEvent OnWaveCleared;
    public UnityEvent OnRoomCleared;

    private MultiplierSystem multiplier;

    private void Start()
    {
        SaveData data = SaveSystem.LoadGame();
        if (data != null)
        {
            if (data.roomState.clearedRooms.Contains(roomName))
                roomCleared = true;
        }

        multiplier = FindFirstObjectByType<MultiplierSystem>();
        foreach(GameObject barrier in barriers)
        {
            if (barrier)
                barrier.SetActive(false);
        }
    }

    public void StartRoom()
    {
        if (roomActive || roomCleared)
            return;

        Debug.Log("Room enocunter started!");
        roomActive = true;
        OnRoomStarted?.Invoke();

        ToggleBarriers(true);
        UIManager.instance.ShowRoomInfo(roomName, waves.Count);
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(currentWave.startDelay);
        for (currentWaveIndex = 0; currentWaveIndex < waves.Count; currentWaveIndex++)
        {
            currentWave = waves[currentWaveIndex];

            Debug.Log($"Starting Wave {currentWaveIndex + 1}");
            OnWaveStarted?.Invoke();
            multiplier?.ResumeDecay(); // Resume decay when wave starts
            SpawnWave();

            UIManager.instance.UpdateWaveDisplay(currentWaveIndex + 1);
            UIManager.instance.UpdateEnemiesRemaining(activeEnemies.Count, roomActive);

            // Wait until progression conditions are met
            yield return new WaitUntil(() => ShouldAdvanceWave());

            //multiplier?.PauseDecay(); // Pause decay when wave ends
            OnWaveCleared?.Invoke();
            yield return new WaitForSeconds(currentWave.delayBeforeNextWave);
        }

        OnRoomCleared?.Invoke();
        roomCleared = true;
        roomActive = false;
        ToggleBarriers(false);
        multiplier?.PauseDecay(); // Pause decay when room is cleared
        UIManager.instance.HideWaveDisplay();
        UIManager.instance?.HideEnemiesRemaining();
        Debug.Log($"{roomName} cleared!");
    }

    private void ToggleBarriers(bool value)
    {
        foreach (GameObject barrier in barriers)
        {
            if (barrier)
                barrier.SetActive(value);
        }
    }

    private void SpawnWave()
    {
        enemiesKilledThisWave = 0;
        activeEnemies.Clear();
        foreach (EnemySpawnInfo enemyInfo in currentWave.enemiesToSpawn)
            for (int i = 0; i < enemyInfo.count; i++)
                SpawnEnemy(enemyInfo);
    }

    private void SpawnEnemy(EnemySpawnInfo enemyInfo)
    {
        // Get random spawn point
        Transform spawnPoint = enemyInfo.fixedSpawnPoint ? enemyInfo.fixedSpawnPoint : GetRandomSpawnPoint();
        if (!spawnPoint)
            return;

        // Find a safe spawn positon using the spawn point transform so the enemy doesn't slide or clip
        Vector3 safePos = Vector3.zero;
        if (Physics.Raycast(spawnPoint.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            safePos = hit.point + Vector3.up * 0.1f;

        // Instantiate new enemy nd add to list
        GameObject enemy = Instantiate(enemyInfo.enemyPrefab, safePos, spawnPoint.rotation);
        activeEnemies.Add(enemy);

        // Apply scaling
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
        if (enemyHealth)
        {
            enemyHealth.maxHealth *= currentWave.healthMultiplier;
            enemyHealth.currentHealth = enemyHealth.maxHealth;
            enemyHealth.OnEnemyDeath.AddListener(() => OnEnemyDied(enemy));
        }

        if (baseEnemy)
            baseEnemy.damage *= currentWave.damageMultiplier;
    }

    private bool ShouldAdvanceWave()
    {
        if (currentWave.advanceWhenCleared && activeEnemies.Count <= 0)
            return true;

        if (currentWave.killsRequiredForNextWave > 0 && enemiesKilledThisWave >= currentWave.killsRequiredForNextWave)
            return true;

        return false;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count <= 0)
            return null;

        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    private void OnEnemyDied(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);

        enemiesKilledThisWave++;
        UIManager.instance.UpdateEnemiesRemaining(activeEnemies.Count, roomActive);
    }

    private void OnDrawGizmosSelected()
    {
        // Draws wire spheres at spawn point positions
        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint)
                Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        }
    }
}