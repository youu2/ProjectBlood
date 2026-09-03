using UnityEngine;

/// <summary>
/// 玩家状态组件：管理角色的当前状态，供技能系统查询
/// </summary>
public class PlayerState : MonoBehaviour
{
    // 角色状态枚举
    public enum State
    {
        Normal,     // 正常
        Rolling,    // 翻滚中
        Attacking,  // 攻击中
        Stunned,    // 眩晕
        Dead        // 死亡
    }

    [Header("当前状态")]
    [SerializeField] private State currentState = State.Normal;

    /// <summary>
    /// 当前状态（只读属性）
    /// </summary>
    public State CurrentState => currentState;

    /// <summary>
    /// 角色是否可以移动
    /// </summary>
    public bool CanMove => currentState == State.Normal || currentState == State.Attacking;

    /// <summary>
    /// 角色是否可以使用指定类型的技能
    /// </summary>
    public bool CanUseSkill(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Roll:
                // 翻滚需要处于正常或攻击状态
                return currentState == State.Normal || currentState == State.Attacking;
            default:
                // 其他技能默认只能在正常状态使用
                return currentState == State.Normal;
        }
    }

    /// <summary>
    /// 设置角色状态
    /// </summary>
    public void SetState(State newState)
    {
        if (currentState == newState) return;

        State oldState = currentState;
        currentState = newState;
        Debug.Log($"玩家状态切换：{oldState} → {newState}");

        // 可在这里加入状态变化的额外逻辑（例如取消攻击等）
    }

    /// <summary>
    /// 恢复到正常状态
    /// </summary>
    public void ResetToNormal()
    {
        SetState(State.Normal);
    }

    public State GetState()
    {
        return currentState;
    }
}