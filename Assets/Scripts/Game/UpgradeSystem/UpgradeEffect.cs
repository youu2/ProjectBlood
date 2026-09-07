using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectBlood
{
    // 基础属性类型
    public enum StatType
    {
        MaxHP,              // 最大生命值
        MoveSpeed,          // 移动速度
        BloodBankCapacity,  // 血库容量上限
    }

    // 全局被动类型
    public enum PassiveType
    {
        SwitchWeaponBuff,   // 切换武器后短暂强化全武器伤害
        SingleWeaponRamp,   // 单武器持续输出时逐步增加伤害(上限30%,切枪重置)
    }

    // 单条基础属性效果,value 可为负(代价型强化)
    [Serializable]
    public class StatEffect
    {
        public StatType statType = StatType.MaxHP;
        [Tooltip("每次选择的变化值,可为负实现属性降低")]
        public float value = 3f;
    }

    // 单条武器伤害效果,weapons 为多选掩码(Inspector 勾选框),damageBonusPerStack 可为负
    [Serializable]
    public class WeaponDamageEffect
    {
        [Tooltip("目标武器(可勾选多把)")]
        public WeaponTypeFlags weapons = WeaponTypeFlags.DE;
        [Tooltip("每次选择增加的伤害比例,可为负(0.1 = +10%)")]
        public float damageBonusPerStack = 0.1f;
    }

    // 单条武器弹夹容量效果,weapons 为多选掩码,ammoBonusPerStack 可为负
    [Serializable]
    public class WeaponAmmoEffect
    {
        [Tooltip("目标武器(可勾选多把)")]
        public WeaponTypeFlags weapons = WeaponTypeFlags.DE;
        [Tooltip("每次选择的弹夹容量变化(发),可为负,幅度至少 1")]
        public int ammoBonusPerStack = 2;
    }

    // 组合强化效果：一个 UpgradeSO 内可同时配置多条不同大类、多个子类型的效果,
    // 数值可正可负；应用时按 stats -> weaponDamages -> weaponAmmos -> passives 顺序逐条生效。
    // 可用性判定为"全部条目都可用才可抽取"(全有或全无),避免出现半生效的组合强化。
    [Serializable]
    public class UpgradeEffect
    {
        public const int MaxStackCount = 5; // 所有可叠加属性(含武器伤害/弹夹)的最大强化次数

        [Tooltip("基础属性加成/降低(可多条,同一属性不可重复配置)")]
        public List<StatEffect> stats = new List<StatEffect>();

        [Tooltip("武器伤害升级(可多条,同一武器不可在多条中重复出现)")]
        public List<WeaponDamageEffect> weaponDamages = new List<WeaponDamageEffect>();

        [Tooltip("武器弹夹容量升级(可多条,同一武器不可在多条中重复出现)")]
        public List<WeaponAmmoEffect> weaponAmmos = new List<WeaponAmmoEffect>();

        [Tooltip("解锁的全局被动(同一被动不可重复)(后续可能放进宝箱)")]
        public List<PassiveType> passives = new List<PassiveType>();

        // 是否一条效果都没配置(无效配置)
        public bool IsEmpty
            => stats.Count == 0 && weaponDamages.Count == 0 && weaponAmmos.Count == 0 && passives.Count == 0;

        // 当前是否可被抽取：所有条目均满足各自条件才返回 true
        // BaseStat      -> 该属性叠加次数未满 5 次
        // WeaponDamage  -> 掩码中每把武器均已拥有且伤害升级未满 5 次
        // WeaponAmmo    -> 掩码中每把武器均已拥有且弹夹升级未满 5 次
        // Passive       -> 该被动尚未解锁
        public bool IsAvailable()
        {
            foreach (var stat in stats)
            {
                if (PlayerUpgradeState.GetStatStacks(stat.statType) >= MaxStackCount) return false;
            }
            foreach (var damage in weaponDamages)
            {
                foreach (var weaponType in damage.weapons.ToWeaponTypes())
                {
                    if (!PlayerUpgradeState.IsWeaponOwned(weaponType)
                        || PlayerUpgradeState.GetWeaponDamageLevel(weaponType) >= MaxStackCount) return false;
                }
            }
            foreach (var ammo in weaponAmmos)
            {
                foreach (var weaponType in ammo.weapons.ToWeaponTypes())
                {
                    if (!PlayerUpgradeState.IsWeaponOwned(weaponType)
                        || PlayerUpgradeState.GetWeaponAmmoLevel(weaponType) >= MaxStackCount) return false;
                }
            }
            foreach (var passive in passives)
            {
                if (PlayerUpgradeState.IsPassiveUnlocked(passive)) return false;
            }
            return true;
        }

        // 配置校验：把发现的问题追加到 errors,返回是否通过(供编辑期 OnValidate 与运行期拦截共用)。
        // 校验规则：不允许空配置、0 值(无效果)、同列表内重复子类型(重复叠加会造成配置歧义)。
        public bool Validate(List<string> errors)
        {
            if (IsEmpty) errors.Add("未配置任何效果");

            var statTypes = new HashSet<StatType>();
            foreach (var stat in stats)
            {
                if (stat.value == 0f) errors.Add($"属性 {GetStatName(stat.statType)} 变化值为 0(无效果)");
                if (!statTypes.Add(stat.statType)) errors.Add($"属性 {GetStatName(stat.statType)} 重复配置");
            }

            var damageWeapons = new HashSet<WeaponType>();
            foreach (var damage in weaponDamages)
            {
                if (damage.weapons == WeaponTypeFlags.None) errors.Add("武器伤害条目未勾选任何武器");
                if (damage.damageBonusPerStack == 0f) errors.Add("武器伤害条目数值为 0(无效果)");
                foreach (var weaponType in damage.weapons.ToWeaponTypes())
                {
                    if (!damageWeapons.Add(weaponType)) errors.Add($"武器 {weaponType} 在多条伤害条目中重复");
                }
            }

            var ammoWeapons = new HashSet<WeaponType>();
            foreach (var ammo in weaponAmmos)
            {
                if (ammo.weapons == WeaponTypeFlags.None) errors.Add("弹夹容量条目未勾选任何武器");
                if (ammo.ammoBonusPerStack == 0) errors.Add("弹夹容量条目数值为 0(无效果)");
                foreach (var weaponType in ammo.weapons.ToWeaponTypes())
                {
                    if (!ammoWeapons.Add(weaponType)) errors.Add($"武器 {weaponType} 在多条弹夹条目中重复");
                }
            }

            if (passives.Count != passives.Distinct().Count()) errors.Add("同一被动重复配置");
            return errors.Count == 0;
        }

        // 整数量化：四舍五入且幅度至少 1,保留符号(血库/弹夹容量应用逻辑共用)
        public static int GetSignedInt(float value)
        {
            int magnitude = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(value)));
            return value >= 0f ? magnitude : -magnitude;
        }

        // ============================== 私有工具 ==============================

        private static string GetStatName(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHP: return "最大生命值";
                case StatType.MoveSpeed: return "移动速度";
                case StatType.BloodBankCapacity: return "血库容量";
                default: return type.ToString();
            }
        }
    }
}
