using System;
using System.Collections.Generic;

namespace ProjectBlood
{
    public class EnemyWaveConfig
    {
        
    }
    public enum RoomType
    {
        InitRoom,
        NormalRoom,
        ChestRoom,
        BossRoom,
    }

    public class RoomNode
    {
        public RoomType roomType = RoomType.InitRoom;
        public List<RoomNode> Children = new List<RoomNode>();

        public RoomNode(RoomType roomType)
        {
            this.roomType = roomType;
        }

        public RoomNode NextRoom(RoomType roomType, Action<RoomNode> branch = null)
        {
            var roomNode = new RoomNode(roomType);
            Children.Add(roomNode);
            branch?.Invoke(roomNode);
            return roomNode;
        }
    }

    public class RoomConfig
    {
        public RoomType roomType;
        public List<string> roomMap;
        
        /*
            地图：18x18格，边界为（'1'和‘2’）房门为（'d'）内部地面（' '） 玩家（'P'） 敌人（'e'） 传送门（'#'） 宝箱（'C'）
        */
        public static RoomConfig InitRoom = new RoomConfig()
        {
            roomType = RoomType.InitRoom,
            roomMap = new List<string>()
            {
                "21111111ddd11111112",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "d                 d",
                "d                 d",
                "d                 d",
                "2                 2",
                "2                 2",
                "2        P        2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "21111111ddd11111112"
            }
        };

        public static List<RoomConfig> normalRoomConfigList = new List<RoomConfig>()
        {
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "21111111ddd11111112",
                    "2                 2",
                    "2                 2",
                    "2                 2",
                    "2      e    e     2",
                    "2                 2",
                    "2   e    e    e   2",
                    "2                 2",
                    "d        2        d",
                    "d     e 222 e     d",
                    "d        2        d",
                    "2                 2",
                    "2     e     e     2",
                    "2       222       2",
                    "2                 2",
                    "2        e        2",
                    "2   e          e  2",
                    "2                 2",
                    "21111111ddd11111112"
                }
            },
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "21111111ddd11111112",
                    "2                 2",
                    "2    e   e   e    2",
                    "2    e       e    2",
                    "2    e       e    2",
                    "2                 2",
                    "2    222   222    2",
                    "2     2     2     2",
                    "d     2     2     d",
                    "d     2     2     d",
                    "d        e        d",
                    "2     e     e     2",
                    "2       222       2",
                    "2      2   2      2",
                    "2        e        2",
                    "2     e     e     2",
                    "2        e        2",
                    "2                 2",
                    "21111111ddd11111112"
                }
            },
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "21111111ddd11111112",
                    "2                 2",
                    "2   e         e   2",
                    "2   22       22   2",
                    "2   2    e    2   2",
                    "2                 2",
                    "2        e        2",
                    "2                 2",
                    "d       222       d",
                    "d       222       d",
                    "d       222       d",
                    "2        e        2",
                    "2   e         e   2",
                    "2        e        2",
                    "2   2    e    2   2",
                    "2   22       22   2",
                    "2   e    e    e   2",
                    "2                 2",
                    "21111111ddd11111112"
                }
            }
        };

        public static RoomConfig ChestRoom = new RoomConfig()
        {
            roomType = RoomType.ChestRoom,
            roomMap = new List<string>()
            {
                "21111111ddd11111112",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "d                 d",
                "d        c        d",
                "d                 d",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "21111111ddd11111112"
            }
        };

        public static RoomConfig BossRoom = new RoomConfig()
        {
            roomType = RoomType.BossRoom,
            roomMap = new List<string>()
            {
                "21111111ddd11111112",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2        #        2",
                "2                 2",
                "d                 d",
                "d                 d",
                "d                 d",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "2                 2",
                "21111111ddd11111112"
            }
        };
    }
}