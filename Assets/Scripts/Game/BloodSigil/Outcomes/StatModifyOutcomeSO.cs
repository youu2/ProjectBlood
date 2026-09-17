using UnityEngine;

namespace ProjectBlood
{
    // 数值修改结算效果（持续型）：模块激活期间修改基础属性，结束时对称撤销。
    // 复用 PlayerUpgradeState.ApplyStat（含下限保护 / 当前 HP 同步 / 血库量化逻辑）。
    // 适用：获得血印立即加属性（OnAcquire 触发 + Unlimited 结束），或限时/条件性属性增益。
    [CreateAssetMenu(fileName = "Outcome_Stat", menuName = "血印系统/结算效果/数值修改")]
    public class StatModifyOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("目标属性")]
        public StatType statType = StatType.MaxHP;

        [Tooltip("变化值，可为负（代价型）")]
        public float value = 10f;

        public override void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            PlayerUpgradeState.ApplyStat(statType, value);
        }

        public override void OnRemove(BloodSigilModuleRuntime rt)
        {
            PlayerUpgradeState.ApplyStat(statType, -value);
        }
    }
}
