using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QFramework;
using Unity.Collections;

namespace ProjectBlood
{
    public class LevelGenHelper
    {
        public static List<MapController.Direction> GetValidDirections(int roomPosX, int roomPosY, 
                                        DynaGrid<MapController.RoomGenerateConfig> dynamicDoorLayout)
        {
            // 获取可以延申的方向
            List<MapController.Direction> validDirections = new List<MapController.Direction>();
            // 检查四个方向是否有空房间
            if(dynamicDoorLayout[roomPosX + 1, roomPosY] == null)// 右方  
            {
                validDirections.Add(MapController.Direction.Right);
            }
            if(dynamicDoorLayout[roomPosX - 1, roomPosY] == null)// 左方
            {
                validDirections.Add(MapController.Direction.Left);
            }
            if(dynamicDoorLayout[roomPosX, roomPosY + 1] == null)// 上方
            {
                validDirections.Add(MapController.Direction.Up);
            }
            if(dynamicDoorLayout[roomPosX, roomPosY - 1] == null)// 下方
            {
                validDirections.Add(MapController.Direction.Down);
            }
            return validDirections;
        }

        // 用于记录 【当前房间的可选生成方向】 的进一步预测可选方向数量
        public class DirectionWithCount
        {
            public MapController.Direction Direction; // 当前房间的其中一个可选生成方向
            public int Count; // 该方向进一步预测可选方向数量
        }

        public static List<DirectionWithCount> PredictDirectionWithCount
        (int roomPosX, int roomPosY, DynaGrid<MapController.RoomGenerateConfig> dynamicDoorLayout)
        {
            var validDirections = GetValidDirections(roomPosX, roomPosY, dynamicDoorLayout);

            var directionsWithCount = new List<DirectionWithCount>();

            if (validDirections.Count == 0) 
            {
                // Debug.LogError("没有可以延申的方向");
                return directionsWithCount;
            }
            foreach(var direction in validDirections)
            {
                if(direction == MapController.Direction.Right)
                {
                    var rightRoomValidDirections = GetValidDirections(roomPosX + 1, roomPosY, dynamicDoorLayout);
                    directionsWithCount.Add(new DirectionWithCount { Direction = direction, 
                    Count = rightRoomValidDirections.Count });
                }
                else if(direction == MapController.Direction.Left)
                {
                    var leftRoomValidDirections = GetValidDirections(roomPosX - 1, roomPosY, dynamicDoorLayout);
                    directionsWithCount.Add(new DirectionWithCount { Direction = direction, 
                    Count = leftRoomValidDirections.Count });
                }
                else if(direction == MapController.Direction.Up)
                {
                    var upRoomValidDirections = GetValidDirections(roomPosX, roomPosY + 1, dynamicDoorLayout);
                    directionsWithCount.Add(new DirectionWithCount { Direction = direction, 
                    Count = upRoomValidDirections.Count });
                }
                else if(direction == MapController.Direction.Down)
                {
                    var downRoomValidDirections = GetValidDirections(roomPosX, roomPosY - 1, dynamicDoorLayout);
                    directionsWithCount.Add(new DirectionWithCount { Direction = direction, 
                    Count = downRoomValidDirections.Count });
                }
            }


            return directionsWithCount;
        }
    }
}