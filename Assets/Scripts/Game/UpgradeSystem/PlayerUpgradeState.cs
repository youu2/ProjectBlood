using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 玩家强化状态(静态,与旧 PlayerUpgrade 的访问方式保持一致)。
    // 职责：记录基础属性叠加次数、各武器等级与伤害系数、已拥有武器、已解锁被动；
    //       所有强化效果的应用(Apply*)与伤害计算查询(GetFinalDamageRatio)都统一走这里。
    // 生命周期：单局有效,Global.ResetLevel() 时调用 Reset() 清空。
    public static class PlayerUpgradeState
    {
        // 系统满级标准(固定 5 次):武器伤害达此等级触发进化,IsXxxMaxed 满级查询也以此为准。
        // 单张强化卡的可选择次数上限走 UpgradeEffect.MaxUpgradeCount(可配置,池过滤用),两者互不影响。
        public const int MaxStackCount = UpgradeEffect.MaxStackCount;

        // ---- 武器伤害强化等级(0 = 未强化,5 = 满级):武器进化触发线,ApplyWeaponDamage 返回值据此判断是否触发 IWeaponEvolution ----
        private static readonly Dictionary<WeaponType, int> weaponDamageLevels = new Dictionary<WeaponType, int>();
        // ---- 武器伤害系数(1.0 = 100%,每次强化累加 bonusPerStack)----
        private static readonly Dictionary<WeaponType, float> weaponDamageRatios = new Dictionary<WeaponType, float>();
        // ---- 已解锁的全局被动 ----
        private static readonly HashSet<PassiveType> unlockedPassives = new HashSet<PassiveType>();
        // ---- 每张强化卡(UpgradeSO 资产)的累计被选择次数 ----
        // 按卡牌维度独立计数:MaxUpgradeCount 限制的是"这张卡能被选几次",
        // 多张影响同一属性/武器的卡互不影响(例如两张 MaxUpgradeCount=5 的生命卡,生命值最多可被改 10 次)
        private static readonly Dictionary<UpgradeSO, int> upgradeUsageCounts = new Dictionary<UpgradeSO, int>();

        // ---- 基础属性基线(首次强化对应属性时捕获,供单局重置还原；-1 表示未记录)----
        // Player 与 BloodBank 是跨场景持久的单例/Prefab 实例,强化后需手动回退
        private static float baseMoveSpeed = -1f;
        private static int baseBloodBankCapacity = -1;
        // 累计移速加成(静态,跨场景保留；场景重载后 Player 被销毁重建,由 OnPlayerSpawned 补回)
        private static float moveSpeedBonus;

        // ---- 负数强化(代价型)的下限保护：防止代价强化把属性打成不可玩状态 ----
        private const float MinPlayerMaxHP = 1f;          // 最大生命值下限(当前生命同步夹到 [1, 上限],强化不会直接致死)
        private const float MinMoveSpeed = 0.5f;          // 移动速度下限
        private const int MinBloodBankMax = 1;            // 血库容量下限(Global.ReduceHP 有按容量的除法,必须 >= 1)
        private const float MinWeaponDamageRatio = 0.05f; // 单武器伤害系数下限(5%)
        private const int MinWeaponMaxAmmo = 1;           // 弹夹容量下限(至少 1 发)

        // 全局伤害倍率(迁移自旧 PlayerUpgrade.DamageRatio,默认 1.0 不影响原有伤害)
        public static float GlobalDamageRatio { get; private set; } = 1f;

        // ============ 被动运行时状态(数值为占位默认值,后续平衡阶段调整)============

        // 切枪增益：切换武器后短时间内全武器伤害提升
        private const float SwitchBuffDuration = 3f;       // TODO: 切枪增益持续时间(秒)
        private const float SwitchBuffMultiplier = 1.5f;   // TODO: 切枪增益伤害倍率
        private static float switchBuffTimer;

        // 单武器持续输出叠加：连续使用同一把武器射击时伤害逐步提升,上限 30%,切枪重置
        private const float RampPerShot = 0.02f;           // TODO: 每开一枪增加的伤害比例
        private const float RampMax = 0.30f;               // 上限 30%
        private static float singleWeaponRamp;
        private static WeaponType currentWeaponType = WeaponType.None;

        private static bool initialized;

        // 订阅武器开火事件(由 Global.Initialize 在启动时调用一次)
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

        // 被动计时,由 Player.Update 驱动(暂停时 Time.deltaTime 为 0,不会误走时)
        public static void TickPassives(float deltaTime)
        {
            if (switchBuffTimer > 0f)
            {
                switchBuffTimer = Mathf.Max(0f, switchBuffTimer - deltaTime);
            }
        }

        // 切枪钩子,由 Player.UseWeapon 调用：重置单武器叠加,并尝试激活切枪增益
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

        // 武器伤害全局累计等级:ApplyWeaponDamage 内部使用,计算进化触发(返回 level >= MaxStackCount)
        public static int GetWeaponDamageLevel(WeaponType type)
            => weaponDamageLevels.TryGetValue(type, out int v) ? v : 0;

        public static float GetWeaponDamageRatio(WeaponType type)
            => weaponDamageRatios.TryGetValue(type, out float v) ? v : 1f;

        public static bool IsPassiveUnlocked(PassiveType type)
            => unlockedPassives.Contains(type);

        // 某张强化卡累计被选择的次数(MaxUpgradeCount 池过滤依据,按卡牌独立计数)
        public static int GetUpgradeUsageCount(UpgradeSO upgrade)
            => upgrade != null && upgradeUsageCounts.TryGetValue(upgrade, out int v) ? v : 0;

        // 在 WeaponDataSystem 中查找武器数据(未拥有时返回 null)
        private static WeaponData FindWeaponData(WeaponType type)
        {
            if (type == WeaponType.None) return null;
            string weaponName = type.ToWeaponName();
            for (int i = 0; i < WeaponDataSystem.weaponDataList.Count; i++)
            {
                if (WeaponDataSystem.weaponDataList[i].weaponName == weaponName) return WeaponDataSystem.weaponDataList[i];
            }
            return null;
        }

        // 已拥有武器以 WeaponDataSystem.weaponDataList(宝箱解锁)为准
        public static bool IsWeaponOwned(WeaponType type)
            => FindWeaponData(type) != null;

        // 伤害计算统一入口(PlayerBullet / Laser 命中时调用)：
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

        // 基础属性强化:每次调用直接应用效果(属性无全局等级跟踪,池过滤由每卡 MaxUpgradeCount 独立控制)
        public static void ApplyStat(StatType type, float valuePerStack)
        {
            ApplyStatEffect(type, valuePerStack);
        }

        // 具体属性生效逻辑：每次强化叠加 value(由 UpgradeSO 配置,可为负实现代价型强化),并夹到各自下限
        private static void ApplyStatEffect(StatType type, float value)
        {
            switch (type)
            {
                case StatType.MaxHP:
                    // 上限与当前生命同步变化；负值时夹到 [1, 新上限],代价强化不会把玩家直接打到 0 血致死
                    //(上限本身的单局重置由 Global.ResetLevel 中 INGAME_MAX_HP = INIT_MAX_HP 处理)
                    Global.INGAME_MAX_HP.Value = Mathf.Max(MinPlayerMaxHP, Global.INGAME_MAX_HP.Value + value);
                    Global.currentHP.Value = Mathf.Clamp(Global.currentHP.Value + value, 1f, Global.INGAME_MAX_HP.Value);
                    break;

                case StatType.MoveSpeed:
                    {
                        float applied = value;
                        if (Player.player1 != null)
                        {
                            if (baseMoveSpeed < 0f) baseMoveSpeed = Player.player1.moveSpeed; // 首次强化时记录基线
                            float newSpeed = Mathf.Max(MinMoveSpeed, Player.player1.moveSpeed + value);
                            applied = newSpeed - Player.player1.moveSpeed; // 被下限截断时按实际生效量累计
                            Player.player1.moveSpeed = newSpeed;
                        }
                        moveSpeedBonus += applied; // 场景重载后由 OnPlayerSpawned 补回
                        break;
                    }

                case StatType.BloodBankCapacity:
                    {
                        // 血库为整数计量：量化为幅度至少 1 的带符号整数(与 GetSignedInt 量化规则一致)
                        int delta = UpgradeEffect.GetSignedInt(value);
                        if (baseBloodBankCapacity < 0) baseBloodBankCapacity = BloodBank.Instance.MaxBloodAmount; // 首次强化时记录基线
                        int newMax = Mathf.Max(MinBloodBankMax, BloodBank.Instance.MaxBloodAmount + delta);
                        int actualDelta = newMax - BloodBank.Instance.MaxBloodAmount;
                        BloodBank.Instance.MaxBloodAmount = newMax;
                        // 当前血液随容量同步增减,并夹到 [0, 新上限]
                        BloodBank.Instance.CurrentBloodAmount = Mathf.Clamp(BloodBank.Instance.CurrentBloodAmount + actualDelta, 0, newMax);
                        break;
                    }
            }
        }

        // 武器伤害强化：等级 +1、伤害系数累加(bonusPerStack 可为负,系数下限 MinWeaponDamageRatio)；
        // 返回 true 表示本次达到满级,应触发进化
        public static bool ApplyWeaponDamage(WeaponType type, float bonusPerStack)
        {
            int level = GetWeaponDamageLevel(type) + 1;
            weaponDamageLevels[type] = level;
            weaponDamageRatios[type] = Mathf.Max(MinWeaponDamageRatio, GetWeaponDamageRatio(type) + bonusPerStack);
            return level >= MaxStackCount;
        }

        // 武器弹夹容量强化:同步更新 WeaponData(静态持久)与已实例化武器的 GunClip。
        // bonusPerStack 可为负(代价型),容量下限 1 发;容量变化同步到当前弹药,并夹到 [0, 新上限]。
        // 如果升级的是当前装备武器,立即刷新弹药 UI。
        // 注:弹夹容量不追踪全局等级,池过滤由每卡 MaxUpgradeCount 独立控制,也不参与武器进化线。
        public static void ApplyWeaponAmmo(WeaponType type, int bonusPerStack)
        {
            int delta = UpgradeEffect.GetSignedInt(bonusPerStack); // 幅度至少 1,保留符号

            WeaponData weaponData = FindWeaponData(type);
            if (weaponData == null)
            {
                Debug.LogWarning($"[PlayerUpgradeState] 弹夹强化目标武器 {type} 未拥有,已跳过");
                return;
            }

            // 静态 WeaponData 持久化(切枪时通过 LoadWeaponData 读入,场景切换不销毁)
            int oldMax = weaponData.weaponMaxAmmo;
            weaponData.weaponMaxAmmo = Mathf.Max(MinWeaponMaxAmmo, oldMax + delta);
            int actualDelta = weaponData.weaponMaxAmmo - oldMax; // 被下限截断时按实际生效量同步
            weaponData.weaponCurrentAmmo = Mathf.Clamp(weaponData.weaponCurrentAmmo + actualDelta, 0, weaponData.weaponMaxAmmo);

            // 武器对象已实例化(玩家已在场),直接改 GunClip,切枪前即可生效
            if (Player.player1 != null)
            {
                WeaponBase weapon = Player.player1.GetWeapon(type);
                var gunClip = weapon != null ? weapon.GetGunClip() : null;
                if (gunClip != null)
                {
                    int clipOldMax = gunClip.maxAmmo;
                    gunClip.maxAmmo = Mathf.Max(MinWeaponMaxAmmo, clipOldMax + actualDelta);
                    int clipDelta = gunClip.maxAmmo - clipOldMax;
                    gunClip.currentAmmo = Mathf.Clamp(gunClip.currentAmmo + clipDelta, 0, gunClip.maxAmmo);
                    // 如果是当前装备武器,立即刷新弹药 UI
                    if (Player.player1.currentWeapon == weapon)
                    {
                        gunClip.UpdateClipUI();
                    }
                }
            }
        }

        // 被动解锁：不可重复,重复解锁由池过滤保证
        public static void UnlockPassive(PassiveType type)
        {
            unlockedPassives.Add(type);
        }

        // 记录一张强化卡被选择一次(由 UpgradeManager.ApplyUpgrade 在应用前调用)。
        // 计数按 UpgradeSO 资产引用独立累加,与属性/武器的全局效果计数互不影响。
        public static void RecordUpgradeUsage(UpgradeSO upgrade)
        {
            if (upgrade == null) return;
            upgradeUsageCounts[upgrade] = GetUpgradeUsageCount(upgrade) + 1;
        }

        // Player 重建时补回累计移速加成(Player.Awake 中调用；Player 不跨场景,静态加成需手动重新应用)
        public static void OnPlayerSpawned()
        {
            if (moveSpeedBonus != 0f && Player.player1 != null)
            {
                Player.player1.moveSpeed += moveSpeedBonus;
            }
        }

        // 单局重置(Global.ResetLevel 调用)
        public static void Reset()
        {
            weaponDamageLevels.Clear();
            weaponDamageRatios.Clear();
            unlockedPassives.Clear();
            upgradeUsageCounts.Clear();
            GlobalDamageRatio = 1f;
            switchBuffTimer = 0f;
            singleWeaponRamp = 0f;
            currentWeaponType = WeaponType.None;

            // 还原基础属性基线(MaxHP 上限由 Global.ResetLevel 重置,无需在此处理)
            // 血库为静态单例、跨局持久,必须回退；Player 若已被销毁则场景重载后由 Prefab 默认值还原
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
