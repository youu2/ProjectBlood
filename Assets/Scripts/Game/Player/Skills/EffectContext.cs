using System.Collections.Generic;
using UnityEngine;

public struct EffectContext
{
    public GameObject caster;          // 施法者
    public Vector2 direction;          // 释放方向
    public float duration;             // 技能总持续时间，效果可参考

    // 私有状态字典，用于存储每个效果实例的临时数据
    // 键是效果的 GetInstanceID() + 效果类型名，确保唯一
    private Dictionary<string, Dictionary<string, object>> effectStates;

    /// <summary>
    /// 获取或创建指定效果实例的状态字典
    /// </summary>
    public Dictionary<string, object> GetOrCreateEffectState(SkillEffect effect)
    {
        if (effectStates == null)
            effectStates = new Dictionary<string, Dictionary<string, object>>();

        // 使用效果的实例ID和类型名组合作为键，避免不同实例冲突
        string key = effect.GetInstanceID() + effect.name;
        if (!effectStates.ContainsKey(key))
        {
            effectStates[key] = new Dictionary<string, object>();
        }
        return effectStates[key];
    }

    // 构造函数，便于创建上下文时初始化
    public EffectContext(GameObject caster, Vector2 direction, float duration)
    {
        this.caster = caster;
        this.direction = direction;
        this.duration = duration;
        effectStates = new Dictionary<string, Dictionary<string, object>>();
    }
}
