using System.Collections;
using UnityEngine;

/// <summary>
/// 所有技能的抽象基类，定义技能的生命周期和通用属性。
/// 技能实例由 SkillManager 持有和驱动，技能本身不继承 MonoBehaviour。
/// </summary>
public abstract class SkillBase
{
    // ========== 运行时状态 ==========
    public bool IsRunning { get; protected set; }      // 技能是否正在运行
    public float RemainingCooldown { get; set; }       // 剩余冷却时间（由 SkillManager 更新）
    public bool IsCooldownReady => RemainingCooldown <= 0f;  // 冷却是否就绪

    // ========== 引用 ==========
    protected SkillData data;                          // 技能数据（静态参数）
    protected MonoBehaviour owner;                     // 技能拥有者（用于启动协程）
    protected Transform ownerTransform;                // 拥有者的 Transform
    protected Rigidbody2D ownerRb;                     // 拥有者的 2D 刚体（若有）

    // 技能数据公开访问
    public SkillData Data => data;

    /// <summary>
    /// 初始化技能，传递数据和拥有者。由 SkillManager 在创建技能实例后调用。
    /// </summary>
    public virtual void Init(SkillData skillData, MonoBehaviour skillOwner)
    {
        data = skillData;
        owner = skillOwner;
        ownerTransform = owner.transform;
        ownerRb = owner.GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 技能开始逻辑（由 SkillManager 在条件满足时调用）
    /// </summary>
    public virtual void OnSkillStart()
    {
        IsRunning = true;

        // 播放开始音效（如果有）
        if (data != null && data.startSFX != null)
        {
            AudioSource.PlayClipAtPoint(data.startSFX, ownerTransform.position);
        }
    }

    /// <summary>
    /// 每帧更新逻辑，仅在 IsRunning 时由 SkillManager 调用
    /// </summary>
    public virtual void OnSkillUpdate() { }

    /// <summary>
    /// 技能结束逻辑（持续时间结束或被中断）
    /// </summary>
    public virtual void OnSkillEnd()
    {
        IsRunning = false;

        // 播放结束音效（如果有）
        if (data != null && data.endSFX != null)
        {
            AudioSource.PlayClipAtPoint(data.endSFX, ownerTransform.position);
        }
    }

    /// <summary>
    /// 强制中断技能
    /// </summary>
    public virtual void Interrupt()
    {
        if (IsRunning)
        {
            OnSkillEnd();
        }
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 在 owner 上启动协程（因为技能不是 MonoBehaviour，需要借助 owner 来运行协程）
    /// </summary>
    protected Coroutine StartCoroutine(IEnumerator routine)
    {
        return owner.StartCoroutine(routine);
    }

    /// <summary>
    /// 停止协程
    /// </summary>
    protected void StopCoroutine(Coroutine routine)
    {
        if (routine != null)
        {
            owner.StopCoroutine(routine);
        }
    }
}