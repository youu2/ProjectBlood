using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectBlood
{
    /// <summary>
    /// 暂停页技能槽位的悬停交互（与血印的 PauseSigilSlot 对称）：
    /// 鼠标移入时在详情文本框显示"技能名(CD)+空行+描述"，移出时隐藏详情。
    /// 由 PausePageController 在打开暂停页填充技能槽位时挂载并初始化。
    /// </summary>
    public class PauseSkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SkillData skillData;          // 本槽位对应的技能配置
        private float chargeInterval;         // 减免后的最终充能间隔（CD）
        private string keyHint;               // 键位提示（如 "[F]"，来自 SkillManager 键位配置）
        private TextMeshProUGUI detailText;   // 技能详情文本框

        public void Initialize(SkillData skill, float cd, string key, TextMeshProUGUI detailLabel)
        {
            skillData = skill;
            chargeInterval = cd;
            keyHint = key;
            detailText = detailLabel;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (skillData == null || detailText == null) return;

            detailText.text =
                $"{skillData.skillName}{keyHint}({chargeInterval:F1}S)\n\n{skillData.description}";
            detailText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (detailText != null)
            {
                detailText.gameObject.SetActive(false);
            }
        }
    }
}
