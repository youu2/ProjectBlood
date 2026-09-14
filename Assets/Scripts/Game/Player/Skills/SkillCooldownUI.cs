using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 绑定技能图标和冷却遮罩,根据技能管理器更新冷却显示
/// </summary>
public class SkillCooldownUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("冷却遮罩 Image(Fill 类型)")]
    public Image cooldownOverlay;
    public Image chargeCDOverlay;
    public Image chargeCircle;

    [Tooltip("可选：显示冷却数字的 Text")]
    public TextMeshProUGUI cooldownText;

    [Tooltip("可选：显示充能层数的 Text")]
    public TextMeshProUGUI chargeText;

    [Header("技能设置")]
    [Tooltip("要显示冷却的技能名称")]
    public string skillName = "翻滚";

    // 技能管理器引用
    private SkillManager skillManager;

    private void Start()
    {
        // 查找玩家身上的技能管理器
        // 如果你的玩家对象名称不同,请相应调整
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            skillManager = player.GetComponent<SkillManager>();
        }
        else
        {
            Debug.LogWarning("未找到标签为 Player 的对象,请为玩家设置标签或手动赋值 skillManager");
        }
    }

    private void Update()
    {
        // GameUI 是 DontDestroyOnLoad,场景重载后旧 Player 被销毁,引用失效时需重新查找
        if (skillManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillManager = player.GetComponent<SkillManager>();
            }
            if (skillManager == null) return;
        }

        // 获取下一次充能进度(0=刚开始充能,1=已就绪/满充能)
        float chargeProgress = skillManager.GetCooldownPercent(skillName);



        // 更新充能层数与数字
        int currentCharges = skillManager.GetCurrentCharges(skillName);
        int maxCharges = skillManager.GetMaxCharges(skillName);

        // 层数小于1时更新遮罩填充量：充能中时填充量从1递减到0,提示玩家当前无法使用技能
        if (cooldownOverlay != null && currentCharges < 1)
        {
            cooldownOverlay.fillAmount = 1f - chargeProgress;
        }


        if (maxCharges > 1)
        {
            // 多层充能：显示 "当前/最大"
            if (chargeText != null) chargeText.text = $"{currentCharges}";
            if (chargeCircle != null) chargeCircle.Show();
            if (chargeCDOverlay != null) chargeCDOverlay.fillAmount = chargeProgress;
        }
        else
        {
            // 等效无充能层数CD制
            if (chargeText != null) chargeText.text = "";
            if (chargeCircle != null) chargeCircle.Hide();
            if (chargeCDOverlay != null) chargeCDOverlay.Hide();
        }

        if (cooldownText != null)
        {
            float remaining = skillManager.GetRemainingCooldown(skillName);
            // 有层数时不显示cd
            if (remaining > 0f && currentCharges < 1)
            {
                cooldownText.text = remaining.ToString("F1");
            }
            else
            {
                cooldownText.text = "";
            }
        }
    }
}