using UnityEngine;

[CreateAssetMenu(fileName = "MoveEffect", menuName = "技能系统/效果/位移效果")]
public class MoveEffect : SkillEffect
{
    [Header("位移参数")]
    public float speed = 10f;
    public float distance = 3f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    // 运行时状态（每个效果实例独立，这里利用协程控制，但这些状态需要在调用者处存储）
    // 由于 ScriptableObject 不能在多个对象上同时使用不同状态，我们会使用 context 里的一个临时数据容器。
    // 暂时我们先这样设计，在后续的执行器中会为每个效果实例维护一份状态。
    // 为了让持续效果正确工作，我们会在执行器中处理状态管理，这里只负责逻辑。

    // 该效果是持续效果，所以 IsDone 默认返回 false，直到完成
    public override bool IsDone { get; protected set; }

    // 运行时内部变量（由执行器通过上下文传递的状态存储）
    private float elapsedTime;
    private Vector2 startPosition;
    private bool initialized;

    public override void OnStart(EffectContext context)
    {
        // 从上下文获取状态存储（执行器会预先设置好）
        var state = context.GetOrCreateEffectState(this);
        state["elapsed"] = 0f;
        state["startPos"] = (Vector2)context.caster.transform.position;
        IsDone = false;
    }

    public override void OnUpdate(EffectContext context)
    {
        // 获取状态数据
        var state = context.GetOrCreateEffectState(this);
        float elapsed = (float)state["elapsed"];
        Vector2 startPos = (Vector2)state["startPos"];

        elapsed += Time.deltaTime;
        state["elapsed"] = elapsed;

        float totalDuration = context.duration > 0 ? context.duration : 0.5f;
        float t = Mathf.Clamp01(elapsed / totalDuration);
        float curveValue = speedCurve.Evaluate(t);
        float currentSpeed = speed * curveValue;

        // 计算目标位置
        Vector2 newPosition = startPos + context.direction * (currentSpeed * elapsed);

        // 使用 Rigidbody2D 移动（如果存在）
        Rigidbody2D rb = context.caster.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 计算本帧位移量，使用 MovePosition 让物理系统处理碰撞
            Vector2 currentPos = rb.position;
            Vector2 targetPos = newPosition;
            rb.MovePosition(targetPos);
        }
        else
        {
            // 没有 Rigidbody2D 时退回 Transform 移动（会穿墙）
            context.caster.transform.position = newPosition;
        }

        // 检查完成条件
        if (elapsed >= totalDuration)
        {
            IsDone = true;
        }
        else if (distance > 0 && Vector2.Distance(startPos, newPosition) >= distance)
        {
            IsDone = true;
        }
    }

    public override void OnEnd(EffectContext context)
    {
        // 可以在这里做一些结束时的处理
        IsDone = true;
    }
}