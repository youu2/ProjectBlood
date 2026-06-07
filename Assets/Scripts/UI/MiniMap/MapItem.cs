using UnityEngine;
using QFramework;

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
			foreach(var dir in room.roomGenerateConfig.doorDirections)
			{
				// 只有右上是为了避免重复绘制过道
				if(dir == MapController.Direction.Up)
				{
					UpPath.Show();
				}
				if(dir == MapController.Direction.Right)
				{
					RightPath.Show();
				}
			}
		}
		void Start()
		{
			
		}
	}
}
