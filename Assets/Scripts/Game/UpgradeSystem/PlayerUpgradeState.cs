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
        // ---- 武器伤害强化等级（0 = 未强化，5 = 满级）----
        private static readonly Dictionary<WeaponType, int> weaponDamageLevels = new Dictionary<WeaponType, int>();
        // ---- 武器伤害系数（1.0 = 100%，每次强化累加 bonusPerStack）----
        private static readonly Dictionary<WeaponType, float> weaponDamageRatios = new Dictionary<WeaponType, float>();
        // ---- 武器弹夹容量强化等级（0 = 未强化，5 = 满级）----
        private static readonly Dictionary<WeaponType, int> weaponAmmoLevels = new Dictionary<WeaponType, int>();
        // ---- 武器弹夹容量累计加成（发；与 WeaponData 中的 weaponMaxAmmo 保持同步，用于重置时计数清零）----
        private static readonly Dictionary<WeaponType, int> weaponAmmoBonus = new Dictionary<WeaponType, int>();
        // ---- 已解锁的全局被动 ----
        private static readonly HashSet<PassiveType> unlockedPassives = new HashSet<PassiveType>();

        // ---- 基础属性基线（首次强化对应属性时捕获，供单局重置还原；-1 表示未记录）----
        // Player 与 BloodBank 是跨场景持久的单例/Prefab 实例，强化后需手动回退
        private static float baseMoveSpeed = -1f;
        private static int baseBloodBankCapacity = -1;
        // 累计移速加成（静态，跨场景保留；场景重载后 Player 被销毁重建，由 OnPlayerSpawned 补回）
        private static float moveSpeedBonus;

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

        public static int GetWeaponDamageLevel(WeaponType type)
            => weaponDamageLevels.TryGetValue(type, out int v) ? v : 0;

        public static bool IsWeaponDamageMaxed(WeaponType type)
            => GetWeaponDamageLevel(type) >= MaxStackCount;

        public static int GetWeaponAmmoLevel(WeaponType type)
            => weaponAmmoLevels.TryGetValue(type, out int v) ? v : 0;

        public static bool IsWeaponAmmoMaxed(WeaponType type)
            => GetWeaponAmmoLevel(type) >= MaxStackCount;

        public static int GetWeaponAmmoBonus(WeaponType type)
            => weaponAmmoBonus.TryGetValue(type, out int v) ? v : 0;

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

        // 具体属性生效逻辑：每次强化叠加固定值（value 由 UpgradeSO.statValue 配置）
        private static void ApplyStatEffect(StatType type, float value)
        {
            switch (type)
            {
                case StatType.MaxHP:
                    // 提升上限并同步回复等量生命值，保证强化立即生效
                    //（上限本身的单局重置由 Global.ResetLevel 中 INGAME_MAX_HP = INIT_MAX_HP 处理）
                    Global.INGAME_MAX_HP.Value += value;
                    Global.currentHP.Value += value;
                    break;

                case StatType.MoveSpeed:
                    moveSpeedBonus += value; // 先累计到静态加成，场景重载后可补回
                    if (Player.player1 != null)
                    {
                        if (baseMoveSpeed < 0f) baseMoveSpeed = Player.player1.moveSpeed; // 首次强化时记录基线
                        Player.player1.moveSpeed += value;
                    }
                    break;

                case StatType.BloodBankCapacity:
                    int amount = Mathf.Max(1, Mathf.RoundToInt(value)); // 血库为整数计量，配置值四舍五入且至少 +1
                    if (baseBloodBankCapacity < 0) baseBloodBankCapacity = BloodBank.Instance.MaxBloodAmount; // 首次强化时记录基线
                    BloodBank.Instance.MaxBloodAmount += amount;
                    // 同步增加等量当前血液（与 MaxHP 语义一致）
                    BloodBank.Instance.CurrentBloodAmount = Mathf.Min(BloodBank.Instance.CurrentBloodAmount + amount, BloodBank.Instance.MaxBloodAmount);
                    break;
            }
        }

        // 武器伤害强化：等级 +1、伤害系数累加；返回 true 表示本次达到满级，应触发进化
        public static bool ApplyWeaponDamage(WeaponType type, float bonusPerStack)
        {
            int level = GetWeaponDamageLevel(type) + 1;
            weaponDamageLevels[type] = level;
            weaponDamageRatios[type] = GetWeaponDamageRatio(type) + bonusPerStack;
            return level >= MaxStackCount;
        }

        // 武器弹夹容量强化：等级 +1，并同步更新 WeaponData.weaponMaxAmmo（静态持久）与已实例化武器的 GunClip
        // 如果升级的就是当前装备的武器，会立即刷新弹药 UI；返回 true 表示达到满级（当前无进化预留）
        public static bool ApplyWeaponAmmo(WeaponType type, int bonusPerStack)
        {
            int bonus = Mathf.Max(1, bonusPerStack); // 至少 +1
            int level = GetWeaponAmmoLevel(type) + 1;
            weaponAmmoLevels[type] = level;
            weaponAmmoBonus[type] = GetWeaponAmmoBonus(type) + bonus;

            string weaponName = type.ToWeaponName();
            WeaponData weaponData = null;
            for (int i = 0; i < WeaponDataSystem.weaponDataList.Count; i++)
            {
                if (WeaponDataSystem.weaponDataList[i].weaponName == weaponName)
                {
                    weaponData = WeaponDataSystem.weaponDataList[i];
                    break;
                }
            }

            if (weaponData != null)
            {
                // 静态 WeaponData 持久化（切枪时通过 LoadWeaponData 读入，场景切换不销毁）
                weaponData.weaponMaxAmmo += bonus;

                // 武器对象已实例化（玩家已在场），直接改 GunClip，切枪前即可生效
                WeaponBase weapon = null;
                if (Player.player1 != null) weapon = Player.player1.GetWeapon(type);
                if (weapon != null)
                {
                    var gunClip = weapon.GetGunClip();
                    if (gunClip != null)
                    {
                        gunClip.maxAmmo += bonus;
                        // 升级立即给等量子弹（与 MaxHP/血库 规则一致），不超过上限
                        gunClip.currentAmmo = Mathf.Min(gunClip.currentAmmo + bonus, gunClip.maxAmmo);
                        // 同时同步回 WeaponData 的 current（与 SaveWeaponData 对称），避免切枪时弹量回滚
                        weaponData.weaponCurrentAmmo = gunClip.currentAmmo;
                        // 如果是当前装备武器，立即刷新弹药 UI
                        if (Player.player1 != null && Player.player1.currentWeapon == weapon)
                        {
                            gunClip.UpdateClipUI();
                        }
                    }
                }
            }
            return level >= MaxStackCount;
        }

        // 被动解锁：不可重复，重复解锁由池过滤保证
        public static void UnlockPassive(PassiveType type)
        {
            unlockedPassives.Add(type);
        }

        // Player 重建时补回累计移速加成（Player.Awake 中调用；Player 不跨场景，静态加成需手动重新应用）
        public static void OnPlayerSpawned()
        {
            if (moveSpeedBonus != 0f && Player.player1 != null)
            {
                Player.player1.moveSpeed += moveSpeedBonus;
            }
        }

        // 单局重置（Global.ResetLevel 调用）
        public static void Reset()
        {
            statStacks.Clear();
            weaponDamageLevels.Clear();
            weaponAmmoLevels.Clear();
            weaponAmmoBonus.Clear();
            weaponDamageRatios.Clear();
            unlockedPassives.Clear();
            GlobalDamageRatio = 1f;
            switchBuffTimer = 0f;
            singleWeaponRamp = 0f;
            currentWeaponType = WeaponType.None;

            // 还原基础属性基线（MaxHP 上限由 Global.ResetLevel 重置，无需在此处理）
            // 血库为静态单例、跨局持久，必须回退；Player 若已被销毁则场景重载后由 Prefab 默认值还原
            if (baseBloodBankCapacity >= 0)
            {
                BloodBank.Instance.MaxBloodAmount = baseBloodBankCapacity;
                BloodBank.Instance.CurrentBloodAmount = Mathf.Clamp(BloodBank.Instance.CurrentBloodAmount, 0, BloodBank.Instance.MaxBloodAmount);
            }
            if (baseMoveSpeed >= 0f && Player.player1 != null)
            {
                Player.player1.moveSpeed = baseMoveSpeed;
            }
            baseBloodBankCapacity = -1;
            baseMoveSpeed = -1f;
            moveSpeedBonus = 0f;
        }
    }
}
