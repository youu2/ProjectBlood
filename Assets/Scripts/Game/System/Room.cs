using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.System;

namespace ProjectBlood
{
	public partial class Room : ViewController
	{
		private List<Vector3> enemyPosList = new List<Vector3>();
		private List<Door> doorList = new List<Door>();
		public RoomConfig roomConfig {get ; private set ;}
		private HashSet<IDamageable> enemySet = new HashSet<IDamageable>();
		public RoomState roomState = RoomState.Unknown;
		public MapController.RoomGenerateConfig roomGenerateConfig {get ; private set ;}
		private List<EnemyWaveConfig> enemyWaveConfigList = new List<EnemyWaveConfig>();
		private EnemyWaveConfig currentEnemyWaveConfig = null;
		
		public enum RoomState
		{
			Unknown,	// 玩家探测范围外的房间，不会在小地图显示
			Init,	// 玩家观测到的房间，但是没进入过
			Battle,	
			Finished,	// 战斗通关的房间
			Idle,	// 无强制性战斗的房间，可拾取物不会飞向玩家
		}
		public Room WithRoomConfig(RoomConfig roomConfig)
		{
			this.roomConfig = roomConfig;
			
			// 在设置 roomConfig 后立即初始化敌人波次配置
			if(roomConfig.roomType == RoomType.NormalRoom)
			{
				var wavesCount = Random.Range(1,4);
				for (int i = 0; i < wavesCount; i++)
				{
					enemyWaveConfigList.Add(new EnemyWaveConfig());
				}
			}
			
			return this;
		}
		public Room WithRoomGenerateConfig(MapController.RoomGenerateConfig roomGenerateConfig)
		{
			this.roomGenerateConfig = roomGenerateConfig;
			return this;
		}

		void GenerateEnemy()
		{
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
				var enemyToGen = RandomUtility.Choose(
					MapController.instance.Enemy1,
					MapController.instance.Enemy2,
					MapController.instance.Enemy3,
					MapController.instance.Enemy4
					);
				var enemyObj = Instantiate(enemyToGen);
				enemyObj.transform.position = pos2Gen[i];
				var enemy = enemyObj.GetComponent<IDamageable>();
				enemy.Room = this;
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
				enemySet.RemoveWhere(enemy => enemy.IsDying);
			
				if (enemySet.Count == 0 && roomState == RoomState.Battle)
				{
					// 所有敌人死亡后，生成下一批敌人
					if(enemyWaveConfigList.Count > 0)
					{
						GenerateEnemy();
					}else{
						roomState = RoomState.Finished;
						AudioKitManager.Instance.PlayOneShot("DoorOpeningSfx");
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

		public static void FindRoom()
        {
			if (Global.currentRoom == null || Global.currentRoom.roomGenerateConfig == null)
			{
				return;
			}
			
            MapController.instance.RoomGrid.ForEach((x, y, room) =>
            {
				if (room == null || room.roomGenerateConfig == null)
				{
					return;
				}
				
                var playerX = Global.currentRoom.roomGenerateConfig.roomPosX;
                var playerY = Global.currentRoom.roomGenerateConfig.roomPosY;
                // 玩家会发现当前房间的相邻房间
                if (room.roomState == Room.RoomState.Unknown
				&& IsAdjacentRoom(Global.currentRoom, x, y, playerX, playerY))
                {
                    room.roomState = Room.RoomState.Init;
                }
            });
        }

		private static bool IsAdjacentRoom(Room room, int x, int y, int playerX, int playerY)
		{
			var gapX = x - playerX;
			var gapY = y - playerY;
			if((gapX == 1 && gapY == 0 && HasThisDoor(room, MapController.Direction.Right))
			|| (gapX == -1 && gapY == 0 && HasThisDoor(room, MapController.Direction.Left))
			|| (gapX == 0 && gapY == 1 && HasThisDoor(room, MapController.Direction.Up))
			|| (gapX == 0 && gapY == -1 && HasThisDoor(room, MapController.Direction.Down)))
			{
				return true;
			}
			return false;
		} 

		private static bool HasThisDoor(Room room, MapController.Direction dir)
		{
			if (room == null || room.doorList == null)
			{
				return false;
			}
			
			foreach (var door in room.doorList)
			{
				if (door != null && door.direction == dir)
				{
					return true;
				}
			}
			return false;
		}

		private MapController.Direction DirOfRoom()
		{
			return MapController.Direction.Down;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.CompareTag("Player"))
			{
				return;
			}

			if (roomConfig == null || roomGenerateConfig == null)
			{
				return;
			}

			Global.currentRoom = this;	// 非战斗房间也要更新currentRoom

			if (roomState == RoomState.Unknown)
			{
				roomState = RoomState.Init;
			}

			FindRoom();	// 非战斗房间也要更新周围房间

			if (roomConfig.roomType == RoomType.NormalRoom)
			{
				if (roomState == RoomState.Init)
				{
					roomState = RoomState.Battle;
					GenerateEnemy();
					if (doorList != null)
					{
						foreach (var door in doorList)
						{
							if (door != null)
							{
								door.Show();
							}
						}
					}
					AudioKitManager.Instance.PlayOneShot("DoorClosingSfx");
				}
			}
			else
			{
				roomState = RoomState.Idle;
			}
		}

		public void AddDoor(Door door)
		{
			doorList.Add(door);
		}

		public HashSet<IDamageable> GetEnemies()
		{
			return enemySet;
		}
	}
}
