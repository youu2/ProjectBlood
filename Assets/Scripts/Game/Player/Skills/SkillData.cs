using System.Collections.Generic;
using UnityEngine;

// 效果执行策略
public enum EffectExecutionMode
{
    Sequential,  // 顺序执行：一个效果执行完再执行下一个
    Parallel     // 并行执行：所有效果同时开始
}

// 技能类型枚举
public enum SkillType
{
    Roll,       // 翻滚
    Charge,     // 冲锋
    AOE,        // 范围攻击
    Buff,       // 增益
    Other       // 其他
}

[CreateAssetMenu(fileName = "NewSkillData", menuName = "技能系统/技能数据")]
public class SkillData : ScriptableObject
{
    [Header("基本信息")]
    public string skillName = "新技能";          // 技能名称
    public Sprite icon;                          // 技能图标（用于UI）
    [TextArea] public string description;       // 描述文本
    [Tooltip("技能类型，用于分类和显示")]
    public SkillType skillType = SkillType.Other;  // 技能类型，用于分类和显示

    [Header("冷却与持续时间")]
    public float cooldown = 1f;                  // 冷却时间（秒）
    public float duration = 0.5f;               // 技能总持续时间（0为瞬间），效果可在此时间内运行

    [Header("效果组合")]
    [Tooltip("将你想要的效果资源拖入此列表，它们将按照列表顺序或并行执行。")]
    public List<SkillEffect> effects = new List<SkillEffect>();  // 效果列表

    [Tooltip("顺序执行：一个效果结束后才开始下一个；并行执行：所有效果同时启动")]
    public EffectExecutionMode executionMode = EffectExecutionMode.Sequential;

    [Tooltip("开始音效（可选）")]
    public AudioClip startSFX;                  // 开始音效（可选）
    [Tooltip("结束音效（可选）技能持续时间结束后或被中断时播放")]
    public AudioClip endSFX;                    // 结束音效（可选）
}