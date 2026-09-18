using UnityEngine;

namespace ProjectBlood
{
    // 血量阈值比较运算符（触发条件与结束条件共用）
    public enum BloodSigilHealthCompare
    {
        // 当前血量百分比 > 阈值
        GreaterThan = 0,
        // 当前血量百分比 ≈ 阈值（小容差，满血=1 这类恰好相等的场景可靠命中）
        Equal = 1,
        // 当前血量百分比 < 阈值
        LessThan = 2,
    }

    // 血量阈值判定共享逻辑：触发条件(BloodSigilTriggerSO)与结束条件(BloodSigilEndConditionSO)
    // 都通过本类评估"当前血量百分比 vs 配置阈值"，保证两侧比较语义/容差完全一致。
    public static class BloodSigilHealthThreshold
    {
        // Equal 比较容差（百分点）：血量为浮点值，精确相等几乎不可能命中
        public const float EqualTolerance = 0.005f;

        // percent：当前血量百分比（0~1）；threshold：配置阈值（0~1）
        public static bool Evaluate(float percent, BloodSigilHealthCompare op, float threshold)
        {
            switch (op)
            {
                case BloodSigilHealthCompare.GreaterThan:
                    return percent > threshold;
                case BloodSigilHealthCompare.Equal:
                    return Mathf.Abs(percent - threshold) <= EqualTolerance;
                case BloodSigilHealthCompare.LessThan:
                    return percent < threshold;
                default:
                    return false;
            }
        }
    }
}
