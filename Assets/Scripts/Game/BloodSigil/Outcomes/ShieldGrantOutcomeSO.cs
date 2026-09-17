using UnityEngine;

namespace ProjectBlood
{
    // 获得护盾结算效果：复用 Player.ActivateShield / ShieldState（格挡次数 + 无敌宽限期）。
    [CreateAssetMenu(fileName = "Outcome_Shield", menuName = "血印系统/结算效果/获得护盾")]
    public class ShieldGrantOutcomeSO : BloodSigilOutcomeSO
    {
        [Tooltip("护盾可抵挡的伤害次数")]
        [Min(1)] public int blockCount = 5;

        [Tooltip("护盾无敌宽限期（秒）")]
        [Min(0f)] public float duration = 5f;

        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            if (Player.player1 != null)
            {
                Player.player1.ActivateShield(blockCount, duration);
            }
        }
    }
}
