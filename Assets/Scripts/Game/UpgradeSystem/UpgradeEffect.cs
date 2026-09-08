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
        // 系统满级标准(固定):武器伤害"全局累计"5 级触发进化(IWeaponEvolution),IsXxxMaxed 查询也以此为准
        public const int MaxStackCount = 5;

        // 本卡可被选择次数上限(可配置):按卡牌独立计数,本卡被选择达到该次数后自动从随机池移除。
        // 计数维度是"这张卡",不是属性/武器:多张影响同一属性/武器的卡互不影响
        // (例如两张 MaxUpgradeCount=5 的生命卡,生命值最多可被修改 10 次)。
        // 与 MaxStackCount 解耦:武器全局等级满 5 级正常进化,之后只要本卡还有次数就继续出现叠加。
        [Tooltip("本强化卡最多可被选择的次数(按卡独立计数,多张卡互不影响),达到后自动从随机抽取池移除(范围 1-99,默认 5)")]
        [Range(1, 99)]
        public int MaxUpgradeCount = MaxStackCount;

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

        // 条目级可用性(与卡牌身份无关):所有条目均满足才返回 true
        // BaseStat      -> 无运行时限制(属性总能被改,选择次数上限由 UpgradeManager 按 MaxUpgradeCount 控制)
        // WeaponDamage  -> 掩码中每把武器均已拥有
        // WeaponAmmo    -> 掩码中每把武器均已拥有
        // Passive       -> 该被动尚未解锁
        // 注:各属性/武器的全局累计次数不在此判断;每张卡"能被选几次"由 UpgradeManager 查
        //     PlayerUpgradeState.GetUpgradeUsageCount(本卡) 与 MaxUpgradeCount 比较得出,故多张卡互不影响。
        public bool IsAvailable()
        {
            foreach (var damage in weaponDamages)
            {
                foreach (var weaponType in damage.weapons.ToWeaponTypes())
                {
                    if (!PlayerUpgradeState.IsWeaponOwned(weaponType)) return false;
                }
            }
            foreach (var ammo in weaponAmmos)
            {
                foreach (var weaponType in ammo.weapons.ToWeaponTypes())
                {
                    if (!PlayerUpgradeState.IsWeaponOwned(weaponType)) return false;
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
            if (MaxUpgradeCount < 1) errors.Add("MaxUpgradeCount 必须 >= 1(当前为 " + MaxUpgradeCount + ")");

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

        // 编辑期兜底修正：[Range] 特性只约束 Inspector 拖滑条，直接改序列化文件或旧数据可能越界，
        // 这里强制夹到合法区间，保证不会出现 0/负数导致强化永不退场逻辑异常
        public void ClampValid()
        {
            MaxUpgradeCount = Mathf.Clamp(MaxUpgradeCount, 1, 99);
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
