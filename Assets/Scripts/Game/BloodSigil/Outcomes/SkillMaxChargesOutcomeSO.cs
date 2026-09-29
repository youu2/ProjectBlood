using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 技能最大充能层数结算效果（持续型）：将指定技能的最大充能层数设为/调整为某个数，
    // 例如把"翻滚"的充能层数变为 3。模块激活期间生效，结束时对称撤销（OnRemove 反向）。
    //
    // 实现路径（高内聚）：
    //   Outcome 只负责把"目标层数"换算为增量 delta 并复用 PlayerUpgradeState 的层数加成台账；
    //   真正的有效层数由 SkillBase.MaxCharges 聚合(基础 maxCharges + 加成)，
    //   当前层数/充能计时的过渡由 SkillBase.RefreshChargesAfterMaxChanged 处理。
    //
    // 两种模式：
    //   Additive  : 在基础层数上累加 delta（可正可负），适合"+1 层充能"类血印；
    //   SetTo     : 把最大层数设为目标值 targetCharges（内部换算为 delta = target - 基础）。
    [CreateAssetMenu(fileName = "Outcome_SkillMaxCharges", menuName = "血印系统/结算效果/技能充能层数")]
    public class SkillMaxChargesOutcomeSO : BloodSigilOutcomeSO
    {
        public enum Mode
        {
            Additive, // 在基础层数上累加 delta
            SetTo,    // 将最大层数设为 targetCharges
        }

        [Tooltip("目标技能名（与 SkillData.skillName 一致，可多个）")]
        public List<string> skillNames = new List<string> { "翻滚" };

        [Tooltip("修改模式：Additive=累加 delta；SetTo=设为目标层数")]
        public Mode mode = Mode.SetTo;

        [Tooltip("Additive 模式：层数增量（正=增加，负=减少）")]
        public int delta = 1;

        [Tooltip("SetTo 模式：目标最大层数（如把翻滚设为 3 层）")]
        [Min(1)] public int targetCharges = 3;

        public override void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            Apply(rt, true);
        }

        public override void OnRemove(BloodSigilModuleRuntime rt)
        {
            Apply(rt, false);
        }

        // sign=true 应用加成，false 撤销。SetTo 的 delta 依赖技能基础层数，故按技能逐个换算。
        private void Apply(BloodSigilModuleRuntime rt, bool apply)
        {
            if (skillNames == null) return;
            foreach (var name in skillNames)
            {
                if (string.IsNullOrEmpty(name)) continue;

                int amount = mode == Mode.Additive ? delta : ComputeSetToDelta(name);
                if (amount == 0) continue;
                PlayerUpgradeState.ApplySkillMaxChargesBonus(name, apply ? amount : -amount);
            }
        }

        // SetTo 模式：目标层数 - 技能基础层数(SkillData.maxCharges)，得到需要施加的增量
        private int ComputeSetToDelta(string skillName)
        {
            int baseMax = 1;
            var skillManager = Player.player1 != null ? Player.player1.GetComponent<SkillManager>() : null;
            var skill = skillManager != null ? skillManager.GetSkillByName(skillName) : null;
            if (skill != null && skill.Data != null) baseMax = Mathf.Max(1, skill.Data.maxCharges);
            return targetCharges - baseMax;
        }
    }
}
