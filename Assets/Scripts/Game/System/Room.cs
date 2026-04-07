using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System;

namespace ProjectBlood
{
	public partial class Room : ViewController
	{
		private List<Vector3> enemyPosList = new List<Vector3>();
		private List<Door> doorList = new List<Door>();
		public RoomConfig roomConfig {get ; private set ;}
		private HashSet<Enemy> enemySet = new HashSet<Enemy>();
		public RoomState roomState = RoomState.Init;
		private List<EnemyWaveConfig> enemyWaveConfigList = new List<EnemyWaveConfig>()
		{
			new EnemyWaveConfig(),
			new EnemyWaveConfig(),
			new EnemyWaveConfig(),
		};
		private EnemyWaveConfig currentEnemyWaveConfig = null;
		
		public enum RoomState
		{
			Init,
			Battle,
			Finished,
		}
		public Room WithRoomConfig(RoomConfig roomConfig)
		{
			this.roomConfig = roomConfig;
			return this;
		}
	
		void Start()
		{
			// Code Here
		}

		void GenerateEnemy()
		{
			// if(currentEnemyWaveConfig == null)
			// {
			// 	return;
			// }
			
			enemyWaveConfigList.RemoveAt(0);
			foreach (var enemyPos in enemyPosList)
			{
				var enemyObj = Instantiate(MapController.instance.Enemy);
				enemyObj.transform.position = enemyPos;
				var enemy = enemyObj.GetComponent<Enemy>();
				if (enemy != null)
				{
					enemySet.Add(enemy);
				}
			}
		}

		private void Update()
		{
			if(Time.frameCount % 30 == 0)
			{
				enemySet.RemoveWhere(enemy => !enemy);
			
				if (enemySet.Count == 0 && roomState == RoomState.Battle)
				{
					if(enemyWaveConfigList.Count > 0)
					{
						GenerateEnemy();
					}else{
						roomState = RoomState.Finished;
						foreach (var door in doorList)
						{
							door.Hide();
						}
						return;
					}
				}
			}
		}
		
		public void AddEnemy(Vector3 enemyPos)
		{
			enemyPosList.Add(enemyPos);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			// 玩家进入房间，生成敌人,关门
			if (other.CompareTag("Player") && roomConfig.roomType == RoomType.NormalRoom)
			{
				if(roomState != RoomState.Init)
				{
					return;
				}
				else if(roomState == RoomState.Init)
				{
					roomState = RoomState.Battle;
				}

				GenerateEnemy();
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
