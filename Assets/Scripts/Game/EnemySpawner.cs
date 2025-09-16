// reference from my anothor project abyss(MIGA)
using System.Collections;
using System.Linq;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
	// [SerializeField] private GameObject[] normalEnemies;
	[SerializeField] private WavesSystem waves;
	[SerializeField] private Transform player;
	[SerializeField] private float maxSpawnRadius = 50f;
	[SerializeField] private float minSpawnRadius = 20f;
	[SerializeField] private float enemySpawnInterval = 15f;
	[SerializeField] private int enemyNum; // The number of normal enemies generated once
	//private int maxCumulativeNum; // limitation of enemy generated in one wave
	//private int cumulativeNum;  // cumulative number of generated enemies so far
	private int maxCurrentNum;  // limit the num of active enemies 
	private int totalNum;	// The total number of enemies generated
	// private static int currentNum;      // current number of active enemies

	private void Start()
	{
		StartCoroutine(SpawnEnemies());
	}

	private IEnumerator SpawnEnemies()
	{
		if (player)
		{
			while (true)
			{
				// Wait for the specified interval before spawning
				yield return new WaitForSeconds(enemySpawnInterval);

				maxCurrentNum = waves.getWave1MaxActive();
				enemyNum = waves.getWave1SingleSpawnNum();
				totalNum = waves.getWave1TotalNum();
				for (int i = 0; i < enemyNum; i++)
				{
					if (Global.currentNum.Value < maxCurrentNum && Global.cumulativeNum.Value < totalNum)
					{
						Debug.Log("current waves: " + Global.CurrentWaves.Value);
						GameObject[] selectedEnemyArray = waves.SelectEnemiesByWaves();
						GameObject enemyToSpawn = selectedEnemyArray[Random.Range(0, selectedEnemyArray.Length)];

						// Generate a random position within the specified radius range
						Vector2 spawnPosition = GetValidSpawnPosition();

						if (enemyToSpawn != null)
						{
							Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
							Global.currentNum.Value += 1;
							//cumulativeNum += 1;
							Global.cumulativeNum.Value += 1;
						}
						else
						{
							Debug.LogWarning("Enemy to spawn is null.");
						}
					}
				}

			}
		}
	}


	// Get a random valid spawn position within the specified min and max radius
	private Vector2 GetValidSpawnPosition()
	{
		if (player)
		{
			float distance;
			// Generate a random angle in radians
			float angle = Random.Range(0f, Mathf.PI * 2);

			// Generate a random distance between the min and max radius
			distance = Random.Range(minSpawnRadius, maxSpawnRadius);


			// Convert polar coordinates to Cartesian coordinates
			Vector2 randomPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
			return new Vector2(player.position.x + randomPos.x, player.position.y + randomPos.y);
		}
		else return new Vector2();
	}

}