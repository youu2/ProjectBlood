using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    /// <summary>
    /// 暂停页(PausePage)专用逻辑：
    /// 1. Esc 键切换暂停/继续，继续按钮(BtnContinue)恢复游戏；
    /// 2. 打开暂停页时刷新一次：玩家信息、武器信息、血印信息、通关耗时；
    /// 3. 鼠标悬停血印图标时在 SigilDetail 显示血印名字与效果描述。
    /// 页面物体始终保持激活（脚本需要 Update 响应 Esc），可见性通过 CanvasGroup 控制。
    /// </summary>
    public class PausePageController : MonoBehaviour
    {
        // 武器行：武器图标物体 + 行信息文本 + 武器类型
        [System.Serializable]
        public class WeaponRow
        {
            public GameObject rowObject;              // 武器行物体，未拥有该武器时隐藏
            public TextMeshProUGUI dmgText;          // 行文本：攻击力(增伤%)
            public TextMeshProUGUI ammoText;         // 行文本：当前弹药/弹夹容量
            public WeaponType weaponType;             // 本行对应的武器类型
        }

        [Header("玩家信息文本")]
        [SerializeField] private TextMeshProUGUI timerText;               // 通关耗时
        [SerializeField] private TextMeshProUGUI levelText;               // 等级（当前经验/升级所需经验）
        [SerializeField] private TextMeshProUGUI hpText;                  // 生命值：当前/上限
        [SerializeField] private TextMeshProUGUI speedText;               // 移动速度
        [SerializeField] private TextMeshProUGUI bloodBankText;           // 血库容量：当前/上限
        [SerializeField] private TextMeshProUGUI skillCDText;             // 各技能标准CD
        [SerializeField] private TextMeshProUGUI globalDamageRateText;    // 全局增伤百分比

        [Header("武器")]
        [SerializeField] private Button continueButton;  // 继续游戏按钮
        [SerializeField] private Button quitButton;      // 退出游戏按钮
        [SerializeField] private WeaponRow[] weaponRows; // 全部武器行（未拥有的自动隐藏）

        [Header("血印")]
        [SerializeField] private GameObject[] sigilSlots;        // 血印槽位（共 10 个）
        [SerializeField] private TextMeshProUGUI sigilDetailText; // 血印详情文本（悬停时显示）

        private CanvasGroup canvasGroup;
        private bool isPaused;   // 当前是否处于本暂停页打开的状态

        private void Awake()
        {
            // CanvasGroup 未在预制体中预置时运行时补一个，用于整体隐藏页面但保持物体激活
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            HidePage();

            continueButton.onClick.AddListener(Resume);
            sigilDetailText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                TryPause();
            }
        }

        // 仅当玩家存活、且游戏未被其他系统（加载场景/升级选择/结算面板）占用时允许暂停
        private bool CanPause()
        {
            return !Global.IsGamePaused && Player.player1 != null;
        }

        // 进入暂停：冻结时间与玩家操作，刷新页面信息并显示
        private void TryPause()
        {
            if (!CanPause()) return;

            isPaused = true;
            Time.timeScale = 0f;
            Global.IsGamePaused = true;

            RefreshAll();
            ShowPage();
        }

        // 恢复游戏：还原时间流逝与玩家操作并隐藏页面
        private void Resume()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f;
            Global.IsGamePaused = false;

            sigilDetailText.gameObject.SetActive(false);
            HidePage();
        }

        // ============================== 信息刷新 ==============================

        private void RefreshAll()
        {
            RefreshPlayerInfo();
            RefreshWeaponRows();
            RefreshSigils();
        }

        // 玩家相关信息（数据源：Global 静态状态 / Player 实例 / BloodBank 单例）
        private void RefreshPlayerInfo()
        {
            timerText.text = "行动时长：" + FormatDuration(Global.RunElapsedSeconds);

            levelText.text = $"等级：{Global.Level.Value}（{Global.Exp.Value}/{Global.MAX_EXP.Value}）";

            hpText.text = $"生命值：{Mathf.FloorToInt(Global.currentHP.Value)}/{Mathf.FloorToInt(Global.INGAME_MAX_HP.Value)}";

            speedText.text = $"移动速度：{Player.player1.moveSpeed:0.0}";

            bloodBankText.text = $"血库容量：{BloodBank.Instance.CurrentBloodAmount}/{BloodBank.Instance.MaxBloodAmount}";

            globalDamageRateText.text =
                $"全局增伤：{Mathf.RoundToInt((PlayerUpgradeState.GlobalDamageRatio - 1f) * 100f)}%";

            RefreshSkillCD();
        }

        // 技能CD：列出所有技能的标准CD（基础充能间隔减去全部冷却减免后的最终常量，固定显示）
        private void RefreshSkillCD()
        {
            var builder = new StringBuilder("技能CD：");

            foreach (var skill in Player.player1.SelfSkillManager.GetAllSkills())
            {
                builder.Append(skill.Data.skillName)
                       .Append(skill.EffectiveChargeInterval.ToString("F1"))
                       .Append("S ");
            }

            skillCDText.text = builder.ToString().TrimEnd();
        }

        // 武器行：已拥有的武器显示"攻击力(增伤%)   当前弹药/弹夹容量"，未拥有的隐藏
        private void RefreshWeaponRows()
        {
            if (weaponRows == null || Player.player1 == null) return;

            foreach (var row in weaponRows)
            {
                // 引用缺失（预制体未绑上等）时跳过该行，避免空引用中断整页刷新
                if (row == null || row.rowObject == null || row.dmgText == null || row.ammoText == null)
                {
                    Debug.LogWarning("[PausePage] 存在未绑定的武器行引用，已跳过。请检查预制体 weaponRows 配置。");
                    continue;
                }

                bool owned = PlayerUpgradeState.IsWeaponOwned(row.weaponType);
                row.rowObject.SetActive(owned);
                if (!owned) continue;

                var weapon = Player.player1.GetWeapon(row.weaponType);
                if (weapon == null)
                {
                    Debug.LogWarning($"[PausePage] 玩家未持有武器 {row.weaponType} 的实例，跳过该行。");
                    row.rowObject.SetActive(false);
                    continue;
                }

                var clip = weapon.GetGunClip();

                // 单发子弹攻击力 = 子弹预制体基础伤害 × 最终伤害倍率
                // Laser 等无子弹预制体（或组件缺失）时按 0 处理，避免空引用
                var bullet = weapon.BulletPrefab != null
                    ? weapon.BulletPrefab.GetComponent<PlayerBullet>()
                    : null;
                float baseDamage = bullet != null ? bullet.damage : 0f;
                float ratio = PlayerUpgradeState.GetFinalDamageRatio(row.weaponType);
                int bonusPercent = Mathf.RoundToInt((ratio - 1f) * 100f);
                string sign = bonusPercent >= 0 ? "+" : "";

                // 刚获得枪时 clip 可能为 null，弹药部分显示为 "-"
                string ammoText = clip != null
                    ? $"{clip.currentAmmo}/{clip.maxAmmo}"
                    : "-/-";

                row.dmgText.text =
                    $"{Mathf.RoundToInt(baseDamage * ratio)}{sign}{bonusPercent}%";
                row.ammoText.text = ammoText;
            }
        }

        // 血印槽位：按"稀有度→名字"排序后依次填入，多余槽位隐藏
        private void RefreshSigils()
        {
            var unlocked = new List<BloodSigilSO>(BloodSigilState.UnlockedSigils);
            unlocked.Sort((a, b) =>
            {
                int byRarity = a.rarity.CompareTo(b.rarity);
                return byRarity != 0 ? byRarity : string.CompareOrdinal(a.sigilName, b.sigilName);
            });

            for (int i = 0; i < sigilSlots.Length; i++)
            {
                var slot = sigilSlots[i];

                if (i >= unlocked.Count)
                {
                    slot.SetActive(false);
                    continue;
                }

                var sigil = unlocked[i];
                slot.SetActive(true);

                var image = slot.GetComponent<Image>();
                image.sprite = sigil.icon;
                image.enabled = sigil.icon != null; // 无图标时隐藏 Image，避免显示白块

                // 悬停交互组件只挂载一次
                var hover = slot.GetComponent<PauseSigilSlot>();
                if (hover == null) hover = slot.AddComponent<PauseSigilSlot>();
                hover.Initialize(sigil, sigilDetailText);
            }
        }

        // 秒数格式化为 "分:秒"
        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainSeconds:00}";
        }

        // ============================== 页面显隐 ==============================

        private void ShowPage()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void HidePage()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}