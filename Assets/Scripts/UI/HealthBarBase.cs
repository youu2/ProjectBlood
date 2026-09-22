// 血条基类：封装"即时血条 + 延迟平滑血条"的双条掉血反馈
// 子类只需提供数据源（BindableProperty）、最大生命值和两个 Image，
// 玩家血条 / Boss 血条共用同一套掉血手感参数（延迟、平滑时长），避免两份副本调参时漂移
using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public abstract class HealthBarBase : ViewController
    {
        private Coroutine mDelayedBarCoroutine;

        // 延迟扣血显示时间，让玩家看清刚损失的血量（子类可重写调整手感）
        protected virtual float DelaySeconds => 1f;
        // 延迟条平滑追赶时长
        protected virtual float SmoothDuration => 0.3f;

        // 即时血条：血量变化时立刻更新
        protected abstract Image InstantBar { get; }
        // 延迟血条：等待后平滑追赶（露出"刚损失的血量"）
        protected abstract Image DelayedBar { get; }
        // 血量数据源（玩家：Global.currentHP；Boss：Global.BossCurrentHp）
        protected abstract BindableProperty<float> HpSource { get; }
        // 最大生命值，用于换算 fillAmount
        protected abstract float GetMaxHp();

        /// <summary>当前血量 → fillAmount。默认按比例并钳制在 0~1（含除零保护）；
        /// 玩家血条重写以保留原有的整数取整换算。</summary>
        protected virtual float CalculateFill(float currentHp)
        {
            float maxHp = GetMaxHp();
            return maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        }

        // 订阅放在 Awake：Boss 血条会根据 BossActive 禁用自身节点，
        // 若放 Start，节点被禁用后订阅永远无法注册（C# 事件回调不受节点激活状态影响）
        protected virtual void Awake()
        {
            HpSource.RegisterWithInitValue(currentHp =>
            {
                float fill = CalculateFill(currentHp);

                // 即时条：立即更新
                InstantBar.fillAmount = fill;

                // 延迟条：等待后平滑追赶
                if (mDelayedBarCoroutine != null) StopCoroutine(mDelayedBarCoroutine);
                mDelayedBarCoroutine = StartCoroutine(DelayAndSmoothDelayedBar(fill));
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        /// <summary>
        /// 延迟后平滑过渡延迟血条
        /// </summary>
        /// <param name="targetFill">延迟血条的目标 fillAmount</param>
        private IEnumerator DelayAndSmoothDelayedBar(float targetFill)
        {
            // 第一阶段：等待，延迟条保持原样，露出"刚刚损失的血量"
            yield return new WaitForSeconds(DelaySeconds);

            // 第二阶段：平滑过渡到目标值
            float startFill = DelayedBar.fillAmount;
            float elapsed = 0f;

            while (elapsed < SmoothDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / SmoothDuration);
                DelayedBar.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }

            DelayedBar.fillAmount = targetFill;
            mDelayedBarCoroutine = null;
        }
    }
}