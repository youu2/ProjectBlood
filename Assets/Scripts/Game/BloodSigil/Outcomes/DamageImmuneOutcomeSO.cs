using UnityEngine;

namespace ProjectBlood
{
    // 伤害免疫结算效果："免疫下一次/若干次伤害"。
    // 两种触发场景：
    //   1) 挂在 LethalDamage 触发的模块上（如不屈之血）：本次致命伤害由引擎的致命处理直接抵消
    //      （TryHandleLethalDamage 返回 true），本效果不再额外充能，避免多免一次；
    //   2) 挂在其他触发上（如受伤后获得 N 次免疫）：向引擎充能 N 次"下一次伤害免疫"，
    //      Player.TakeDamage 扣血前优先消耗该充能并完全免伤。
    //
    // 生命周期对称撤销：充能虽存入引擎共享池，但本效果按模块在状态袋中记录累计充入量，
    // 模块结束（Duration 到期/切枪换弹/血印移除）时在 OnRemove 中撤下该模块充入且尚未被消耗
    // 的部分；持续期间已被消耗的次数自然消失。献祭自我消耗(Consume)不走 OnRemove，充能保留。
    [CreateAssetMenu(fileName = "Outcome_DamageImmune", menuName = "血印系统/结算效果/伤害免疫")]
    public class DamageImmuneOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("每次触发充能的免疫次数（通常为 1）")]
        [Min(1)] public int charges = 1;

        // 运行时状态键：本模块累计充入引擎免疫池的次数（刷新再触发会累加）
        private const string GrantedKey = "DamageImmune_Granted";

        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            // 致命触发时当前这次伤害已被引擎抵消，无需再充能
            if (ctx.TriggerType == BloodSigilTriggerType.LethalDamage) return;
            if (charges <= 0) return;

            BloodSigilState.AddDamageImmunityCharges(charges);
            rt.SetState(this, GrantedKey, rt.GetState<int>(this, GrantedKey) + charges);
        }

        public override void OnRemove(BloodSigilModuleRuntime rt)
        {
            int granted = rt.GetState<int>(this, GrantedKey);
            if (granted <= 0) return;
            // 撤下本模块充入的次数；引擎按池余量夹取，期间已消耗的部分不会多扣
            BloodSigilState.RemoveDamageImmunityCharges(granted);
            rt.SetState(this, GrantedKey, 0);
        }
    }
}
