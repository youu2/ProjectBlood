using UnityEngine;

namespace ProjectBlood
{
    // 伤害免疫结算效果："免疫下一次伤害"。
    // 两种触发场景：
    //   1) 挂在 LethalDamage 触发的模块上（如不屈之血）：本次致命伤害由引擎的致命处理直接抵消
    //      （TryHandleLethalDamage 返回 true），本效果不再额外充能，避免多免一次；
    //   2) 挂在其他触发上（如技能后获得一次免疫）：向引擎充能 1 次"下一次伤害免疫"，
    //      Player.TakeDamage 扣血前优先消耗该充能并完全免伤。
    [CreateAssetMenu(fileName = "Outcome_DamageImmune", menuName = "血印系统/结算效果/伤害免疫")]
    public class DamageImmuneOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("每次触发充能的免疫次数（通常为 1）")]
        [Min(1)] public int charges = 1;

        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            // 致命触发时当前这次伤害已被引擎抵消，无需再充能
            if (ctx.TriggerType == BloodSigilTriggerType.LethalDamage) return;
            BloodSigilState.AddDamageImmunityCharges(charges);
        }
    }
}
