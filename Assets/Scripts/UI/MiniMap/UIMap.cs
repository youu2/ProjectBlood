using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    public partial class UIMap : ViewController
    {
        // 定义小地图显示的范围（玩家周围多少格房间）
        public int mapRange = 2; 
        private Room currentRoom = Global.currentRoom;
        int playerX;
        int playerY;
        void Update()
        {
            if (MapController.instance == null || MapController.instance.RoomGrid == null)
                return;
                
            MapRoot.DestroyChildren();
            
            // 获取玩家当前所在的房间
            currentRoom = Global.currentRoom;
            if (currentRoom == null || currentRoom.roomGenerateConfig == null) return;
                
            playerX = currentRoom.roomGenerateConfig.roomPosX;
            playerY = currentRoom.roomGenerateConfig.roomPosY;
            
            // 只绘制玩家周围 mapRange 范围内的房间
            MapController.instance.RoomGrid.ForEach((x, y, room) =>
            {
                // 检查房间是否在玩家周围的范围内
                if (Mathf.Abs(x - playerX) <= mapRange && Mathf.Abs(y - playerY) <= mapRange)
                {
                    // 只绘制已发现或已通关的房间
                    if (room.roomState != Room.RoomState.Unknown)
                    {
                        // 计算地图物品位置，以玩家为中心，物体往玩家反方向移动
                        float localX = (x - playerX) * 54f;
                        float localY = (y - playerY) * 54f;
                        
                        MapItem.InstantiateWithParent(MapRoot)
							.WithData(room)
                            .LocalPosition(localX, localY)
                            .Show();
                    }
                }
            });
        }
    }
}