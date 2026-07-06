using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace ProjectBlood
{
    public static class PlayerUpgrade
    {
        public static float DamageRatio = 1; // 玩家伤害倍率
        public static float AttackSpeedRatio = 1; // 玩家攻击速度倍率

        public static void UpgradeDamage()
        {
            DamageRatio += 1.5f;
        }
        public static void UpgradeAttackSpeed()
        {
            AttackSpeedRatio += 0.1f;
        }
        public static void UpgradeHP()
        {
            Global.INGAME_MAX_HP.Value *= 1.1f;
        }

        public static void ResetUpgrade()
        {
            DamageRatio = 1;
            AttackSpeedRatio = 1;
            Global.INGAME_MAX_HP.Value = Global.INIT_MAX_HP.Value;
        }
    }
}