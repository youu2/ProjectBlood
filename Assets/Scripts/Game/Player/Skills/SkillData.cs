using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 效果执行策略
public enum EffectExecutionMode
{
    Sequential,  // 顺序执行：一个效果执行完再执行下一个
    Parallel     // 并行执行：所有效果同时开始
}

// 技能类型枚举
public enum SkillType
{
    Roll,         // 翻滚
    Charge,       // 冲锋
    AOE,          // 范围攻击
    Buff,         // 增益
    HiddenBlade,  // 袖剑（处决突进）
    Other         // 其他
}

[CreateAssetMenu(fileName = "NewSkillData", menuName = "技能系统/技能数据")]
public class SkillData : ScriptableObject
{
    [Header("基本信息")]
    public string skillName = "新技能";          // 技能名称
    public Sprite icon;                          // 技能图标(用于UI)
    [TextArea] public string description;       // 描述文本
    [Tooltip("技能类型,用于分类和显示")]
    public SkillType skillType = SkillType.Other;  // 技能类型,用于分类和显示

    [Header("充能机制")]
    [Tooltip("充能间隔时间(秒)。每隔该时长自动获得 1 层充能,持续进行、不受使用影响。")]
    [FormerlySerializedAs("cooldown")]
    public float chargeInterval = 1f;
    [Tooltip("最大充能层数。=1 时行为完全等同于传统单次冷却;>1 时可连续释放多次。")]
    public int maxCharges = 1;
    [Tooltip("初始拥有的充能层数(通常等于 maxCharges)。进入新关卡/复活时按此初始化。")]
    public int initialCharges = 1;

    [Header("持续时间")]
    [Tooltip("技能总持续时间(0为瞬间),效果可在此时间内运行")]
    public float duration = 0.5f;

    [Tooltip("为 true 时持续型效果按真实时间(unscaledDeltaTime)更新。" +
             "用于袖剑这类会修改 timeScale 的慢动作技能：技能自己走真实时间，世界仍按缩放时间运行。" +
             "普通技能保持 false（翻滚等不受影响）。")]
    public bool useUnscaledTime = false;

    [Header("效果组合")]
    [Tooltip("将你想要的效果资源拖入此列表,它们将按照列表顺序或并行执行。")]
    public List<SkillEffect> effects = new List<SkillEffect>();  // 效果列表

    [Tooltip("顺序执行：一个效果结束后才开始下一个;并行执行：所有效果同时启动")]
    public EffectExecutionMode executionMode = EffectExecutionMode.Sequential;

    [Tooltip("开始音效(可选)")]
    public AudioClip startSFX;                  // 开始音效(可选)
    [Tooltip("结束音效(可选)技能持续时间结束后或被中断时播放")]
    public AudioClip endSFX;                    // 结束音效(可选)
    public float startSfxVolume = 1.0f;
    public float endSfxVolume = 1.0f;
}