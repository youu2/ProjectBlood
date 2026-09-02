using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能执行器：负责根据 SkillData 中的效果列表驱动每个效果的生命周期
/// </summary>
public class SkillExecutor
{
    // 技能数据（包含效果列表和执行模式）
    private SkillData skillData;
    // 施法者
    private GameObject caster;
    // 释放方向
    private Vector2 direction;
    // 技能总持续时间
    private float duration;

    // 执行上下文（效果间共享）
    private EffectContext context;

    // 顺序模式下当前正在执行的效果索引
    private int currentIndex = 0;
    // 标记是否已经完成（顺序模式所有效果完成，或并行模式被外部结束）
    private bool isFinished = false;

    // 所有正在运行的效果（用于统一更新和结束）
    private List<SkillEffect> activeEffects = new List<SkillEffect>();

    /// <summary>
    /// 构造函数
    /// </summary>
    public SkillExecutor(SkillData data, GameObject caster, Vector2 direction, float duration)
    {
        this.skillData = data;
        this.caster = caster;
        this.direction = direction;
        this.duration = duration;

        // 创建执行上下文，效果们会通过它获取状态和共享信息
        context = new EffectContext(caster, direction, duration);
    }

    /// <summary>
    /// 开始执行技能效果
    /// </summary>
    public void Start()
    {
        if (skillData == null || skillData.effects.Count == 0)
        {
            isFinished = true;
            return;
        }

        // 根据执行模式初始化
        if (skillData.executionMode == EffectExecutionMode.Parallel)
        {
            // 并行：所有效果同时开始
            foreach (var effect in skillData.effects)
            {
                effect.OnStart(context);
                activeEffects.Add(effect);
            }
        }
        else // Sequential
        {
            // 顺序：只启动第一个效果
            currentIndex = 0;
            StartEffectAt(currentIndex);
        }
    }

    /// <summary>
    /// 每帧由外部调用，更新所有活跃效果，并处理顺序切换
    /// </summary>
    public void Update()
    {
        if (isFinished) return;

        if (skillData.executionMode == EffectExecutionMode.Parallel)
        {
            // 并行模式下更新所有活跃效果
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.OnUpdate(context);
                // 如果效果自己声明完成，则调用 OnEnd 并移除
                if (effect.IsDone)
                {
                    effect.OnEnd(context);
                    activeEffects.RemoveAt(i);
                }
            }

            // 如果所有效果都完成了（理论上并行效果可能同时完成，或者有些瞬间完成），则标记结束
            if (activeEffects.Count == 0)
            {
                isFinished = true;
            }
        }
        else // Sequential
        {
            // 顺序模式：更新当前效果
            if (currentIndex >= skillData.effects.Count)
            {
                isFinished = true;
                return;
            }

            var currentEffect = skillData.effects[currentIndex];
            // 如果当前效果还没有启动（例如刚切换过来），先启动它
            if (!activeEffects.Contains(currentEffect))
            {
                StartEffectAt(currentIndex);
                return; // 下一帧再更新，确保 OnStart 先执行
            }

            // 更新当前效果
            currentEffect.OnUpdate(context);

            // 检查当前效果是否完成
            if (currentEffect.IsDone)
            {
                // 结束当前效果
                currentEffect.OnEnd(context);
                activeEffects.Remove(currentEffect);

                // 移动到下一个效果
                currentIndex++;
                if (currentIndex >= skillData.effects.Count)
                {
                    isFinished = true;
                }
                else
                {
                    // 启动下一个效果（下一帧会进入更新）
                    StartEffectAt(currentIndex);
                }
            }
        }
    }

    /// <summary>
    /// 强制结束所有正在运行的效果（例如技能被中断或总时间到）
    /// </summary>
    public void End()
    {
        if (isFinished) return;

        foreach (var effect in activeEffects)
        {
            effect.OnEnd(context);
        }
        activeEffects.Clear();
        isFinished = true;
    }

    // 辅助方法：启动指定索引的效果，并加入活跃列表
    private void StartEffectAt(int index)
    {
        var effect = skillData.effects[index];
        effect.OnStart(context);
        activeEffects.Add(effect);
    }

    /// <summary>
    /// 技能是否已经完成
    /// </summary>
    public bool IsFinished => isFinished;
}
