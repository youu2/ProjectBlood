using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 局外养成数据访问接口规范：外部系统（含 Global）仅通过此接口的语义获取/操作养成数据，
    // 不直接触碰存档。当前由静态类 LegacyUpgradeState 提供实现语义，未来可替换为实例实现（如云存档）。
    public interface ILegacyProgressionService
    {
        BindableProperty<int> LegacyPoint { get; }
        int GetLevel(string id);
        bool TryUpgrade(string id, out string error);
    }

    // 局外养成运行时状态：配置加载、升级、结算、效果应用、事件通知
    public static class LegacyUpgradeState
    {
        public static BindableProperty<int> LegacyPoint { get; private set; }

        // 养成项升级后的通知事件（UI 及其他系统订阅）
        public static event Action OnUpgradeChanged;

        private static LegacySaveData saveData;
        private static List<LegacyUpgradeSO> configList;
        private static Dictionary<string, LegacyUpgradeSO> configs;

        public static void Initialize()
        {
            LoadConfigs();
            saveData = LegacySaveManager.Load();

            LegacyPoint = new BindableProperty<int>(saveData.legacyPoint);
            // 点数变化自动持久化：必须先把新值同步进 saveData 再落盘，
            // 否则磁盘上永远是加载时的旧值（下次启动表现为“清空遗产点”）
            LegacyPoint.Register(value =>
            {
                saveData.legacyPoint = value;
                LegacySaveManager.Save(saveData);
            });

            ApplyEffects();
        }

        // 配置驱动：加载 Resources/LegacyUpgrades 下全部养成项资产，新增选项零代码
        private static void LoadConfigs()
        {
            configList = new List<LegacyUpgradeSO>(Resources.LoadAll<LegacyUpgradeSO>("LegacyUpgrades"));
            configs = new Dictionary<string, LegacyUpgradeSO>();
            foreach (var so in configList)
            {
                if (so == null || string.IsNullOrEmpty(so.id))
                {
                    Debug.LogWarning("[LegacyUpgradeState] 存在未配置 id 的养成项资产，已跳过");
                    continue;
                }
                if (configs.ContainsKey(so.id))
                {
                    Debug.LogError($"[LegacyUpgradeState] 养成项 id 重复：{so.id}");
                    continue;
                }
                configs.Add(so.id, so);
            }
        }

        public static IReadOnlyList<LegacyUpgradeSO> GetAllConfigs()
        {
            return configList;
        }

        public static int GetLevel(string id)
        {
            var entry = saveData.entries.Find(e => e.id == id);
            return entry?.level ?? 0;
        }

        // 升级：校验配置/等级上限/点数 → 扣点 → 等级+1 → 应用效果 → 持久化 → 通知
        public static bool TryUpgrade(string id, out string error)
        {
            error = null;
            if (!configs.TryGetValue(id, out var so))
            {
                error = $"未知养成项：{id}";
                return false;
            }
            int level = GetLevel(id);
            if (level >= so.maxLevel)
            {
                error = "已满级";
                return false;
            }
            int cost = so.GetCostAt(level);
            if (LegacyPoint.Value < cost)
            {
                error = "遗产点不足";
                return false;
            }

            LegacyPoint.Value -= cost;
            SetLevel(id, level + 1);
            ApplyEffect(so, level + 1);
            LegacySaveManager.Save(saveData);
            OnUpgradeChanged?.Invoke();
            return true;
        }

        // 关卡结算：局内玩家等级折算为遗产点（原 Global.SettleLegacyPoints 职责）
        public static void SettleFromRun(int playerLevel)
        {
            int gained = Mathf.Max(0, playerLevel - 1);
            LegacyPoint.Value += gained;
            Debug.Log($"[LegacyUpgradeState] 结算获得 {gained} 遗产点，当前 {LegacyPoint.Value}");
        }

        // 启动时按存档等级应用全部养成效果
        public static void ApplyEffects()
        {
            foreach (var entry in saveData.entries)
            {
                if (entry.level <= 0) continue;
                if (configs.TryGetValue(entry.id, out var so))
                {
                    ApplyEffect(so, entry.level);
                }
            }
        }

        // 把养成项按等级写入运行时属性；未来特殊效果（如开局解锁血印）在此加 case
        private static void ApplyEffect(LegacyUpgradeSO so, int level)
        {
            float value = so.baseValue + so.valuePerLevel * level;
            switch (so.statType)
            {
                case LegacyStatType.CoinDropRate:
                    Global.CoinDropRate.Value = value;
                    break;
                case LegacyStatType.InitMaxHp:
                    Global.INIT_MAX_HP.Value = value;
                    // INGAME_MAX_HP 静态初始化是快照，需同步保证首局血量正确
                    Global.INGAME_MAX_HP.Value = value;
                    break;
                case LegacyStatType.SkillCooldown:
                    // 全局减免率跨局保留（PlayerUpgradeState.Reset 不清空此字段），
                    // 游戏启动时 ApplyEffects 设置一次即可永久生效，所有技能（含未来新技能）自动享受
                    PlayerUpgradeState.SetGlobalSkillCooldownReduction(level * so.reductionPerLevel);
                    break;
                case LegacyStatType.MoveSpeed:
                    // 全局移速加成跨局保留；Player 不跨场景，OnPlayerSpawned 时叠加，升级时补差额立即生效
                    PlayerUpgradeState.SetGlobalMoveSpeedBonus(so.valuePerLevel * level);
                    break;
                case LegacyStatType.BloodBankCapacity:
                    // 血库是跨局持久单例，设置时直接补差额；Reset 还原局内基线时保留全局加成
                    PlayerUpgradeState.SetGlobalBloodBankCapacityBonus(Mathf.RoundToInt(so.valuePerLevel * level));
                    break;
                case LegacyStatType.WeaponUnlock:
                    // 跨局保留解锁数量，游戏开始时由 ResetLevel 调 ApplyGlobalWeaponUnlocks 实际解锁
                    PlayerUpgradeState.SetGlobalWeaponUnlockCount(level);
                    break;
                case LegacyStatType.RandomSigilUnlock:
                    // 跨局保留解锁数量；场景级 BloodSigilManager.Awake 时调 ApplyGlobalRandomSigils 实际解锁
                    BloodSigilState.SetGlobalRandomSigilUnlockCount(level);
                    break;
            }
        }

        private static void SetLevel(string id, int level)
        {
            var entry = saveData.entries.Find(e => e.id == id);
            if (entry != null)
            {
                entry.level = level;
            }
            else
            {
                saveData.entries.Add(new LegacySaveEntry { id = id, level = level });
            }
        }
    }
}
