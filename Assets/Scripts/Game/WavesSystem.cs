using System.Collections.Generic;
using System.Linq;
using ProjectBlood;
using UnityEngine;


[System.Serializable]
public class EnemyDefinition
{
    public GameObject prefab;  // Enemy prefab
    public int strength;       // Relative strength (higher = stronger)
}

public class WavesSystem : MonoBehaviour
{
    [Header("Enemy pools")]
    [Tooltip("All enemies with strength value, will be sorted by strength automatically.")]
    [SerializeField] private EnemyDefinition[] allEnemies;   // total pool

    [Tooltip("Max enemy types in current pool.")]
    [SerializeField] private int maxCurrentPoolSize = 3;

    [Tooltip("How many waves between each pool update.")]
    [SerializeField] private int wavesPerPoolUpdate = 5;

    // ======= Old wave difficulty parameters (kept) =======
    [SerializeField] public int maxWavesNum = 3;        // not strictly needed now, saved in Global
    [SerializeField] private int increaseTotalNum = 5;
    [SerializeField] private int increaseMaxActive = 3;
    [SerializeField] private int increaseSingleSpawnNum = 2;
    [SerializeField] private int wave1TotalNum = 15;
    [SerializeField] private int wave1MaxActive = 8;
    [SerializeField] private int wave1SingleSpawnNum = 3;
    [SerializeField] private int spawnInterval = 5;

    // ======= Internal state =======
    private List<EnemyDefinition> sortedAllEnemies;   // total pool sorted by strength
    private List<EnemyDefinition> currentPool = new List<EnemyDefinition>();  // current pool (≤ maxCurrentPoolSize)
    private int nextEnemyGlobalIndex = 0;             // next index in sortedAllEnemies to unlock
    private int lastUpdatedWave = 0;                  // the wave when we last updated currentPool

    private void Awake()
    {
        InitEnemyPools();
    }

    private void Update()
    {
        UpdateCurrentPoolByWave();
    }

    /// <summary>
    /// Initialize total pool and build initial current pool.
    /// </summary>
    private void InitEnemyPools()
    {
        if (allEnemies == null || allEnemies.Length == 0)
        {
            Debug.LogWarning("WavesSystem: allEnemies is empty.");
            sortedAllEnemies = new List<EnemyDefinition>();
            return;
        }

        // Sort all enemies ascending by strength
        sortedAllEnemies = allEnemies
            .OrderBy(e => e.strength)
            .ToList();

        currentPool.Clear();
        nextEnemyGlobalIndex = 0;
        lastUpdatedWave = Global.CurrentWaves.Value;

        // Fill current pool from weakest to stronger, up to maxCurrentPoolSize
        for (int i = 0; i < maxCurrentPoolSize && nextEnemyGlobalIndex < sortedAllEnemies.Count; i++)
        {
            currentPool.Add(sortedAllEnemies[nextEnemyGlobalIndex]);
            nextEnemyGlobalIndex++;
        }
    }

    /// <summary>
    /// Check wave number and update current pool every wavesPerPoolUpdate waves.
    /// </summary>
    private void UpdateCurrentPoolByWave()
    {
        int currentWave = Global.CurrentWaves.Value;

        // safeguard
        if (currentWave <= 0 || sortedAllEnemies == null || sortedAllEnemies.Count == 0)
            return;

        // Only update when we have passed enough waves
        if (currentWave - lastUpdatedWave >= wavesPerPoolUpdate)
        {
            lastUpdatedWave = currentWave;
            PromoteNextEnemyIntoCurrentPool();
        }
    }

    /// <summary>
    /// Promote the next strongest enemy from total pool into current pool.
    /// If current pool is full, remove the weakest one first.
    /// </summary>
    private void PromoteNextEnemyIntoCurrentPool()
    {
        if (nextEnemyGlobalIndex >= sortedAllEnemies.Count)
        {
            // No more enemies to unlock
            Debug.Log("WavesSystem: no more enemies to promote.");
            return;
        }

        EnemyDefinition nextStrongest = sortedAllEnemies[nextEnemyGlobalIndex];
        nextEnemyGlobalIndex++;

        if (currentPool.Count < maxCurrentPoolSize)
        {
            currentPool.Add(nextStrongest);
        }
        else
        {
            // Remove the weakest in current pool
            EnemyDefinition weakest = currentPool
                .OrderBy(e => e.strength)
                .First();

            currentPool.Remove(weakest);
            currentPool.Add(nextStrongest);
        }

        Debug.Log($"WavesSystem: promoted enemy {nextStrongest.prefab.name} into current pool.");
    }

    // ================== Interface used by EnemySpawner ==================

    /// <summary>
    /// Return current enemy pool as GameObject array.
    /// EnemySpawner will randomly choose one from this pool.
    /// </summary>
    public GameObject[] SelectEnemiesByWaves()
    {
        // If currentPool is empty (for some reason), fall back to total pool
        if (currentPool.Count == 0 && sortedAllEnemies != null)
        {
            return sortedAllEnemies.Select(e => e.prefab).ToArray();
        }

        return currentPool.Select(e => e.prefab).ToArray();
    }

    // ================== Difficulty auto-growth ==================

    public void FinishWave()
    {
        // increase the difficulty between waves automatically
        if (Global.CurrentWaves.Value <= Global.maxWavesNum.Value)
        {
            wave1MaxActive += increaseMaxActive;
            wave1TotalNum += increaseTotalNum;
            wave1SingleSpawnNum += increaseSingleSpawnNum;
            Global.ResetWave();
        }
    }

    // getters for EnemySpawner
    public int getWave1TotalNum()
    {
        return wave1TotalNum;
    }

    public int getWave1SingleSpawnNum()
    {
        return wave1SingleSpawnNum;
    }

    public int getWave1MaxActive()
    {
        return wave1MaxActive;
    }
}