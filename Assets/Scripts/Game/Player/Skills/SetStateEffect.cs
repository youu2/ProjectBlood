using UnityEngine;

[CreateAssetMenu(fileName = "SetStateEffect", menuName = "技能系统/效果/状态切换效果")]
public class SetStateEffect : SkillEffect
{
    [Header("状态切换")]
    [Tooltip("技能开始时切换到的状态")]
    public PlayerState.State stateToSet = PlayerState.State.Rolling;

    [Tooltip("技能结束时是否恢复到之前的状态")]
    public bool restoreOnEnd = true;

    // 保存之前的状态
    private PlayerState.State previousState;

    public override void OnStart(EffectContext context)
    {
        PlayerState playerState = context.caster.GetComponent<PlayerState>();
        if (playerState == null)
        {
            Debug.LogWarning("施法者没有 PlayerState 组件，无法切换状态");
            return;
        }

        previousState = playerState.CurrentState;
        playerState.SetState(stateToSet);
        Debug.Log($"状态切换：{previousState} → {stateToSet}");
    }

    public override void OnEnd(EffectContext context)
    {
        if (!restoreOnEnd) return;

        PlayerState playerState = context.caster.GetComponent<PlayerState>();
        if (playerState == null) return;

        playerState.SetState(previousState);
        Debug.Log($"状态恢复：{previousState}");
    }
}