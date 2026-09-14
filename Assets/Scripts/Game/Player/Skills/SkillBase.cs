using System.Collections;
using UnityEngine;

/// <summary>
/// 所有技能的抽象基类,定义技能的生命周期和通用属性。
/// 技能实例由 SkillManager 持有和驱动,技能本身不继承 MonoBehaviour。
/// </summary>
public abstract class SkillBase
{
    // ========== 运行时状态 ==========
    public bool IsRunning { get; protected set; }      // 技能是否正在运行

    // ---- 充能机制运行时状态 ----
    public int CurrentCharges { get; protected set; }  // 当前充能层数
    protected float chargeTimer;                        // 距下一次充能的倒计时(秒)

    // 最大层数(从 data 读取,但缓存运行时以便升级时重新夹取)
    public int MaxCharges => data != null ? Mathf.Max(1, data.maxCharges) : 1;

    // 可使用判定：有至少 1 层充能即可释放
    public bool IsCooldownReady => CurrentCharges > 0;

    // 下一次充能的进度(0~1),供 UI 显示冷却遮罩用
    public float NextChargeProgress
    {
        get
        {
            if (data == null || data.chargeInterval <= 0f) return 1f;
            if (CurrentCharges >= MaxCharges) return 1f; // 已满充能,进度显示为满
            return Mathf.Clamp01(1f - chargeTimer / data.chargeInterval);
        }
    }

    // 距离下一次充能的剩余秒数(UI 数字显示用)
    public float RemainingTimeToNextCharge
    {
        get
        {
            if (CurrentCharges >= MaxCharges) return 0f;
            return Mathf.Max(0f, chargeTimer);
        }
    }

    // ========== 引用 ==========
    protected SkillData data;                          // 技能数据(静态参数)
    protected MonoBehaviour owner;                     // 技能拥有者(用于启动协程)
    protected Transform ownerTransform;                // 拥有者的 Transform
    protected Rigidbody2D ownerRb;                     // 拥有者的 2D 刚体(若有)

    // 技能数据公开访问
    public SkillData Data => data;

    /// <summary>
    /// 初始化技能,传递数据和拥有者。由 SkillManager 在创建技能实例后调用。
    /// </summary>
    public virtual void Init(SkillData skillData, MonoBehaviour skillOwner)
    {
        data = skillData;
        owner = skillOwner;
        ownerTransform = owner.transform;
        ownerRb = owner.GetComponent<Rigidbody2D>();

        // 初始化充能：按 initialCharges 给予初始层数
        ResetCharges();
    }

    /// <summary>
    /// 重置充能状态(初始层数、计时器归零)。
    /// 适用于：技能初始化、进入下一关、角色死亡/复活、技能参数变更。
    /// </summary>
    public void ResetCharges()
    {
        int init = 0;
        if (data != null)
        {
            // 上界至少为 1,避免 maxCharges 为 0 或负数时 Clamp 出现异常
            int upperBound = Mathf.Max(1, data.maxCharges);
            init = Mathf.Clamp(data.initialCharges, 0, upperBound);
        }
        CurrentCharges = init;
        chargeTimer = 0f;
    }

    /// <summary>
    /// 消耗 1 层充能。由 SkillManager 在成功释放技能时调用。
    /// 消耗后不会重置充能计时器(充能过程持续进行、不受使用影响)。
    /// </summary>
    /// <returns>是否消耗成功(充能层数 > 0 时成功)</returns>
    public bool ConsumeCharge()
    {
        if (CurrentCharges <= 0) return false;
        CurrentCharges--;
        // 若计时器还没启动(此前是满充能),则启动一次充能计时
        if (chargeTimer <= 0f && CurrentCharges < MaxCharges)
        {
            chargeTimer = data != null ? data.chargeInterval : 1f;
        }
        return true;
    }

    /// <summary>
    /// 每帧充能累计。由 SkillManager 统一驱动。
    /// 每隔 chargeInterval 秒为未满层的技能增加 1 层。
    /// </summary>
    public void TickCharge(float deltaTime)
    {
        if (data == null) return;

        // 技能升级/参数变更可能导致 maxCharges 减小,夹取当前层数到新上限
        if (CurrentCharges > MaxCharges)
        {
            CurrentCharges = MaxCharges;
        }

        if (CurrentCharges >= MaxCharges)
        {
            chargeTimer = 0f; // 已满,计时归零(下次消耗后重新开始)
            return;
        }

        chargeTimer -= deltaTime;
        // 支持一次 deltaTime 跨多段充能间隔(例如卡顿后补回)
        while (chargeTimer <= 0f && CurrentCharges < MaxCharges)
        {
            CurrentCharges++;
            if (data.chargeInterval > 0f)
                chargeTimer += data.chargeInterval;
            else
                chargeTimer = 0f;
        }
        if (CurrentCharges >= MaxCharges) chargeTimer = 0f;
    }

    /// <summary>
    /// 技能开始逻辑(由 SkillManager 在条件满足时调用)
    /// </summary>
    public virtual void OnSkillStart()
    {
        IsRunning = true;

        // 播放开始音效(如果有)
        if (data != null && data.startSFX != null)
        {
            AudioSource.PlayClipAtPoint(data.startSFX, ownerTransform.position);
        }
    }

    /// <summary>
    /// 每帧更新逻辑,仅在 IsRunning 时由 SkillManager 调用
    /// </summary>
    public virtual void OnSkillUpdate() { }

    /// <summary>
    /// 技能结束逻辑(持续时间结束或被中断)
    /// </summary>
    public virtual void OnSkillEnd()
    {
        IsRunning = false;

        // 播放结束音效(如果有)
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
    /// 在 owner 上启动协程(因为技能不是 MonoBehaviour,需要借助 owner 来运行协程)
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