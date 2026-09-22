// Boss 血条：数据源 Global.BossCurrentHp
// Boss 不在场时隐藏整个节点；进入二阶段后即时条变色
// 双条掉血动画复用 HealthBarBase
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public partial class BossHealthBar : HealthBarBase
    {
        // [Tooltip("进入二阶段后即时血条的颜色")]
        // [SerializeField] private Color phaseTwoColor = new Color(1f, 0.45f, 0f, 1f);

        // // 缓存一阶段颜色，Boss 重置 / 退场时恢复
        // private Color mDefaultInstantColor;

        protected override Image InstantBar => BossBarRed;
        protected override Image DelayedBar => BossBarWhite;
        protected override BindableProperty<float> HpSource => Global.BossCurrentHp;
        protected override float GetMaxHp() => Global.BossMaxHp.Value;

        protected override void Awake()
        {
            // 先注册血量（基类），再缓存颜色并注册显隐 / 阶段变色
            base.Awake();
            // mDefaultInstantColor = BossBarGreen.color;

            // Boss 不在场时隐藏整个血条节点（初始 BossActive=false 会立即触发，节点需默认 Active）
            Global.BossActive.RegisterWithInitValue(active =>
            {
                if (gameObject.activeSelf != active)
                {
                    gameObject.SetActive(active);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            // // 二阶段即时条变色（BossBase.Awake 会先把 BossPhaseTwo 重置为 false，新 Boss 不会串色）
            // Global.BossPhaseTwo.RegisterWithInitValue(isPhaseTwo =>
            // {
            //     BossBarGreen.color = isPhaseTwo ? phaseTwoColor : mDefaultInstantColor;
            // }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}