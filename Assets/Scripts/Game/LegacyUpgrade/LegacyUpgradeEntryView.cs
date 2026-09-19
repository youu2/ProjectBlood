using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    // 遗泽升级条目视图：挂在模板条目上（Inspector 拖引用），克隆时子引用自动重映射
    public class LegacyUpgradeEntryView : MonoBehaviour
    {
        public Button upgradeButton;
        public Image icon;
        public TMPro.TextMeshProUGUI descText;    // 名称 + 效果描述
        public TMPro.TextMeshProUGUI levelText;   // "LV 2/5"
        public TMPro.TextMeshProUGUI costText;    // 按钮价格，满级显示 MAX

        public LegacyUpgradeSO Current { get; private set; }

        // 绑定配置与点击回调（克隆后调用一次）
        public void Bind(LegacyUpgradeSO so, Action onClick)
        {
            Current = so;
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() => onClick?.Invoke());
            }
            if (icon != null)
            {
                icon.sprite = so.icon;
                icon.enabled = so.icon != null;   // 未配置图标时隐藏，避免显示白块
            }
        }

        // 按当前等级与点数刷新显示
        public void Refresh(int level, int cost, bool isMax, bool canAfford)
        {
            if (Current == null) return;
            if (descText != null)
            {
                descText.text = $"{Current.upgradeName}：\n{Current.GetEffectDescription(level)}";
            }
            if (levelText != null)
            {
                levelText.text = $"LV {level}";
            }
            if (costText != null)
            {
                costText.text = isMax ? "MAX" : $"升级\n（{cost}L）";
            }
            if (upgradeButton != null)
            {
                upgradeButton.interactable = !isMax && canAfford;
            }
        }
    }
}
