using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBlood
{
	public partial class Room : ViewController
	{
		private List<Vector3> enemyPosList = new List<Vector3>();
		private List<Door> doorList = new List<Door>();
		public RoomConfig roomConfig {get ; private set ;}
		private HashSet<Enemy> enemySet = new HashSet<Enemy>();
		public RoomState roomState = RoomState.Init;
		public MapController.RoomGenerateConfig roomGenerateConfig {get ; private set ;}
		private List<EnemyWaveConfig> enemyWaveConfigList = new List<EnemyWaveConfig>()
		{
			// 敌人波次配置列表
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
		public Room WithRoomGenerateConfig(MapController.RoomGenerateConfig roomGenerateConfig)
		{
			this.roomGenerateConfig = roomGenerateConfig;
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
			// 每次生成3-6个敌人
			var enemyCount = Random.Range(3,6);
			// 按照离玩家的距离排序enemyPosList，距离玩家远的敌人优先生成
			var pos2Gen = enemyPosList
				.OrderByDescending(pos => (Player.player1.Position2D() - pos.ToVector2()).magnitude)
				.Take(enemyCount).ToList();

			// 生成并记录所有生成的敌人
			for (int i = 0; i < enemyCount; i++)
			{
				var enemyObj = Instantiate(MapController.instance.Enemy);
				enemyObj.transform.position = pos2Gen[i];
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
					// 所有敌人死亡后，生成下一批敌人
					if(enemyWaveConfigList.Count > 0)
					{
						GenerateEnemy();
					}else{
						roomState = RoomState.Finished;
						AudioKit.PlaySound("DoorOpeningSfx");
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
				AudioKit.PlaySound("DoorClosingSfx");
			}
		}

		public void AddDoor(Door door)
		{
			doorList.Add(door);
		}
	}
}
