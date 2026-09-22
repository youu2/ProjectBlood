// 玩家血条：数据源 Global.currentHP，双条掉血动画在 HealthBarBase 中
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public partial class HealthBar : HealthBarBase
    {
        protected override Image InstantBar => HealthBarGreen;
        protected override Image DelayedBar => HealthBarRed;
        protected override BindableProperty<float> HpSource => Global.currentHP;
        protected override float GetMaxHp() => Global.INGAME_MAX_HP.Value;

        // 玩家血条保留重构前的整数取整换算（像素刻度感）
        protected override float CalculateFill(float currentHp)
        {
            return Mathf.FloorToInt(currentHp) / Global.INGAME_MAX_HP.Value;
        }
    }
}