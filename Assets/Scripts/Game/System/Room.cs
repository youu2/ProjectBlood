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

		private void Update()
		{
			if(Time.frameCount % 30 == 0)
			{
				enemySet.RemoveWhere(enemy => !enemy);
			
				if (enemySet.Count == 0)
				{
					if(roomState == RoomState.Battle)
					{
						roomState = RoomState.Finished;
					}
					
					if(roomState == RoomState.Finished)
					{
						// 所有敌人死亡，开门
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
			if (other.CompareTag("Player") && roomConfig == RoomConfig.NormalRoom)
			{
				if(roomState != RoomState.Init)
				{
					return;
				}
				else if(roomState == RoomState.Init)
				{
					roomState = RoomState.Battle;
				}

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
