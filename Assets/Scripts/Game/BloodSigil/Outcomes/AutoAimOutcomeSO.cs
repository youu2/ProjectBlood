using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 自动瞄准锁敌结算效果（持续型）：将原本的"自动瞄准锁敌"基础能力改造为血印效果。
    // 激活期间玩家自动锁定视野内最近的敌人（35° 角度范围、墙遮挡检测、平滑过渡），
    // 结束后恢复为纯鼠标瞄准，不再自动吸附目标。
    //
    // 使用方式：绑定一个触发条件（如 OnAcquire/WeaponFired/DamageTaken）+ 结束条件（如 Duration/WeaponSwitched）。
    // 无需配置参数：角度/平滑速度等锁定行为参数统一由 Player 侧的静态常量控制，
    // 确保血印激活时的体验与原基础能力完全一致。
    [CreateAssetMenu(fileName = "Outcome_AutoAim", menuName = "血印系统/结算效果/自动瞄准锁敌")]
    public class AutoAimOutcomeSO : BloodSigilOutcomeSO
    {
        // 只修改 Player 实例上的自动瞄准开关：新场景 Player 重建后必须重新生效
        public override OutcomeScope Scope => OutcomeScope.PlayerInstance;

        public override void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            Enable(true);
        }

        public override void OnRemove(BloodSigilModuleRuntime rt)
        {
            Enable(false);
        }

        private static void Enable(bool on)
        {
            if (Player.player1 == null) return;
            Player.player1.AutoAimLockEnabled = on;
            // AimMark 是 Player 的实例字段（SpriteRenderer），Hide 为 QFramework 扩展方法，
            // 效果结束时通过 Player 实例隐藏瞄准标记，避免残留指示
            if (!on && Player.player1.AimMark != null)
            {
                Player.player1.AimMark.Hide();
            }
        }
    }
}
