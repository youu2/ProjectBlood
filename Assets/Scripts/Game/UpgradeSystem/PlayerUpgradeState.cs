using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 玩家强化状态（静态，与旧 PlayerUpgrade 的访问方式保持一致）。
    // 职责：记录基础属性叠加次数、各武器等级与伤害系数、已拥有武器、已解锁被动；
    //       所有强化效果的应用（Apply*）与伤害计算查询（GetFinalDamageRatio）都统一走这里。
    // 生命周期：单局有效，Global.ResetLevel() 时调用 Reset() 清空。
    public static class PlayerUpgradeState
    {
        public const int MaxStackCount = UpgradeEffect.MaxStackCount; // 所有可叠加属性最多强化 5 次

        // ---- 基础属性叠加次数 ----
        private static readonly Dictionary<StatType, int> statStacks = new Dictionary<StatType, int>();
        // ---- 武器强化等级（0 = 未强化，5 = 满级）----
        private static readonly Dictionary<WeaponType, int> weaponLevels = new Dictionary<WeaponType, int>();
        // ---- 武器伤害系数（1.0 = 100%，每次强化累加 bonusPerStack）----
        private static readonly Dictionary<WeaponType, float> weaponDamageRatios = new Dictionary<WeaponType, float>();
        // ---- 已解锁的全局被动 ----
        private static readonly HashSet<PassiveType> unlockedPassives = new HashSet<PassiveType>();

        // 全局伤害倍率（迁移自旧 PlayerUpgrade.DamageRatio，默认 1.0 不影响原有伤害）
        public static float GlobalDamageRatio { get; private set; } = 1f;

        // ============ 被动运行时状态（数值为占位默认值，后续平衡阶段调整）============

        // 切枪增益：切换武器后短时间内全武器伤害提升
        private const float SwitchBuffDuration = 3f;       // TODO: 切枪增益持续时间（秒）
        private const float SwitchBuffMultiplier = 1.5f;   // TODO: 切枪增益伤害倍率
        private static float switchBuffTimer;

        // 单武器持续输出叠加：连续使用同一把武器射击时伤害逐步提升，上限 30%，切枪重置
        private const float RampPerShot = 0.02f;           // TODO: 每开一枪增加的伤害比例
        private const float RampMax = 0.30f;               // 上限 30%
        private static float singleWeaponRamp;
        private static WeaponType currentWeaponType = WeaponType.None;

        private static bool initialized;

        // 订阅武器开火事件（由 Global.Initialize 在启动时调用一次）
        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            WeaponBase.OnWeaponFired += OnWeaponFired;
        }

        // 武器开火回调：单武器持续输出叠加的累加入口
        private static void OnWeaponFired(WeaponBase weapon)
        {
            if (IsPassiveUnlocked(PassiveType.SingleWeaponRamp)
                && Player.player1 != null
                && weapon == Player.player1.currentWeapon)
            {
                singleWeaponRamp = Mathf.Min(RampMax, singleWeaponRamp + RampPerShot);
            }
        }

        // 被动计时，由 Player.Update 驱动（暂停时 Time.deltaTime 为 0，不会误走时）
        public static void TickPassives(float deltaTime)
        {
            if (switchBuffTimer > 0f)
            {
                switchBuffTimer = Mathf.Max(0f, switchBuffTimer - deltaTime);
            }
        }

        // 切枪钩子，由 Player.UseWeapon 调用：重置单武器叠加，并尝试激活切枪增益
        public static void OnWeaponSwitched(WeaponType weaponType)
        {
            currentWeaponType = weaponType;
            singleWeaponRamp = 0f; // 切枪后重置单武器伤害叠加
            if (IsPassiveUnlocked(PassiveType.SwitchWeaponBuff))
            {
                switchBuffTimer = SwitchBuffDuration;
            }
        }

        // ============================== 查询 ==============================

        public static int GetStatStacks(StatType type)
            => statStacks.TryGetValue(type, out int v) ? v : 0;

        public static bool IsStatMaxed(StatType type)
            => GetStatStacks(type) >= MaxStackCount;

        public static int GetWeaponLevel(WeaponType type)
            => weaponLevels.TryGetValue(type, out int v) ? v : 0;

        public static bool IsWeaponMaxed(WeaponType type)
            => GetWeaponLevel(type) >= MaxStackCount;

        public static float GetWeaponDamageRatio(WeaponType type)
            => weaponDamageRatios.TryGetValue(type, out float v) ? v : 1f;

        public static bool IsPassiveUnlocked(PassiveType type)
            => unlockedPassives.Contains(type);

        // 已拥有武器以 WeaponDataSystem.weaponDataList（宝箱解锁）为准
        public static bool IsWeaponOwned(WeaponType type)
        {
            if (type == WeaponType.None) return false;
            string weaponName = type.ToWeaponName();
            for (int i = 0; i < WeaponDataSystem.weaponDataList.Count; i++)
            {
                if (WeaponDataSystem.weaponDataList[i].weaponName == weaponName) return true;
            }
            return false;
        }

        // 伤害计算统一入口（PlayerBullet / Laser 命中时调用）：
        // 最终倍率 = 全局倍率 × 该武器独立系数 × 切枪增益 × 单武器持续叠加
        public static float GetFinalDamageRatio(WeaponType weaponType)
        {
            float ratio = GlobalDamageRatio * GetWeaponDamageRatio(weaponType);
            if (switchBuffTimer > 0f)
            {
                ratio *= SwitchBuffMultiplier;
            }
            if (IsPassiveUnlocked(PassiveType.SingleWeaponRamp))
            {
                ratio *= (1f + singleWeaponRamp);
            }
            return ratio;
        }

        // ============================== 应用强化 ==============================

        // 基础属性强化：叠加次数 +1，并应用具体属性效果
        public static void ApplyStat(StatType type, float valuePerStack)
        {
            statStacks[type] = GetStatStacks(type) + 1;
            ApplyStatEffect(type, valuePerStack);
        }

        // 具体属性生效逻辑（后续实现，示例）：
        //   MaxHP:        Global.INGAME_MAX_HP.Value += value; Global.currentHP.Value += value;
        //   MoveSpeed:    Player.player1.moveSpeed += value;
        //   AmmoCapacity: 提升各武器 GunClip.maxAmmo 与 WeaponData.weaponMaxAmmo 并刷新 UI
        private static void ApplyStatEffect(StatType type, float value)
        {
            // TODO: 待实现具体属性效果，当前框架仅记录叠加次数
        }

        // 武器伤害强化：等级 +1、伤害系数累加；返回 true 表示本次达到满级，应触发进化
        public static bool ApplyWeaponDamage(WeaponType type, float bonusPerStack)
        {
            int level = GetWeaponLevel(type) + 1;
            weaponLevels[type] = level;
            weaponDamageRatios[type] = GetWeaponDamageRatio(type) + bonusPerStack;
            return level >= MaxStackCount;
        }

        // 被动解锁：不可重复，重复解锁由池过滤保证
        public static void UnlockPassive(PassiveType type)
        {
            unlockedPassives.Add(type);
        }

        // 单局重置（Global.ResetLevel 调用）
        public static void Reset()
        {
            statStacks.Clear();
            weaponLevels.Clear();
            weaponDamageRatios.Clear();
            unlockedPassives.Clear();
            GlobalDamageRatio = 1f;
            switchBuffTimer = 0f;
            singleWeaponRamp = 0f;
            currentWeaponType = WeaponType.None;
        }
    }
}
