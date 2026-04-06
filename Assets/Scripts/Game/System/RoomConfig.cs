using System.Collections.Generic;

namespace ProjectBlood
{
    public class RoomConfig
    {
        public enum RoomType
        {
            InitRoom,
            NormalRoom,
            BossRoom,
        }

        /*
            初始地图设计：10x10格，边界为（'1'），内部地面（' '） 玩家（'P'） 敌人（'e'）
        */
        public static List<string> InitRoom{get ; set ;} = new List<string>()
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
        };

        public static List<string> NormalRoom{get ; set ;} = new List<string>()
        {
            "211111111111111112",
            "2                2",
            "2                2",
            "2     e    e     2",
            "2                2",
            "2       e        2",
            "2                2",
            "d       2        d",
            "d  e   222   e   d",
            "d       2        d",
            "2                2",
            "2                2",
            "2      222       2",
            "2                2",
            "2       e        2",
            "2                2",
            "2                2",
            "211111111111111112"
        };

        public static List<string> BossRoom{get ; set ;} = new List<string>()
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
        };
    }
}