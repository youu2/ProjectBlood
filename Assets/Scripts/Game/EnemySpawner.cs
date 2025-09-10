// reference from my anothor project abyss(MIGA)
using System.Collections;
using System.Linq;
using UnityEngine;

public class enemySpawner : MonoBehaviour
{
	[SerializeField] private GameObject[] normalEnemies;
	[SerializeField] private Transform player;
	[SerializeField] private float maxSpawnRadius = 50f;
	[SerializeField] private float minSpawnRadius = 20f;
	[SerializeField] private float enemySpawnInterval = 15f;
	[SerializeField] private float EnemyNum = 3f; // The number of normal enemies generated once


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

				for (int i = 0; i <= EnemyNum; i++)
				{
					GameObject enemyToSpawn = normalEnemies[Random.Range(0, normalEnemies.Length)];

					// Generate a random position within the specified radius range
					Vector2 spawnPosition = GetValidSpawnPosition();

					if (enemyToSpawn != null)
					{
						Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
					}
					else
					{
						Debug.LogWarning("Enemy to spawn is null.");
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