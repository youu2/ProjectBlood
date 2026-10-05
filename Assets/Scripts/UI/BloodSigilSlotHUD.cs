using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    /// <summary>
    /// 主动血印槽位 HUD（挂 GameUI 画布下，DontDestroyOnLoad 跨场景存活）：
    /// 4 个固定槽位对应数字键 1~4，显示血印图标与门控生命周期遮罩。
    ///
    /// 驱动模式：纯轮询 BloodSigilState（与 SkillCooldownUI 同款），不订阅事件——
    /// 槽位/阶段事件在继续游戏（RestoreSlots 静默恢复）与新局 Reset 两条静默路径中不派发，
    /// 轮询可在任意路径后于下一帧自动对齐，单一代码路径覆盖激活/替换/移除/读档/重开。
    /// 开销：每帧 4 次 O(1) 查询 + 4 次 fillAmount 赋值，零分配。
    ///
    /// 遮罩约定（每槽单个 Mask Image，同时服务持续计时与冷却计时）：
    ///   Inspector 一次性配置 Image Type=Filled、Fill Method=Vertical、Fill Origin=Top（代码不切锚点）；
    ///   - 有限持续：填"已流逝比例"，0→1，遮罩从上向下生长；
    ///   - 冷却：填"剩余比例"，1→0，遮罩下缘从底边上移（从下缩减至上）；
    ///   两个比例共用同一 Top 锚点，方向天然相反；持续结束(fill=1)→冷却开始(fill=1)无视觉跳变；
    ///   - 无限持续门控：激活恒 1（半透明全覆盖），就绪瞬时归 0（明暗瞬时切换）。
    ///
    /// 显隐控制（两层与运算，最终可见 = 全局未隐藏 && 槽位有血印）：
    ///   - 单槽：槽位无血印时隐藏对应槽位（SlotView.root，直接在本组件绑定）；
    ///   - 全局：GameUI.HideGameUI（主界面/加载页等）调 HideAll 锁定全部隐藏，
    ///     ShowGameUI 调 RestoreVisibility 按槽位占用恢复（空槽保持隐藏，不做无差别全显）。
    ///   显隐通过 CanvasGroup.alpha 实现而非 SetActive：SetActive(false) 会停掉槽位子树内
    ///   所有组件的 Update——若本组件位于槽位根节点内部，隐藏空槽会连带停掉自身轮询，
    ///   之后装备血印再也无法刷新（继续游戏时槽位在读档阶段已占用、根节点保持激活，
    ///   故表现为"只有存档退出重进才显示"）。CanvasGroup 只控透明度，子树始终激活。
    /// </summary>
    public class BloodSigilSlotHUD : MonoBehaviour
    {
        public static BloodSigilSlotHUD Instance { get; private set; }

        [Serializable]
        public class SlotView
        {
            [Tooltip("槽位根节点：空槽/全局隐藏时通过 CanvasGroup 整体隐藏")]
            public RectTransform root;

            [Tooltip("血印图标（BloodSigilSO.icon）")]
            public Image icon;

            [Tooltip("计时遮罩：Filled/Vertical/Fill Origin=Top，半透明色，关闭 raycastTarget")]
            public Image mask;
        }

        [Header("槽位引用（固定 4 个，按数字键 1~4 顺序拖入）")]
        public SlotView[] slots = new SlotView[BloodSigilState.MaxActiveSigils];

        // 变更检测缓存：仅在实际变化时重绑图标；读档/重开的静默变更也能被下一帧轮询捕获
        private readonly BloodSigilSO[] bound = new BloodSigilSO[BloodSigilState.MaxActiveSigils];

        // 全局显隐锁：false 时所有槽位强制隐藏（主界面/加载页），由 GameUI.HideGameUI/ShowGameUI 驱动
        private bool globalVisible = true;
        // 每槽最终可见性缓存，仅在变化时写 alpha
        private readonly bool[] slotVisibleCache = new bool[BloodSigilState.MaxActiveSigils];
        // 首帧强制应用一次可见性（纠正编辑器默认状态）
        private bool visibilityInitialized;
        // 每槽 CanvasGroup（Awake 时自动补齐），显隐只改 alpha，子树保持激活
        private CanvasGroup[] slotGroups;

        private void Awake()
        {
            // GameUI 为 DDOL 且各场景均放有 GameUI 副本：副本销毁前其子物体 Awake 仍会执行，
            // 首个实例优先，防止副本覆盖 Instance 后又在其 OnDestroy 中清空
            if (Instance != null && Instance != this) return;
            Instance = this;

            slotGroups = new CanvasGroup[slots != null ? slots.Length : 0];
            for (int i = 0; i < slotGroups.Length; i++)
            {
                var root = slots[i] != null ? slots[i].root : null;
                if (root == null) continue;
                var group = root.GetComponent<CanvasGroup>();
                if (group == null) group = root.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;   // HUD 纯展示，不拦截点击
                slotGroups[i] = group;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 全局隐藏：主界面/加载页等"隐藏全部 UI"场景由 GameUI.HideGameUI 调用，立即生效不等下一帧
        public static void HideAll()
        {
            if (Instance == null) return;
            Instance.globalVisible = false;
            Instance.ApplyAllVisibility();
        }

        // 全局恢复：按槽位占用恢复显隐（空槽保持隐藏），由 GameUI.ShowGameUI 调用
        public static void RestoreVisibility()
        {
            if (Instance == null) return;
            Instance.globalVisible = true;
            Instance.ApplyAllVisibility();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slots != null && slots.Length != BloodSigilState.MaxActiveSigils)
                Debug.LogWarning($"[BloodSigilSlotHUD] 槽位数量应为 {BloodSigilState.MaxActiveSigils}（当前 {slots.Length}）", this);
        }
#endif

        private void Update()
        {
            ApplyAllVisibility();

            int count = Mathf.Min(slots != null ? slots.Length : 0, BloodSigilState.MaxActiveSigils);
            for (int i = 0; i < count; i++)
            {
                var view = slots[i];
                if (view == null) continue;

                var sigil = BloodSigilState.GetSigilAtSlot(i);
                if (!ReferenceEquals(sigil, bound[i]))
                {
                    bound[i] = sigil;
                    ApplyBinding(view, sigil);
                }

                ApplyMask(view, i, sigil);
            }
        }

        // 应用全部槽位可见性：最终可见 = 全局未隐藏 && 槽位有血印；仅在变化时写 alpha
        private void ApplyAllVisibility()
        {
            int count = Mathf.Min(slotGroups != null ? slotGroups.Length : 0, BloodSigilState.MaxActiveSigils);
            for (int i = 0; i < count; i++)
            {
                bool visible = globalVisible && BloodSigilState.GetSigilAtSlot(i) != null;
                if (visibilityInitialized && visible == slotVisibleCache[i]) continue;
                slotVisibleCache[i] = visible;

                if (slotGroups[i] != null) slotGroups[i].alpha = visible ? 1f : 0f;
            }
            visibilityInitialized = true;
        }

        // 槽位重绑：入槽设图标；移除清空并把遮罩归零（替换时遮罩由阶段逻辑接管）
        private static void ApplyBinding(SlotView view, BloodSigilSO sigil)
        {
            if (view.icon == null) return;

            bool hasIcon = sigil != null && sigil.icon != null;
            view.icon.enabled = hasIcon;
            view.icon.sprite = hasIcon ? sigil.icon : null;

            if (sigil == null && view.mask != null)
                view.mask.fillAmount = 0f;
        }

        // 遮罩推进：按门控生命周期阶段计算填充比例（阶段→公式的映射见类头注释）
        private static void ApplyMask(SlotView view, int slot, BloodSigilSO sigil)
        {
            var mask = view.mask;
            if (mask == null || sigil == null) return;

            // 理论不可达 false（主动血印必有门控状态机），防御性按就绪处理
            if (!BloodSigilState.TryGetGateHudAtSlot(slot, out var hud))
            {
                mask.fillAmount = 0f;
                return;
            }

            switch (hud.Phase)
            {
                case SigilGatePhase.Active when hud.PhaseTotal > 0f:
                    // 有限持续：已流逝比例，遮罩从上向下生长（0→1）
                    mask.fillAmount = Mathf.Clamp01(1f - hud.PhaseRemaining / hud.PhaseTotal);
                    break;
                case SigilGatePhase.Active:
                    mask.fillAmount = 1f;   // 无限持续：半透明全覆盖
                    break;
                case SigilGatePhase.Cooldown:
                    // 冷却：剩余比例，遮罩下缘从底边上移（1→0）；引擎保证 PhaseTotal>0，防御除零
                    mask.fillAmount = hud.PhaseTotal > 0f
                        ? Mathf.Clamp01(hud.PhaseRemaining / hud.PhaseTotal)
                        : 0f;
                    break;
                default:
                    mask.fillAmount = 0f;   // 就绪：瞬时归零
                    break;
            }
        }
    }
}
