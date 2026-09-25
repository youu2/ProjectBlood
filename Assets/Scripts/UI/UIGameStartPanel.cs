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
            GameUI.GUIInstance.Hide();

            // 遗产点显示与条目刷新（加载/持久化统一由 LegacyUpgradeState 负责）
            LegacyUpgradeState.LegacyPoint.RegisterWithInitValue(legacy =>
            {
                LegacyHeldText.text = "Lagacy: " + legacy;
                RefreshEntries();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // 升级后刷新条目（等级/价格/按钮状态）
            LegacyUpgradeState.OnUpgradeChanged += RefreshEntries;

            BtnStartGame.onClick.AddListener(() =>
            {
                GameUI.GUIInstance.Show();
                this.CloseSelf();
                RunSaveService.DeleteSave();    // 新游戏 = 新一局，清除旧存档
                Global.ResetLevel();
                Time.timeScale = 1.0f;
                GameUI.ShowLoadingPage("InGame");
            });

            // 继续游戏：读档 → 校验 → 载荷填充 → 加载 InGame 场景走还原路径
            BtnContinueGame.onClick.AddListener(() =>
            {
                if (!RunSaveService.TryLoad(out var data))
                {
                    Debug.LogWarning("[UIGameStartPanel] 无存档或存档无效，无法继续游戏");
                    return;
                }

                RunSaveService.PendingRestore = data;
                GameUI.GUIInstance.Show();
                this.CloseSelf();
                // 注意：不调用 Global.ResetLevel()——还原路径会从存档恢复全部状态
                Time.timeScale = 1.0f;
                GameUI.ShowLoadingPage("InGame");
            });

            // 根据存档存在与否控制继续按钮可用状态（无存档时显示但不可点击）
            BtnContinueGame.interactable = RunSaveService.HasSave();

            BtnLegacyUpgrade.onClick.AddListener(() =>
            {
                BuildUpgradeEntries();
                RefreshEntries();
                LegacyUpgradePanel.gameObject.SetActive(true);
                ScrollUpgradeListToTop();
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
            // 清空旧条目（保留模板）；先 SetActive(false) 立即排除出布局，
            // 避免 Destroy 延迟到帧末导致同帧布局高度计算包含旧条目
            for (int i = UpgradeEntryContainer.childCount - 1; i >= 0; i--)
            {
                var child = UpgradeEntryContainer.GetChild(i);
                if (child == (Transform)UpgradeEntryTemplate) continue;
                child.gameObject.SetActive(false);
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

        // 滚动列表回顶部：UGUI 布局（VerticalLayoutGroup/ContentSizeFitter）在帧末才重算，
        // 须先用 ForceUpdateCanvases 强制完成布局与文本排版，再设置归一化位置才不会因 Content 高度未更新而失效
        private void ScrollUpgradeListToTop()
        {
            var scrollRect = UpgradeEntryContainer != null
                ? UpgradeEntryContainer.GetComponentInParent<ScrollRect>()
                : null;
            if (scrollRect == null) return;

            Canvas.ForceUpdateCanvases();          // 完成条目实例化/文本变化引发的布局重算
            scrollRect.verticalNormalizedPosition = 1f;   // 1 = 顶部
            Canvas.ForceUpdateCanvases();          // 应用滚动位置
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
