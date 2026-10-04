using UnityEngine;

namespace ProjectBlood
{
    // 切换武器结算效果（瞬时型）：触发时立即切换到下一把已持有武器，
    // 等效于玩家按一次 E 键 / 鼠标滚轮向下。武器数量任意，由 Player 侧模运算循环。
    //
    // 复用 Player.SwitchToNextWeapon 的完整切枪流程（保存旧枪状态、加载新枪、音效、
    // 相机尺寸、瞄准矫正），并经由 UseWeapon 内的 BloodSigilState.NotifyWeaponSwitched
    // 正常派发切枪事件，因此切枪触发/切枪结束的其他血印模块也能联动。
    //
    // 触发频率节流请配置所属模块的"触发冷却 triggerCooldownSeconds"（模块级通用属性），
    // 本结算效果自身无参数。
    [CreateAssetMenu(fileName = "Outcome_SwitchWeapon", menuName = "血印系统/结算效果/切换下一把武器")]
    public class SwitchWeaponOutcomeSO : BloodSigilOutcomeSO
    {
        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            Player.player1?.SwitchToNextWeapon();
        }
    }
}
