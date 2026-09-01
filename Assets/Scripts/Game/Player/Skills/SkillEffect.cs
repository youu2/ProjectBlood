using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    /// <summary>
    /// 效果开始时调用一次，可进行初始化
    /// </summary>
    public virtual void OnStart(EffectContext context) { }

    /// <summary>
    /// 每帧由技能管理器调用，用于更新效果逻辑
    /// </summary>
    public virtual void OnUpdate(EffectContext context) { }

    /// <summary>
    /// 效果结束时调用一次，用于清理
    /// </summary>
    public virtual void OnEnd(EffectContext context) { }

    /// <summary>
    /// 效果是否已经完成（只有完成才会执行下一个效果，顺序模式下使用）
    /// 默认返回 true，表示瞬间完成；持续效果需重写此方法并返回 false 直到完成
    /// </summary>
    public virtual bool IsDone { get { return true; } protected set { } }
}