using UnityEngine;

[CreateAssetMenu(fileName = "MoveEffect", menuName = "技能系统/效果/位移效果")]
public class MoveEffect : SkillEffect
{
    [Header("位移参数")]
    public float speed = 10f;
    public float distance = 3f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    public override bool IsDone { get; protected set; }

    public override void OnStart(EffectContext context)
    {
        var state = context.GetOrCreateEffectState(this);
        state["elapsed"] = 0f;
        state["startPos"] = (Vector2)context.caster.transform.position;
        state["totalDisplacement"] = 0f;      // 新增累积位移
        IsDone = false;
    }

    public override void OnUpdate(EffectContext context)
    {
        var state = context.GetOrCreateEffectState(this);
        float elapsed = (float)state["elapsed"];
        Vector2 startPos = (Vector2)state["startPos"];
        float totalDisplacement = (float)state["totalDisplacement"];

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime;
        state["elapsed"] = elapsed;

        float totalDuration = context.duration > 0 ? context.duration : 0.5f;
        float t = Mathf.Clamp01(elapsed / totalDuration);
        float curveValue = speedCurve.Evaluate(t);
        float currentSpeed = speed * curveValue;

        // 计算本帧位移增量（积分）
        float deltaDistance = currentSpeed * deltaTime;
        totalDisplacement += deltaDistance;

        // 限制最大位移
        float maxDistance = distance > 0 ? distance : float.MaxValue;
        if (totalDisplacement > maxDistance)
        {
            totalDisplacement = maxDistance;
        }
        state["totalDisplacement"] = totalDisplacement;

        // 计算目标位置
        Vector2 newPosition = startPos + context.direction * totalDisplacement;

        // 应用移动
        Rigidbody2D rb = context.caster.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 使用 MovePosition 让物理处理碰撞
            rb.MovePosition(newPosition);
        }
        else
        {
            context.caster.transform.position = newPosition;
        }

        // 完成条件：时间到 或 达到最大距离
        if (elapsed >= totalDuration || totalDisplacement >= maxDistance)
        {
            IsDone = true;
        }
    }

    public override void OnEnd(EffectContext context)
    {
        IsDone = true;
    }
}