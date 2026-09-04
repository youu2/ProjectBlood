using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 绑定技能图标和冷却遮罩，根据技能管理器更新冷却显示
/// </summary>
public class SkillCooldownUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("冷却遮罩 Image(Fill 类型)")]
    public Image cooldownOverlay;

    [Tooltip("可选：显示冷却数字的 Text")]
    public TextMeshProUGUI cooldownText;

    [Header("技能设置")]
    [Tooltip("要显示冷却的技能名称")]
    public string skillName = "翻滚";

    // 技能管理器引用
    private SkillManager skillManager;

    private void Start()
    {
        // 查找玩家身上的技能管理器
        // 如果你的玩家对象名称不同，请相应调整
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            skillManager = player.GetComponent<SkillManager>();
        }
        else
        {
            Debug.LogWarning("未找到标签为 Player 的对象，请为玩家设置标签或手动赋值 skillManager");
        }
    }

    private void Update()
    {
        // GameUI 是 DontDestroyOnLoad，场景重载后旧 Player 被销毁，引用失效时需重新查找
        if (skillManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillManager = player.GetComponent<SkillManager>();
            }
            if (skillManager == null) return;
        }

        // 获取剩余冷却比例（0表示冷却完毕，1表示刚使用）
        float cooldownPercent = skillManager.GetCooldownPercent(skillName);

        // 更新遮罩填充量：冷却中时填充量从1递减到0
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 1f - cooldownPercent; // 冷却完成度越高，遮罩越小
        }

        // 更新冷却数字（可选）
        if (cooldownText != null)
        {
            float remaining = skillManager.GetRemainingCooldown(skillName);
            if (remaining > 0f)
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