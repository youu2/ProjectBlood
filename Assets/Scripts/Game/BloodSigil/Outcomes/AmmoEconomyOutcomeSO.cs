using UnityEngine;

namespace ProjectBlood
{
    // 弹药经济结算效果（瞬时型）：直接修改所选武器当前弹夹内的子弹数。
    // "所选武器"取值顺序：
    //   1) 触发上下文中的 ctx.Weapon（WeaponFired / WeaponSwitched 等事件附带的武器）；
    //   2) 退回到 Player.player1.currentWeapon（OnAcquire / SkillCast / UnitKilled 等无武器上下文的触发）。
    // 四种修改模式：
    //   AddFixed        —— 在当前子弹数基础上加固定数量（封顶 maxAmmo）
    //   AddPercentOfMax —— 加 maxAmmo 的 amount 比例（0.5=补一半弹夹）
    //   SetToPercent    —— 直接设为 maxAmmo 的 amount 比例（1=满弹）
    //   Refill          —— 装满弹夹（忽略 amount）
    // 换弹进行中跳过修改（FinishReload 会将 currentAmmo 重置为 maxAmmo，此时修改会被覆盖且无意义）；
    // 修改后刷新弹药 HUD 并 SaveWeaponData 以便跨切枪保留。
    [CreateAssetMenu(fileName = "Outcome_AmmoEconomy", menuName = "血印系统/结算效果/弹药经济")]
    public class AmmoEconomyOutcomeSO : BloodSigilOutcomeSO
    {
        public enum Mode
        {
            // 在当前基础上加固定数量
            AddFixed = 0,
            // 加 maxAmmo 的百分比
            AddPercentOfMax = 1,
            // 直接设为 maxAmmo 的百分比
            SetToPercent = 2,
            // 装满弹夹
            Refill = 3,
        }

        [Tooltip("修改模式")]
        public Mode mode = Mode.AddFixed;

        [Tooltip("AddFixed=增加的子弹数（负值用于诅咒血印扣弹）；AddPercentOfMax/SetToPercent=占 maxAmmo 的比例（0.5=50%）；Refill 忽略")]
        public float amount = 3f;

        public override void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            // 选定目标武器：优先事件附带的武器，否则回退到玩家当前武器
            WeaponBase weapon = ctx.Weapon;
            if (weapon == null)
            {
                weapon = Player.player1 != null ? Player.player1.currentWeapon : null;
            }
            if (weapon == null) return;

            GunClip clip = weapon.GetGunClip();
            if (clip == null || clip.isReloading) return; // 换弹中跳过，避免被 FinishReload 覆盖

            int max = clip.maxAmmo;
            if (max <= 0) return;
            int current = clip.currentAmmo;

            int newValue;
            switch (mode)
            {
                case Mode.AddFixed:
                    newValue = current + Mathf.RoundToInt(amount);
                    break;
                case Mode.AddPercentOfMax:
                    newValue = current + Mathf.RoundToInt(max * amount);
                    break;
                case Mode.SetToPercent:
                    newValue = Mathf.RoundToInt(max * amount);
                    break;
                case Mode.Refill:
                    newValue = max;
                    break;
                default:
                    return;
            }

            // 夹到 [0, max]，无变化不刷新
            newValue = Mathf.Clamp(newValue, 0, max);
            if (newValue == current) return;

            clip.currentAmmo = newValue;
            clip.UpdateClipUI();
            // 持久化跨切枪；Data 未加载时跳过以避免日志噪音
            if (weapon.Data != null)
            {
                weapon.SaveWeaponData();
            }
        }
    }
}
