using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Room : ViewController
	{
		private List<Vector3> enemyPosList = new List<Vector3>();
		private List<Door> doorList = new List<Door>();
		void Start()
		{
			// Code Here
		}
		public void AddEnemy(Vector3 enemyPos)
		{
			enemyPosList.Add(enemyPos);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.CompareTag("Player"))
			{
				foreach (var enemyPos in enemyPosList)
				{
					var enemy = Instantiate(MapController.instance.Enemy);
					enemy.transform.position = enemyPos;
				}
				foreach (var door in doorList)
				{
					door.Show();
				}
			}
		}

		public void AddDoor(Door door)
		{
			doorList.Add(door);
		}
	}
}
