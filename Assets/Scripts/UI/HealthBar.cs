using System.Collections;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class HealthBar : ViewController
    {
        private Coroutine mRedBarCoroutine;
        private float delaySeconds = 1f; // 延迟扣血量显示时间，让玩家看清;
        private float smoothDuration = 0.3f; // 平滑平滑过渡时长;

        void Start()
        {
            Global.currentHP.RegisterWithInitValue(currentHP =>
            {
                float greenFill = Mathf.FloorToInt(currentHP) / Global.INGAME_MAX_HP.Value;

                // 绿色血条：立即更新
                HealthBarGreen.fillAmount = greenFill;

                // 红色血条：延迟 1 秒后平滑追赶
                if (mRedBarCoroutine != null) StopCoroutine(mRedBarCoroutine);
                mRedBarCoroutine = StartCoroutine(DelayAndSmoothRedBar(greenFill, delaySeconds, smoothDuration));
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        /// <summary>
        /// 延迟后平滑过渡红色血条
        /// </summary>
        /// <param name="targetFill">红色血条的目标 fillAmount</param>
        /// <param name="delaySeconds">延迟秒数（让玩家看清扣血量）</param>
        /// <param name="smoothDuration">平滑过渡时长</param>
        private IEnumerator DelayAndSmoothRedBar(float targetFill, float delaySeconds, float smoothDuration)
        {
            // 第一阶段：等待，红色条保持原样，露出"刚刚损失的血量"
            yield return new WaitForSeconds(delaySeconds);

            // 第二阶段：平滑过渡到目标值
            float startFill = HealthBarRed.fillAmount;
            float elapsed = 0f;

            while (elapsed < smoothDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / smoothDuration);
                HealthBarRed.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }

            HealthBarRed.fillAmount = targetFill;
            mRedBarCoroutine = null;
        }
    }
}