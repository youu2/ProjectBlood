using ProjectBlood;
using UnityEngine;

/// <summary>
/// 慢动作效果：OnStart 当帧把 timeScale 降到目标值，OnEnd 无条件恢复为 1。
/// 持续时间按真实时间（unscaledDeltaTime）计时——本效果会修改 timeScale，
/// 用缩放时间会永远走不完。
/// 恢复为单一出口 + 幂等：自然结束、earlyFinish、Interrupt 都只调一次 OnEnd。
/// </summary>
[CreateAssetMenu(fileName = "TimeScaleEffect", menuName = "技能系统/效果/慢动作效果")]
public class TimeScaleEffect : SkillEffect
{
    [Tooltip("慢动作时间缩放（0.2 = 世界速度变为 1/5）")]
    [Range(0.05f, 1f)]
    [SerializeField] private float targetTimeScale = 0.2f;

    // 持续型效果：开始前保持完成态，OnStart 后置 false，走满真实时长才完成
    private bool finished = true;
    // 幂等标记：是否实际设置过 timeScale（防止未生效/重复恢复）
    private bool applied = false;
    private float elapsed = 0f;

    public override bool IsDone => finished;

    public override void OnStart(EffectContext context)
    {
        finished = false;
        applied = true;
        elapsed = 0f;

        Time.timeScale = targetTimeScale;
        HiddenBladeSettings.IsExecuting = true;
    }

    public override void OnUpdate(EffectContext context)
    {
        if (finished) return;

        elapsed += Time.unscaledDeltaTime;
        float totalDuration = context.duration > 0f ? context.duration : 0.5f;
        if (elapsed >= totalDuration)
        {
            finished = true;
        }
    }

    public override void OnEnd(EffectContext context)
    {
        finished = true;
        if (!applied) return;
        applied = false;

        // 单一恢复出口：任何路径结束都恢复正常时间流速
        Time.timeScale = 1f;
        HiddenBladeSettings.IsExecuting = false;
    }
}
