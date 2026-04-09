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
            BossRoom,
        }
    public class RoomConfig
    {
        public RoomType roomType;
        public List<string> roomMap;
        
        /*
            地图：18x18格，边界为（'1'和‘2’）房门为（'d'）内部地面（' '） 玩家（'P'） 敌人（'e'） 传送门（'#'）
        */
        public static RoomConfig InitRoom = new RoomConfig()
        {
            roomType = RoomType.InitRoom,
            roomMap = new List<string>()
            {
                "211111111111111112",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                d",
                "2                d",
                "2                d",
                "2                2",
                "2                2",
                "2        P       2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "211111111111111112"
            }
        };

        public static List<RoomConfig> normalRoomConfigList = new List<RoomConfig>()
        {
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "211111111111111112",
                    "2                2",
                    "2                2",
                    "2     e    e     2",
                    "2                2",
                    "2  e    e    e   2",
                    "2                2",
                    "d       2        d",
                    "d    e 222 e     d",
                    "d       2        d",
                    "2                2",
                    "2    e     e     2",
                    "2      222       2",
                    "2                2",
                    "2       e        2",
                    "2  e          e  2",
                    "2                2",
                    "211111111111111112"
                }
            },
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "211111111111111112",
                    "2                2",
                    "2   e        e   2",
                    "2       e        2",
                    "2    e     e     2",
                    "2   222   222    2",
                    "2    2     2     2",
                    "d    2  e  2     d",
                    "d    2     2     d",
                    "d    e     e     d",
                    "2       e        2",
                    "2      222       2",
                    "2     2   2      2",
                    "2       e        2",
                    "2    e     e     2",
                    "2       e        2",
                    "2                2",
                    "211111111111111112"
                }
            },
            new RoomConfig()
            {
                roomType = RoomType.NormalRoom,
                roomMap = new List<string>()
                {
                    "211111111111111112",
                    "2                2",
                    "2       e        2",
                    "2   22      22   2",
                    "2   2        2   2",
                    "2       e        2",
                    "2                2",
                    "d        e       d",
                    "d  e   e22    e  d",
                    "d       22e      d",
                    "2       e        2",
                    "2                2",
                    "2  e    e    e   2",
                    "2  2e       e2   2",
                    "2  22e     e22   2",
                    "2       e        2",
                    "2                2",
                    "211111111111111112"
                }
            }
        };

        // 整合进normalRoomConfigList
        // public static RoomConfig NormalRoom = new RoomConfig()
        // {
        //     roomType = RoomType.NormalRoom,
        //     roomMap = new List<string>()
        //     {
        //         "211111111111111112",
        //         "2                2",
        //         "2                2",
        //         "2     e    e     2",
        //         "2                2",
        //         "2       e        2",
        //         "2                2",
        //         "d       2        d",
        //         "d  e   222   e   d",
        //         "d       2        d",
        //         "2                2",
        //         "2                2",
        //         "2      222       2",
        //         "2                2",
        //         "2       e        2",
        //         "2                2",
        //         "2                2",
        //         "211111111111111112"
        //     }
        // };

        public static RoomConfig BossRoom = new RoomConfig()
        {
            roomType = RoomType.BossRoom,
            roomMap = new List<string>()
            {
                "211111111111111112",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2       #        2",
                "2                2",
                "d                2",
                "d                2",
                "d                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "2                2",
                "211111111111111112"
            }
        };
    }
}