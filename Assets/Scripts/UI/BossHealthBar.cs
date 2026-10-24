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
        protected override Image InstantBar => BossBarRed;
        protected override Image DelayedBar => BossBarWhite;
        protected override BindableProperty<float> HpSource => Global.BossCurrentHp;
        protected override float GetMaxHp() => Global.BossMaxHp.Value;

        protected override void Awake()
        {
            // 先注册血量（基类），再注册显隐
            base.Awake();

            // Boss 不在场时隐藏整个血条节点（初始 BossActive=false 会立即触发，节点需默认 Active）
            Global.BossActive.RegisterWithInitValue(active =>
            {
                if (gameObject.activeSelf != active)
                {
                    gameObject.SetActive(active);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}