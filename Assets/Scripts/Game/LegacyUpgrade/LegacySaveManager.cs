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
            }

            return new LegacySaveData();
        }

        public static void Save(LegacySaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
