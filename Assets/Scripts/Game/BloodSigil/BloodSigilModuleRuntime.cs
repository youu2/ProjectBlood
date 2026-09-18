using System.Collections.Generic;

namespace ProjectBlood
{
    // 单个血印模块（一条 BloodSigilEffect 配置）的运行时状态。
    // SO 配置（trigger/outcomes/endConditions/maxStacks）跨局共享，所有可变数据集中于此：
    //   firedCount     —— 已触发次数（MaxStacks 控制）
    //   active         —— 持续型结算效果当前是否处于激活状态
    //   结束条件跟踪   —— 每个 Duration 条件的剩余秒数、事件条件（切枪/换弹）是否已发生
    //   outcomeStates  —— 结算效果自定义状态袋（叠层数值等），键由效果类自行约定
    public class BloodSigilModuleRuntime
    {
        public BloodSigilSO Sigil { get; }
        public BloodSigilEffect Module { get; }

        // 已触发次数
        public int FiredCount { get; set; }

        // 持续型效果是否激活中
        public bool Active { get; set; }

        // 边沿触发类条件（血量阈值）上次评估结果：true=当前已满足（停留在区间内），
        // 仅在条件由 false→true 的跨越瞬间允许 Fire；离开区间自动复位以支持反复跨越
        public bool TriggerLatched { get; set; }

        // 结束条件运行时状态（与 Module.endConditions 按索引一一对应；null 表示该条件无需计时）
        public float[] EndRemaining { get; }
        public bool[] EndLatched { get; }

        // 结算效果自定义状态袋：复合键 = OutcomeKey + "|" + stateKey
        private readonly Dictionary<string, object> outcomeStates = new Dictionary<string, object>();

        public BloodSigilModuleRuntime(BloodSigilSO sigil, BloodSigilEffect module)
        {
            Sigil = sigil;
            Module = module;
            int endCount = module.endConditions != null ? module.endConditions.Count : 0;
            EndRemaining = new float[endCount];
            EndLatched = new bool[endCount];
        }

        // 是否还能继续触发（maxStacks < 0 表示无限制）
        public bool CanFire(int maxStacks)
            => maxStacks < 0 || FiredCount < maxStacks;

        // 初始化/刷新结束条件计时与事件锁存（每次触发时调用）
        public void ResetEndTracking()
        {
            var ends = Module.endConditions;
            if (ends == null) return;
            for (int i = 0; i < ends.Count; i++)
            {
                var end = ends[i];
                EndRemaining[i] = end != null && end.endConditionType == BloodSigilEndConditionType.Duration
                    ? end.duration
                    : 0f;
                EndLatched[i] = false;
            }
        }

        // 推进 Duration 条件计时（仅在激活且存在 Duration 条件时由引擎调用）
        public void TickEndTimers(float deltaTime)
        {
            var ends = Module.endConditions;
            if (ends == null) return;
            for (int i = 0; i < ends.Count; i++)
            {
                if (ends[i] != null && ends[i].endConditionType == BloodSigilEndConditionType.Duration && EndRemaining[i] > 0f)
                {
                    EndRemaining[i] -= deltaTime;
                    if (EndRemaining[i] < 0f) EndRemaining[i] = 0f;
                }
            }
        }

        // 锁存一个游戏事件类结束条件（切枪/换弹）
        public void LatchEndEvent(BloodSigilEndConditionType eventType)
        {
            var ends = Module.endConditions;
            if (ends == null) return;
            for (int i = 0; i < ends.Count; i++)
            {
                if (ends[i] != null && ends[i].endConditionType == eventType)
                    EndLatched[i] = true;
            }
        }

        // 是否包含指定类型的结束条件（引擎据此只在相关事件中评估对应模块，避免无谓遍历）
        public bool HasEndType(BloodSigilEndConditionType type)
        {
            var ends = Module.endConditions;
            if (ends == null) return false;
            for (int i = 0; i < ends.Count; i++)
            {
                if (ends[i] != null && ends[i].endConditionType == type) return true;
            }
            return false;
        }

        // 按匹配模式判断结束条件是否满足（由引擎在 Tick / 事件后调用）。
        // healthPercent：本次评估携带的当前血量百分比（0~1）；传负数表示本次评估不涉及血量
        // （如 Tick 推进时），HealthThreshold 条件一律按未满足处理，它只在血量变化事件中评估。
        public bool IsEndSatisfied(BloodSigilEndMatchMode mode, float healthPercent = -1f)
        {
            var ends = Module.endConditions;
            if (ends == null || ends.Count == 0) return false; // 无条件 = 永不主动结束

            bool any = false;
            bool all = true;
            for (int i = 0; i < ends.Count; i++)
            {
                bool satisfied = EvaluateSingle(i, healthPercent);
                any |= satisfied;
                all &= satisfied;
            }
            return mode == BloodSigilEndMatchMode.Any ? any : all;
        }

        private bool EvaluateSingle(int index, float healthPercent)
        {
            var end = Module.endConditions[index];
            if (end == null) return false;
            switch (end.endConditionType)
            {
                case BloodSigilEndConditionType.Unlimited:
                    return false; // 永不满足（只有血印移除能结束含此条件的 All 组合）
                case BloodSigilEndConditionType.Duration:
                    return EndRemaining[index] <= 0f;
                case BloodSigilEndConditionType.WeaponSwitched:
                case BloodSigilEndConditionType.Reload:
                    return EndLatched[index];
                case BloodSigilEndConditionType.HealthThreshold:
                    // 电平判定：仅在携带血量的评估（血量变化事件/解锁初评）中生效
                    if (healthPercent < 0f) return false;
                    return BloodSigilHealthThreshold.Evaluate(
                        healthPercent, end.healthCompare, end.healthThreshold);
                default:
                    return false;
            }
        }

        // ===== 结算效果状态袋 =====

        public T GetState<T>(BloodSigilOutcomeSO outcome, string key, T defaultValue = default)
        {
            if (outcomeStates.TryGetValue(BuildKey(outcome, key), out var v) && v is T typed)
                return typed;
            return defaultValue;
        }

        public void SetState(BloodSigilOutcomeSO outcome, string key, object value)
        {
            outcomeStates[BuildKey(outcome, key)] = value;
        }

        private static string BuildKey(BloodSigilOutcomeSO outcome, string key)
            => (outcome != null ? outcome.OutcomeKey : "null") + "|" + key;
    }
}
