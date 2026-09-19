using UnityEngine;

namespace ProjectBlood
{
    // 养成属性类型：新增数值型养成项时在此加枚举，并在 LegacyUpgradeState.ApplyEffect 加对应 case
    public enum LegacyStatType
    {
        CoinDropRate,
        InitMaxHp,
    }

    // 局外养成项配置（纯数据）。
    // 放入 Assets/Resources/LegacyUpgrades/ 目录即自动加载，新增选项零代码。
    // 存档只存等级，属性值由 baseValue + valuePerLevel * level 推导。
    [CreateAssetMenu(fileName = "LegacyUpgrade_", menuName = "局外养成/养成项配置")]
    public class LegacyUpgradeSO : ScriptableObject
    {
        [Header("显示信息")]
        public string id;                    // 存档匹配键，创建后不可改
        public string upgradeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("等级与价格")]
        [Min(1)] public int maxLevel = 5;    // 等级上限
        public int baseCost = 5;
        public int costIncrement = 0;        // 升级价格 = baseCost + costIncrement * 当前等级

        [Header("数值效果")]
        public LegacyStatType statType;
        public float baseValue;              // 0 级基准值（如 CoinDropRate=0.30 / MaxHP=30）
        public float valuePerLevel;          // 每级增量（如 0.05 / 5）

        // 从当前等级升到下一级的价格
        public int GetCostAt(int level)
        {
            return baseCost + costIncrement * level;
        }

        // 当前等级的效果描述（升级面板动态展示）
        public string GetEffectDescription(int level)
        {
            float current = baseValue + valuePerLevel * level;
            float max = baseValue + valuePerLevel * maxLevel;
            switch (statType)
            {
                case LegacyStatType.CoinDropRate:
                    return $"金币掉率 +{valuePerLevel * 100f:F0}%/级（当前 {current * 100f:F0}%）";
                case LegacyStatType.InitMaxHp:
                    return $"最大生命 +{valuePerLevel:F0}/级（当前 {current:F0}）";
                default:
                    return string.Empty;
            }
        }
    }
}
