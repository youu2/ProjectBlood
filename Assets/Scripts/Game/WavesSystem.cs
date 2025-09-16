using System.Collections;
using System.Linq;
using ProjectBlood;
using UnityEngine;

public class WavesSystem : MonoBehaviour
{
    [SerializeField] private GameObject[] wave1EnemyPool;
    [SerializeField] private GameObject[] wave2EnemyPool;
    [SerializeField] private GameObject[] wave3EnemyPool;
    [SerializeField] public int maxWavesNum = 3;   // The total number of enemy waves generated
    [SerializeField] private int increaseTotalNum = 5;  // The increase in the total number of enemy generated between each two consecutive waves
    [SerializeField] private int increaseMaxActive = 3;  // The increase in the total number of enemy generated between each two consecutive waves
    // [SerializeField] private int maxActiveEnemiesNum = 5;  // The max number of active enemies at the same time, limite the density of enemies
    [SerializeField] private int increaseSingleSpawnNum = 2; // Increase the number of enemies generated in a single time by waves
    [SerializeField] private int wave1TotalNum = 15;  // The total number of enemies generated in first wave
    [SerializeField] private int wave1MaxActive = 8;  // The max number of active enemies at the same time in first wave, limite the density of enemies
    [SerializeField] private int wave1SingleSpawnNum = 3;  // The number of enemies generated at a single time
    [SerializeField] private int spawnInterval = 5;  // The interval between each enemy generation

    // The system can provide different enemy pools according to different waves.
    public GameObject[] SelectEnemiesByWaves()
    {
        int waveNum = Global.CurrentWaves.Value;
        if (waveNum == 1)
        {
            return wave1EnemyPool;
        }
        else if (waveNum == 2)
        {
            return wave2EnemyPool;
        }
        else if (waveNum == 3)
        {
            return wave3EnemyPool;
        }
        return null;
    }

    // increase the difficulty between waves automatically
    public void FinishWave()
    {
        if (Global.CurrentWaves.Value <= maxWavesNum)
        {
            wave1MaxActive += increaseMaxActive;
            wave1TotalNum += increaseTotalNum;
            wave1SingleSpawnNum += increaseSingleSpawnNum;
            Global.ResetWave();
        }

    }

    // getter
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