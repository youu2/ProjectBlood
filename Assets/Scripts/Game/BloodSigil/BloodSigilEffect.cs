using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印效果模块（四要素组合模板）：
    //   触发条件 trigger       —— 什么游戏事件导致本模块生效
    //   结算效果 outcomes      —— 生效时执行什么（可多个同时结算）
    //   结束条件 endConditions —— 持续型结算效果何时结束（可多个，按 EndMatchMode 任一/全部）
    //   可生效次数 maxStacks   —— 触发上限（-1 = 无限制，1 = 仅一次）
    //
    // 运行时状态机（见 BloodSigilState / BloodSigilModuleRuntime）：
    //   事件到达 → 匹配 trigger 且次数未满 → Fire：
    //     · 未激活：持续型 outcomes.OnApply 生效，进入激活态并开始结束条件跟踪
    //     · 已激活：outcomes.OnRefresh（刷新时长 / 累加叠层）
    //     · 每次：瞬时 outcomes.OnImmediate 执行
    //   结束条件满足 → 持续型 outcomes.OnRemove 对称撤销，退出激活态（次数计数保留）
    //   血印被移除 → 所有激活模块强制结束
    //
    // 本类为 SO 配置资产，Create > 血印系统 > 效果模块 创建；
    // 一个 BloodSigilSO 可挂多个本模块，运行时互相独立计数与计时。
    [CreateAssetMenu(fileName = "SigilModule_", menuName = "血印系统/效果模块", order = 1)]
    public class BloodSigilEffect : ScriptableObject
    {
        [Header("① 触发条件（必配）")]
        [Tooltip("什么游戏事件触发本模块")]
        public BloodSigilTriggerSO trigger;

        [Header("② 结算效果（可配多个，触发时同时结算）")]
        [Tooltip("触发时执行的结算效果列表")]
        public List<BloodSigilOutcomeSO> outcomes = new List<BloodSigilOutcomeSO>();

        [Header("③ 结束条件（可配多个；留空或仅 Unlimited = 不主动结束）")]
        [Tooltip("持续型结算效果的结束条件列表")]
        public List<BloodSigilEndConditionSO> endConditions = new List<BloodSigilEndConditionSO>();

        [Tooltip("多个结束条件的匹配方式：满足任一 / 必须全部满足")]
        public BloodSigilEndMatchMode endMatchMode = BloodSigilEndMatchMode.Any;

        [Header("④ 可生效次数")]
        [Tooltip("本模块最多可触发的次数；-1 表示无限制，1 表示整局仅一次")]
        [Min(-1)] public int maxStacks = -1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (trigger == null)
                Debug.LogWarning($"[BloodSigilEffect] {name} 未配置触发条件", this);
            if (outcomes.Count == 0)
                Debug.LogWarning($"[BloodSigilEffect] {name} 未配置任何结算效果", this);
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] == null)
                    Debug.LogWarning($"[BloodSigilEffect] {name} 第 {i} 个结算效果为空", this);
            }
        }
#endif
    }
}
