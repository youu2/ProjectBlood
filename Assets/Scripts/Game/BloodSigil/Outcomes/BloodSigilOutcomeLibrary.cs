using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印结算效果注册表（SO 资产）：OutcomeKey -> 效果资产模板。
    // 用途：表格/JSON 驱动配置时，一行配置可写 "OutcomeKey=DamageMultiplier" 等稳定键，
    //       由注册表查得对应效果资产模板，再结合行内数值参数实例化/填充。
    // 使用方式：创建一个 BloodSigilOutcomeLibrary 资产放 Resources 或指定目录，
    //          将内置/自定义结算效果资产登记进列表；BloodSigilManager 持有其引用。
    [CreateAssetMenu(fileName = "BloodSigilOutcomeLibrary", menuName = "血印系统/结算效果注册表", order = 12)]
    public class BloodSigilOutcomeLibrary : ScriptableObject
    {
        [Tooltip("登记所有可被表格/JSON 按 OutcomeKey 引用的结算效果模板")]
        [SerializeField] private List<BloodSigilOutcomeSO> outcomes = new List<BloodSigilOutcomeSO>();

        private Dictionary<string, BloodSigilOutcomeSO> lookup;

        private void BuildLookup()
        {
            lookup = new Dictionary<string, BloodSigilOutcomeSO>();
            foreach (var outcome in outcomes)
            {
                if (outcome == null) continue;
                lookup[outcome.OutcomeKey] = outcome;
            }
        }

        // 按注册键查询效果模板；找不到返回 null
        public BloodSigilOutcomeSO Get(string outcomeKey)
        {
            if (lookup == null) BuildLookup();
            return string.IsNullOrEmpty(outcomeKey) ? null
                : (lookup.TryGetValue(outcomeKey, out var o) ? o : null);
        }
    }
}
