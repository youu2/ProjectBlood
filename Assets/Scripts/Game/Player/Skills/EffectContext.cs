using System.Collections.Generic;
using ProjectBlood;
using UnityEngine;

// 注意：必须是 class（引用类型）。
// 执行器会把同一个上下文传给多个 SkillEffect，目标引用/落点/处决标记等字段
// 需要跨效果共享；若为 struct，按值传递时各效果拿到的是副本，写入会丢失。
public class EffectContext
{
    public GameObject caster;          // 施法者
    public Vector2 direction;          // 释放方向
    public float duration;             // 技能总持续时间，效果可参考

    // 时间口径：true 时持续型效果使用 Time.unscaledDeltaTime（慢动作技能用）。
    // 由 SkillExecutor 从 SkillData.useUnscaledTime 填充。
    public bool useUnscaledTime;

    // ===== 袖剑（及未来"选定目标"类技能）共享数据 =====
    public IDamageable lockedTarget;       // 本次施法锁定的目标
    public bool isExecutionTarget;         // 出手时锁定目标是否满足处决条件（濒死且可处决）
    public Vector2 dashDirection;          // 玩家→目标的归一化方向
    public Vector3 plannedLanding;         // 越过目标后的预定落点
    public bool targetDiedMidDash;         // 冲刺途中目标是否已死亡/失效
    public bool targetKilledByBlade;       // 本次袖剑伤害是否实际导致目标死亡（死亡管线已执行）
    public bool earlyFinish;               // 技能是否需要提前结束（如目标中途死亡，到点即收）

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
