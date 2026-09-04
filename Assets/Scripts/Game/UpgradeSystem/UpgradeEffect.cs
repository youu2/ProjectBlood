using System;
using UnityEngine;

namespace ProjectBlood
{
    // 强化效果大类
    public enum UpgradeEffectType
    {
        BaseStat,       // 基础属性加成（最大生命值/移动速度/弹药容量），可叠加 5 次
        WeaponDamage,   // 指定武器的独立伤害升级，可叠加 5 次，满级触发进化
        Passive,        // 全局被动机制解锁，不可重复选择
    }

    // 基础属性类型
    public enum StatType
    {
        MaxHP,          // 最大生命值
        MoveSpeed,      // 移动速度
        AmmoCapacity,   // 弹药容量
    }

    // 全局被动类型
    public enum PassiveType
    {
        SwitchWeaponBuff,   // 切换武器后短暂强化全武器伤害
        SingleWeaponRamp,   // 单武器持续输出时逐步增加伤害（上限30%，切枪重置）
    }

    // 可序列化的强化效果对象，作为 UpgradeSO 的内嵌数据。
    // 不同 effectType 使用下方对应的一组参数，未使用的参数留默认值即可。
    [Serializable]
    public class UpgradeEffect
    {
        public const int MaxStackCount = 5; // 所有可叠加属性（含武器伤害）的最大强化次数

        [Tooltip("效果大类，决定使用下方哪组参数")]
        public UpgradeEffectType effectType = UpgradeEffectType.BaseStat;

        [Header("基础属性加成参数 (BaseStat)")]
        public StatType statType = StatType.MaxHP;
        [Tooltip("每次强化增加的固定数值")]
        public float statValue = 10f;

        [Header("武器伤害升级参数 (WeaponDamage)")]
        public WeaponType weaponType = WeaponType.DE;
        [Tooltip("每次强化增加的伤害百分比, 0.1 = +10%")]
        public float damageBonusPerStack = 0.1f;

        [Header("被动解锁参数 (Passive)")]
        public PassiveType passiveType = PassiveType.SwitchWeaponBuff;

        // 判断该效果当前是否可被抽取：
        // BaseStat      -> 该属性叠加次数未满 5 次
        // WeaponDamage  -> 武器已拥有且该武器未满级
        // Passive       -> 该被动尚未解锁
        public bool IsAvailable()
        {
            switch (effectType)
            {
                case UpgradeEffectType.BaseStat:
                    return PlayerUpgradeState.GetStatStacks(statType) < MaxStackCount;
                case UpgradeEffectType.WeaponDamage:
                    return PlayerUpgradeState.IsWeaponOwned(weaponType)
                        && PlayerUpgradeState.GetWeaponLevel(weaponType) < MaxStackCount;
                case UpgradeEffectType.Passive:
                    return !PlayerUpgradeState.IsPassiveUnlocked(passiveType);
                default:
                    return false;
            }
        }
    }
}
