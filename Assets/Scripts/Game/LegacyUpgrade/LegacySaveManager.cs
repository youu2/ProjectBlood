using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 局外养成存档结构（JsonUtility 序列化，version 用于未来数据迁移）
    [Serializable]
    public class LegacySaveData
    {
        public int version = 1;
        public int legacyPoint;
        public List<LegacySaveEntry> entries = new List<LegacySaveEntry>();
    }

    [Serializable]
    public class LegacySaveEntry
    {
        public string id;
        public int level;
    }

    // 局外养成持久化：PlayerPrefs 单 JSON key 存储全部数据
    public static class LegacySaveManager
    {
        public const string SaveKey = "LegacySaveData_v1";

        // 旧版散落的 PlayerPrefs key（v1 迁移源）
        private const string LegacyPointKey = "LegacyPoint";
        private const string CoinDropRateKey = "CoinDropRate";
        private const string InitMaxHpKey = "INIT_MAX_HP";

        public static LegacySaveData Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    var data = JsonUtility.FromJson<LegacySaveData>(PlayerPrefs.GetString(SaveKey));
                    if (data != null)
                    {
                        return data;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LegacySaveManager] 存档解析失败，回退默认数据：{e.Message}");
                }
                return new LegacySaveData();
            }

            // 首次运行且存在旧版散落 key 时迁移
            if (PlayerPrefs.HasKey(LegacyPointKey) || PlayerPrefs.HasKey(CoinDropRateKey) || PlayerPrefs.HasKey(InitMaxHpKey))
            {
                return MigrateFromLegacyKeys();
            }

            return new LegacySaveData();
        }

        public static void Save(LegacySaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        // 旧版迁移：LegacyPoint 直接搬；CoinDropRate 反推等级（RoundToInt 消除浮点误差，
        // 等级超过新配置上限的部分由 LegacyUpgradeState.Initialize 收敛）；
        // INIT_MAX_HP 旧版无升级入口，等级恒为 0，原值语义由 SO 的 baseValue 承载。
        // 注意：coin_drop_rate 为迁移约定的固定 id，与 LegacyUpgrade_CoinDropRate 资产的 id 一致。
        private static LegacySaveData MigrateFromLegacyKeys()
        {
            var data = new LegacySaveData
            {
                legacyPoint = PlayerPrefs.GetInt(LegacyPointKey, 0),
            };

            float coinRate = PlayerPrefs.GetFloat(CoinDropRateKey, 0.30f);
            int coinLevel = Mathf.Max(0, Mathf.RoundToInt((coinRate - 0.30f) / 0.05f));
            if (coinLevel > 0)
            {
                data.entries.Add(new LegacySaveEntry { id = "coin_drop_rate", level = coinLevel });
            }

            PlayerPrefs.DeleteKey(LegacyPointKey);
            PlayerPrefs.DeleteKey(CoinDropRateKey);
            PlayerPrefs.DeleteKey(InitMaxHpKey);
            Save(data);
            Debug.Log($"[LegacySaveManager] 旧存档迁移完成：LegacyPoint={data.legacyPoint}，CoinDropRate 等级={coinLevel}");
            return data;
        }
    }
}
