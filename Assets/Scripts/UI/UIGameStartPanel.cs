using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public class UIGameStartPanelData : UIPanelData
    {
    }
    public partial class UIGameStartPanel : UIPanel
    {
        private readonly List<LegacyUpgradeEntryView> entryViews = new List<LegacyUpgradeEntryView>();

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UIGameStartPanelData ?? new UIGameStartPanelData();
            Time.timeScale = 0;
            Global.IsGamePaused = true;

            // 遗泽点显示与条目刷新（加载/持久化统一由 LegacyUpgradeState 负责）
            LegacyUpgradeState.LegacyPoint.RegisterWithInitValue(legacy =>
            {
                LegacyHeldText.text = "Lagacy: " + legacy;
                RefreshEntries();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // 升级后刷新条目（等级/价格/按钮状态）
            LegacyUpgradeState.OnUpgradeChanged += RefreshEntries;

            BtnStartGame.onClick.AddListener(() =>
            {
                this.CloseSelf();
                Global.ResetLevel();
                Time.timeScale = 1.0f;
                GameUI.ShowLoadingPage("InGame");
            });

            BtnLegacyUpgrade.onClick.AddListener(() =>
            {
                BuildUpgradeEntries();
                RefreshEntries();
                LegacyUpgradePanel.gameObject.SetActive(true);
                TittleText.Hide();
            });
            BtnCloseUpgradePage.onClick.AddListener(() =>
            {
                LegacyUpgradePanel.gameObject.SetActive(false);
                TittleText.Show();
            });
        }

        // 按配置动态生成养成条目（模板克隆，新增 SO 资产即自动出现）
        private void BuildUpgradeEntries()
        {
            // 清空旧条目（保留模板）
            for (int i = UpgradeEntryContainer.childCount - 1; i >= 0; i--)
            {
                var child = UpgradeEntryContainer.GetChild(i);
                if (child == (Transform)UpgradeEntryTemplate) continue;
                Destroy(child.gameObject);
            }
            entryViews.Clear();

            foreach (var so in LegacyUpgradeState.GetAllConfigs())
            {
                var entry = Instantiate(UpgradeEntryTemplate, UpgradeEntryContainer);
                entry.gameObject.SetActive(true);
                var view = entry.GetComponent<LegacyUpgradeEntryView>();
                var captured = so;
                view.Bind(captured, () => LegacyUpgradeState.TryUpgrade(captured.id, out _));
                entryViews.Add(view);
            }
        }

        // 按当前等级/点数刷新所有条目
        private void RefreshEntries()
        {
            foreach (var view in entryViews)
            {
                if (view == null || view.Current == null) continue;
                int level = LegacyUpgradeState.GetLevel(view.Current.id);
                bool isMax = level >= view.Current.maxLevel;
                int cost = isMax ? 0 : view.Current.GetCostAt(level);
                view.Refresh(level, cost, isMax, LegacyUpgradeState.LegacyPoint.Value >= cost);
            }
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
            LegacyUpgradeState.OnUpgradeChanged -= RefreshEntries;
            Time.timeScale = 1;
            Global.IsGamePaused = false;
        }
    }
}
