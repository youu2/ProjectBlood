using UnityEngine;
using QFramework;
using Assets.Scripts.Game.System;

namespace ProjectBlood
{
	public partial class MapItem : ViewController
	{
		private Room room;
		// 在进入房间后更新数据,用于UIMap中链式调用
		public MapItem WithData(Room r)
		{
			room = r;
			UpdateView();
			return this;
		}
		// 更新房间图标,过道
		public void UpdateView()
		{
			// 先隐藏所有元素
			UpPath.Hide();
			RightPath.Hide();
			DownPath.Hide();
			LeftPath.Hide();
			InitBG.Hide();
			CurrentBG.Hide();
			HomeIcon.Hide();
			ChestIcon.Hide();
			ShopIcon.Hide();

			foreach(var dir in room.roomGenerateConfig.doorDirections)
			{
				// 小地图需要全方向生成
				if(dir == MapController.Direction.Up)
				{
					UpPath.Show();
				}
				if(dir == MapController.Direction.Right)
				{
					RightPath.Show();
				}
				if(dir == MapController.Direction.Down)
				{
					DownPath.Show();
				}
				if(dir == MapController.Direction.Left)
				{
					LeftPath.Show();
				}
			}
			if(room.roomState == Room.RoomState.Init)
			{
				InitBG.Show();
			}
			else if(room.roomConfig.roomType == RoomType.ChestRoom)
			{
				ChestIcon.Show();
			}
			else if(room.roomConfig.roomType == RoomType.ShopRoom)
			{
				ShopIcon.Show();
			}
			else if(room.roomConfig.roomType == RoomType.BossRoom)
			{
				ChestIcon.Show();	// 暂无boss专属Icon
			}

			if(room == Global.currentRoom)
			{
				CurrentBG.Show();
			}
			// 按照房间类型显示图标
			if(room.roomConfig.roomType == RoomType.InitRoom)
			{
				HomeIcon.Show();
			}
			
		}
	}
}
