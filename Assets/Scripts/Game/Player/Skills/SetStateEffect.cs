using UnityEngine;

[CreateAssetMenu(fileName = "SetStateEffect", menuName = "技能系统/效果/状态切换效果")]
public class SetStateEffect : SkillEffect
{
    [Header("状态切换")]
    [Tooltip("技能开始时切换到的状态")]
    public PlayerState.State stateToSet = PlayerState.State.Rolling;

    [Tooltip("技能结束时是否恢复到之前的状态")]
    public bool restoreOnEnd = true;

    // 持续型效果：技能持续期间保持 false，时长结束后才为 true，
    // 由 SkillExecutor 调用 OnEnd 恢复状态（否则基类默认 true 会导致下一帧立即恢复）
    public override bool IsDone { get; protected set; }

    public override void OnStart(EffectContext context)
    {
        PlayerState playerState = context.caster.GetComponent<PlayerState>();
        if (playerState == null)
        {
            Debug.LogWarning("施法者没有 PlayerState 组件，无法切换状态");
            IsDone = true;   // 效果未生效，直接标记完成，避免执行器一直等待
            return;
        }

        // 每次施法的运行时数据保存在上下文中（随施法独立创建），
        // 不能写入 SO 资产字段，否则同一效果资产被多次/多技能引用时会互相覆盖
        var state = context.GetOrCreateEffectState(this);
        state["previousState"] = playerState.CurrentState;
        state["elapsed"] = 0f;
        state["applied"] = true;

        IsDone = false;
        playerState.SetState(stateToSet);
        Debug.Log($"状态切换：{state["previousState"]} → {stateToSet}");
    }

    public override void OnUpdate(EffectContext context)
    {
        if (IsDone) return;

        var state = context.GetOrCreateEffectState(this);
        float elapsed = (float)state["elapsed"] + Time.deltaTime;
        state["elapsed"] = elapsed;

        // 与其他持续型效果（MoveEffect/InvincibleEffect）一致：技能总时长结束后完成
        float totalDuration = context.duration > 0f ? context.duration : 0.5f;
        if (elapsed >= totalDuration)
        {
            IsDone = true;
        }
    }

    public override void OnEnd(EffectContext context)
    {
        IsDone = true;

        var state = context.GetOrCreateEffectState(this);
        // 幂等保护：仅在 OnStart 成功设置过状态时恢复一次，
        // 防止自然结束与 SkillExecutor.End() 中断路径重复恢复，或未生效时误恢复
        if (!state.ContainsKey("applied") || !(bool)state["applied"]) return;
        state["applied"] = false;

        if (!restoreOnEnd) return;

        PlayerState playerState = context.caster != null
            ? context.caster.GetComponent<PlayerState>()
            : null;
        if (playerState == null) return;

        PlayerState.State previousState = (PlayerState.State)state["previousState"];
        playerState.SetState(previousState);
        Debug.Log($"状态恢复：{stateToSet} → {previousState}");
    }
}
