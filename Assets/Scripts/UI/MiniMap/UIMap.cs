using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class UIMap : ViewController
    {
        // 定义小地图显示的范围（玩家周围多少格房间）
        public int mapRange = 2;

        void OnEnable()
        {
            // 订阅玩家进入房间事件
            Room.OnPlayerEnteredRoom += DrawMap;
        }

        void OnDisable()
        {
            Room.OnPlayerEnteredRoom -= DrawMap;
        }

        void Start()
        {
            // 场景加载时先绘制一遍（出生房的触发器事件由物理回合触发，可能晚于首帧）
            DrawMap(Global.currentRoom);
        }

        // 玩家进入房间时重绘一遍小地图
        private void DrawMap(Room enteredRoom)
        {
            if (MapController.instance == null || MapController.instance.RoomGrid == null)
                return;

            MapRoot.DestroyChildren();

            // 获取玩家当前所在的房间
            var currentRoom = Global.currentRoom;
            if (currentRoom == null || currentRoom.roomGenerateConfig == null) return;

            int playerX = currentRoom.roomGenerateConfig.roomPosX;
            int playerY = currentRoom.roomGenerateConfig.roomPosY;

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
