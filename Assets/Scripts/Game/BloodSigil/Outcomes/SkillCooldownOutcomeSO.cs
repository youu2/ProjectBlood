using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 技能充能减免结算效果（持续型）：复用 PlayerUpgradeState.ApplySkillCooldownReduction，
    // 模块激活期间生效，结束时以相反数撤销（进行中充能倒计时的缩放逻辑直接继承）。
    [CreateAssetMenu(fileName = "Outcome_SkillCooldown", menuName = "血印系统/结算效果/技能充能减免")]
    public class SkillCooldownOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("目标技能名（与 SkillData.skillName 一致，可多个）")]
        public List<string> skillNames = new List<string> { "翻滚" };

        [Tooltip("充能时间减免比例（0.1 = 缩短 10%，可为负）")]
        public float reduction = 0.1f;

        public override void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            if (skillNames == null) return;
            foreach (var name in skillNames)
            {
                if (!string.IsNullOrEmpty(name))
                    PlayerUpgradeState.ApplySkillCooldownReduction(name, reduction);
            }
        }

        public override void OnRemove(BloodSigilModuleRuntime rt)
        {
            if (skillNames == null) return;
            foreach (var name in skillNames)
            {
                if (!string.IsNullOrEmpty(name))
                    PlayerUpgradeState.ApplySkillCooldownReduction(name, -reduction);
            }
        }
    }
}
