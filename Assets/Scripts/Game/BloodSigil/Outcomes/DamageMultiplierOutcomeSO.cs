using UnityEngine;

namespace ProjectBlood
{
    // 伤害系数结算效果（持续型，参与 GetFinalDamageRatio 统一乘区）。
    // 两种堆叠模式：
    //   Fixed      —— 激活期间固定 +amount（限时增伤；重复触发只刷新持续时间，不累加）
    //   Accumulate —— 每次触发累加 perFireAdd，上限 cap（连射递增；切枪/换弹结束即清零）
    // 武器掩码为 None(0) 时表示对全部武器生效；否则仅对勾选武器生效。
    [CreateAssetMenu(fileName = "Outcome_DamageMultiplier", menuName = "血印系统/结算效果/伤害系数")]
    public class DamageMultiplierOutcomeSO : BloodSigilOutcomeSO
    {
        public enum StackMode
        {
            // 固定值（重复触发刷新时长）
            Fixed = 0,
            // 逐次累加（上限封顶）
            Accumulate = 1,
        }

        [Tooltip("堆叠模式")]
        public StackMode stackMode = StackMode.Fixed;

        [Tooltip("Fixed 模式的固定增伤比例（0.3 = +30%）")]
        public float amount = 0.3f;

        [Tooltip("Accumulate 模式：每次触发增加的比例（0.02 = +2%）")]
        public float perFireAdd = 0.02f;

        [Tooltip("Accumulate 模式：累加上限（0.30 = 最高 +30%）")]
        [Min(0f)] public float cap = 0.30f;

        [Tooltip("生效武器掩码；None(不勾选任何武器) 表示对全部武器生效")]
        public WeaponTypeFlags targetWeapons = WeaponTypeFlags.None;

        private const string AccumKey = "DamageMultiplier_Accum";

        public override void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            // 首次触发：Accumulate 从第一层开始，Fixed 无需状态
            if (stackMode == StackMode.Accumulate)
            {
                rt.SetState(this, AccumKey, Mathf.Min(cap, perFireAdd));
            }
        }

        public override void OnRefresh(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            if (stackMode != StackMode.Accumulate) return;
            float current = rt.GetState<float>(this, AccumKey);
            rt.SetState(this, AccumKey, Mathf.Min(cap, current + perFireAdd));
        }

        public override float GetDamageMultiplier(BloodSigilModuleRuntime rt, WeaponType weapon)
        {
            if (targetWeapons != WeaponTypeFlags.None && !targetWeapons.Contains(weapon))
                return 1f;

            if (stackMode == StackMode.Fixed) return 1f + amount;

            return 1f + rt.GetState<float>(this, AccumKey);
        }
    }
}
