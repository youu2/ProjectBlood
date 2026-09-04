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

        protected override void OnInit(IUIData uiData = null)
        {
            GameUI.ShowGameUI();

            mData = uiData as UIGamePanelData ?? new UIGamePanelData();
            // bind to Global properties
            // update UI when properties change
            Global.currentHP.RegisterWithInitValue(currentHP =>
            {
                HPText.text = "HP: " + Mathf.FloorToInt(currentHP) + "/" + Mathf.FloorToInt(Global.INGAME_MAX_HP.Value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.INGAME_MAX_HP.RegisterWithInitValue(maxHP =>
            {
                HPText.text = "HP: " + Mathf.FloorToInt(Global.currentHP.Value) + "/" + Mathf.FloorToInt(maxHP);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Exp.RegisterWithInitValue(Exp =>
            {
                ExpText.text = "Exp: " + Exp + "/" + Global.MAX_EXP;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Level.RegisterWithInitValue(Level =>
            {
                LevelText.text = "Level: " + Level;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Coin.RegisterWithInitValue(Coin =>
            {
                CoinText.text = Coin.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // 升级时暂停游戏，弹出随机强化选项
            Global.Level.Register(Level =>
            {
                Time.timeScale = 0;
                Global.IsGamePaused = true; // 禁用武器操作
                ShowUpgradeOptions();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.RemainingTime.RegisterWithInitValue(Second =>
            {
                TimeText.text = "Wave in: " + Second + "s";
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            UpgradeRoot.Hide();

            // display Legacy Point
            Global.LegacyPoint.RegisterWithInitValue(legacy =>
            {
                LegacyText.text = "Lagacy: " + legacy;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

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

            // 池中已无可用强化、或卡片尚未配置时，直接恢复游戏，避免升级面板卡死（后续可改为提示文本）
            if (options.Count == 0 || validCardCount == 0)
            {
                ResumeGame();
            }
        }

        // 点击卡片：应用强化并关闭面板恢复游戏
        private void OnUpgradeSelected(UpgradeSO upgrade)
        {
            UpgradeManager.Instance?.ApplyUpgrade(upgrade);
            ResumeGame();
        }

        private void ResumeGame()
        {
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
