using UnityEngine;

namespace ProjectBlood
{
    // 献祭结算效果（瞬时特殊行为）：移除所有其他可移除血印，
    // 每失去一个提供最大生命值 + 永久增伤，然后献祭血印自身消失（不触发对称撤销）。
    // 配置方式：模块触发条件 = OnAcquire，结束条件 = Unlimited，MaxStacks = 1；
    //           血印自身的 Removable 必须关闭，避免被其他献祭计入。
    [CreateAssetMenu(fileName = "Outcome_Sacrifice", menuName = "血印系统/结算效果/血印献祭")]
    public class SacrificeOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("每失去一个血印提供的最大生命值")]
        public float maxHpPerSigil = 10f;

        [Tooltip("每失去一个血印提供的永久增伤比例（0.05 = +5%）")]
        public float damageBonusPerSigil = 0.05f;

        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            // 幂等保护：MaxStacks=1 已保证只触发一次，运行时再兜底
            if (rt.GetState<bool>(this, "Done")) return;
            rt.SetState(this, "Done", true);

            // 1. 移除所有其他可移除血印（自身 removable=false 不会计入）
            var lost = BloodSigilState.RemoveAll(removableOnly: true);
            int count = lost.Count;

            if (count > 0)
            {
                // 2. 结算收益：最大生命走 ApplyStat；增伤写入永久台账（血印消失后仍保留）
                PlayerUpgradeState.ApplyStat(StatType.MaxHP, maxHpPerSigil * count);
                BloodSigilState.AddPermanentDamageBonus(damageBonusPerSigil * count);
            }

            // 3. 献祭血印自身消失（不触发 OnRemove，避免撤销收益）
            BloodSigilState.Consume(rt.Sigil);
        }
    }
}
