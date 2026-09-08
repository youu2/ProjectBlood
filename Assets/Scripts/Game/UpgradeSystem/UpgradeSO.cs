using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 强化项配置资产(数据驱动)。
    // 在 Project 窗口通过 Create > 强化系统 > 强化项配置 创建,
    // 配置好名称/描述/图标/是否入池/组合效果后,拖入 UpgradeManager 的升级池列表。
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "强化系统/强化项配置", order = 0)]
    public class UpgradeSO : ScriptableObject
    {
        [Header("显示信息")]
        public string upgradeName;          // 强化名称(卡片标题)
        [TextArea(2, 4)]
        public string description;          // 强化描述(卡片正文)
        public Sprite icon;                 // 强化图标

        [Header("池配置")]
        [Tooltip("是否进入随机抽取池,置 false 可临时禁用而不删除资产")]
        public bool isInPool = true;

        [Header("组合强化效果(可同时配置多条不同大类效果,数值可为负)")]
        public UpgradeEffect effect = new UpgradeEffect();

#if UNITY_EDITOR
        // 编辑期配置校验：先强制修正可安全兜底的越界值(如 MaxUpgradeCount 被序列化/旧数据写成 0 或负数),
        // 再提示其余无效/冲突配置(空配置、0 值、重复子类型等)
        private void OnValidate()
        {
            if (effect == null) return;
            effect.ClampValid();

            var errors = new List<string>();
            if (!effect.Validate(errors))
            {
                Debug.LogWarning($"[UpgradeSO] {name} 配置问题：\n- " + string.Join("\n- ", errors), this);
            }
        }
#endif
    }
}
