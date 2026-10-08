using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public class UIGamePanelData : UIPanelData
    {
    }

    // 升级选项卡片：按钮在 Prefab 中手动摆放（共 3 个），内容由脚本动态填充
    [Serializable]
    public class UpgradeOptionCard
    {
        public Button button;
        public Image icon;                       // 强化图标
        public TMPro.TextMeshProUGUI titleText;  // 强化名称
        public TMPro.TextMeshProUGUI descText;   // 强化描述

        // 用强化配置填充卡片并显示
        public void Fill(UpgradeSO upgrade)
        {
            button.gameObject.SetActive(true);
            if (icon != null)
            {
                icon.sprite = upgrade.icon;
                icon.enabled = upgrade.icon != null; // 未配置图标时隐藏 Image，避免显示白块
            }
            if (titleText != null) titleText.text = upgrade.upgradeName;
            if (descText != null) descText.text = upgrade.description;
        }

        // 清空监听并隐藏（可抽选项不足 3 个时）
        public void Hide()
        {
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    public partial class UIGamePanel : UIPanel
    {
        [Tooltip("升级面板中的 3 个选项卡片，按场景中手动摆放的按钮顺序赋值")]
        [SerializeField] private UpgradeOptionCard[] optionCards = new UpgradeOptionCard[3];

        // 升级面板是否处于打开状态（打开期间 timeScale=0、IsGamePaused=true）
        private bool isUpgradePanelOpen;

        protected override void OnInit(IUIData uiData = null)
        {
            GameUI.ShowGameUI();

            mData = uiData as UIGamePanelData ?? new UIGamePanelData();

            // 升级后不再自动打开面板，仅累计可升级次数；
            // 次数大于 0 时显示升级提示图标 UpgradeNotice，否则隐藏
            Global.PendingUpgradeCount.RegisterWithInitValue(count =>
            {
                if (GameUI.GUIInstance != null && GameUI.GUIInstance.UpgradeNotice != null)
                {
                    GameUI.GUIInstance.UpgradeNotice.gameObject.SetActive(count > 0);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            UpgradeRoot.Hide();
        }

        private void Update()
        {
            // 袖剑处决慢动作期间屏蔽 Alt：关闭面板会把 timeScale 硬编码回 1，破坏处决节奏（处决仅约 0.5 真实秒）
            if (HiddenBladeSettings.IsExecuting) return;

            if (!Input.GetKeyDown(KeyCode.LeftAlt) && !Input.GetKeyDown(KeyCode.RightAlt)) return;

            if (isUpgradePanelOpen)
            {
                // 升级界面中再次按 Alt：直接关闭面板（次数可保留，之后仍可按 Alt 继续选择）
                CloseUpgradePanel();
            }
            else if (!Global.IsGamePaused && Global.PendingUpgradeCount.Value > 0)
            {
                // 有可升级次数且游戏未被暂停页/结算/演出等其他系统占用时，按 Alt 打开升级面板
                OpenUpgradePanel();
            }
        }

        // 打开升级面板：暂停游戏，弹出随机强化选项
        private void OpenUpgradePanel()
        {
            isUpgradePanelOpen = true;
            Time.timeScale = 0;
            Global.IsGamePaused = true; // 禁用武器操作
            ShowUpgradeOptions();
        }

        // 从升级池随机抽取并填充 3 张卡片
        private void ShowUpgradeOptions()
        {
            int validCardCount = 0;
            for (int i = 0; i < optionCards.Length; i++)
            {
                if (optionCards[i] != null && optionCards[i].button != null) validCardCount++;
            }

            List<UpgradeSO> options = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.GetRandomUpgrades(validCardCount)
                : new List<UpgradeSO>();

            int optionIndex = 0;
            for (int i = 0; i < optionCards.Length; i++)
            {
                var card = optionCards[i];
                if (card == null || card.button == null) continue;

                card.button.onClick.RemoveAllListeners();

                if (optionIndex < options.Count)
                {
                    var upgrade = options[optionIndex];
                    optionIndex++;
                    card.Fill(upgrade);
                    card.button.onClick.AddListener(() => OnUpgradeSelected(upgrade));
                }
                else
                {
                    card.Hide(); // 可抽选项不足时隐藏多余卡片
                }
            }

            UpgradeRoot.Show();

            // 池中已无可用强化、或卡片尚未配置时，直接关闭面板避免卡死；
            // 不消耗累计次数（升级提示图标仍保留，待有新的可用强化后可再次打开）
            if (options.Count == 0 || validCardCount == 0)
            {
                CloseUpgradePanel();
            }
        }

        // 点击卡片：应用强化并消耗一次累计升级次数；
        // 次数仍有剩余则重新抽取选项继续选择，次数耗尽则自动关闭面板
        private void OnUpgradeSelected(UpgradeSO upgrade)
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ApplyUpgrade(upgrade);
            }

            if (Global.PendingUpgradeCount.Value > 0)
            {
                Global.PendingUpgradeCount.Value--;
            }

            if (Global.PendingUpgradeCount.Value <= 0)
            {
                CloseUpgradePanel();
            }
            else
            {
                ShowUpgradeOptions();
            }
        }

        // 关闭升级面板并恢复游戏（不再由选择卡片触发，仅由 Alt 或次数耗尽触发）
        private void CloseUpgradePanel()
        {
            isUpgradePanelOpen = false;
            Time.timeScale = 1;
            Global.IsGamePaused = false; // 重新启用开火
            UpgradeRoot.Hide();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}
