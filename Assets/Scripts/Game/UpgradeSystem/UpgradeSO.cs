using UnityEngine;

namespace ProjectBlood
{
    // 强化项配置资产（数据驱动）。
    // 在 Project 窗口通过 Create > ProjectBlood > Upgrade > Upgrade Config 创建，
    // 配置好名称/描述/图标/是否入池/效果后，拖入 UpgradeManager 的升级池列表。
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "ProjectBlood/Upgrade/Upgrade Config", order = 0)]
    public class UpgradeSO : ScriptableObject
    {
        [Header("显示信息")]
        public string upgradeName;          // 强化名称（卡片标题）
        [TextArea(2, 4)]
        public string description;          // 强化描述（卡片正文）
        public Sprite icon;                 // 强化图标

        [Header("池配置")]
        [Tooltip("是否进入随机抽取池，置 false 可临时禁用而不删除资产")]
        public bool isInPool = true;

        [Header("强化效果")]
        public UpgradeEffect effect = new UpgradeEffect();
    }
}
