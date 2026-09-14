using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能管理器：挂载在角色身上,负责装载、触发、更新技能
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("技能配置")]
    [Tooltip("将你创建好的技能数据资源拖入此列表,运行时将自动生成对应的技能实例")]
    [SerializeField] private List<SkillData> skillDataList = new List<SkillData>();

    [Header("输入设置(临时)")]
    [SerializeField] private KeyCode rollKey = KeyCode.Space;  // 翻滚快捷键,后续可换成 Input System
    [SerializeField] private KeyCode skill2Key = KeyCode.Q;    // 第二个技能快捷键,示例用

    // 运行时技能实例列表
    private List<SkillBase> skills = new List<SkillBase>();

    // 通过技能名称快速查找
    private Dictionary<string, SkillBase> skillDict = new Dictionary<string, SkillBase>();

    // 角色状态引用
    private PlayerState playerState;

    // 记录角色面向方向(由移动输入或最后方向决定)
    private Vector2 facingDirection = Vector2.right;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();

        // 根据数据列表创建技能实例
        InitializeSkills();
    }

    private void Update()
    {
        // 1. 充能累计：每帧为所有未满层的技能推进充能计时(持续进行,不受使用影响)
        foreach (var skill in skills)
        {
            skill.TickCharge(Time.deltaTime);
        }

        // 2. 更新所有正在运行的技能
        foreach (var skill in skills)
        {
            if (skill.IsRunning)
            {
                skill.OnSkillUpdate();
            }
        }

        // 3. 更新角色朝向(临时：根据水平输入判断)
        UpdateFacingDirection();

        // 4. 处理输入
        if (Input.GetKeyDown(rollKey))
        {
            TryUseSkillByName("翻滚");
        }
        if (Input.GetKeyDown(skill2Key))
        {
            TryUseSkillByName("技能2");
        }
    }

    /// <summary>
    /// 根据技能数据列表创建技能实例
    /// </summary>
    private void InitializeSkills()
    {
        skills.Clear();
        skillDict.Clear();

        foreach (var data in skillDataList)
        {
            if (data == null) continue;

            // 统一使用 GenericSkill 作为所有数据驱动技能的运行时类
            GenericSkill skill = new GenericSkill();
            skill.Init(data, this);   // this 是 MonoBehaviour,用于启动协程

            skills.Add(skill);
            skillDict[data.skillName] = skill;

            Debug.Log($"技能已加载：{data.skillName}");
        }
    }

    /// <summary>
    /// 根据技能名称尝试使用技能
    /// </summary>
    public bool TryUseSkillByName(string skillName)
    {
        if (!skillDict.ContainsKey(skillName))
        {
            Debug.LogWarning($"未找到技能：{skillName}");
            return false;
        }

        return TryUseSkill(skillDict[skillName]);
    }

    /// <summary>
    /// 尝试使用指定技能实例(内部方法)
    /// </summary>
    private bool TryUseSkill(SkillBase skill)
    {
        // 检查充能：至少 1 层充能才能释放
        if (!skill.IsCooldownReady)
        {
            return false;
        }

        // 检查是否已在运行
        if (skill.IsRunning)
        {
            return false;
        }

        // 检查角色状态
        if (playerState != null && !playerState.CanUseSkill(skill.Data.skillType))
        {
            return false;
        }

        // 如果是 GenericSkill,设置方向
        if (skill is GenericSkill genericSkill)
        {
            genericSkill.SetDirection(facingDirection);
        }

        // 开始技能
        skill.OnSkillStart();

        // 消耗 1 层充能(充能过程持续进行,不重置计时器)
        skill.ConsumeCharge();

        return true;
    }

    /// <summary>
    /// 更新角色朝向(临时实现：根据水平轴输入判断)
    /// </summary>
    private void UpdateFacingDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 如果有任意输入,直接组合成方向向量,不做归一化到四方向
        Vector2 inputDirection = new Vector2(horizontal, vertical);

        if (inputDirection.magnitude > 0.01f)
        {
            // 归一化,确保斜向移动速度不会比正向快
            facingDirection = inputDirection.normalized;
        }
    }

    /// <summary>
    /// 获取下一次充能进度(0~1),供 UI 冷却遮罩使用。
    /// 满充能时返回 1,充能中时返回计时器进度。
    /// </summary>
    public float GetCooldownPercent(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            return skill.NextChargeProgress;
        }
        return 1f;
    }

    /// <summary>
    /// 获取距离下一次充能的剩余秒数(UI 数字显示用)
    /// </summary>
    public float GetRemainingCooldown(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            return skill.RemainingTimeToNextCharge;
        }
        return 0f;
    }

    /// <summary>
    /// 获取当前充能层数
    /// </summary>
    public int GetCurrentCharges(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            return skill.CurrentCharges;
        }
        return 0;
    }

    /// <summary>
    /// 获取最大充能层数
    /// </summary>
    public int GetMaxCharges(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            return skill.MaxCharges;
        }
        return 1;
    }

    /// <summary>
    /// 重置所有技能的充能状态(用于进入下一关、角色死亡/复活、技能参数变更等场景)。
    /// 充能层数恢复为 SkillData.initialCharges,计时器归零。
    /// </summary>
    public void ResetAllCharges()
    {
        foreach (var skill in skills)
        {
            skill.ResetCharges();
        }
    }

    /// <summary>
    /// 立即为指定技能补充一层充能(上限受 maxCharges 限制)。
    /// 可用于技能升级、奖励道具等场景。
    /// </summary>
    public void AddCharge(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            if (skill.CurrentCharges < skill.MaxCharges)
            {
                // 直接调用 TickCharge 消耗 chargeInterval 秒(若计时未启动)或直接加层
                // 简化处理：通过设置一个极小的计时器让下一帧 tick 时立即获得充能
                // 这里采用直接操作：若已满则忽略
                // 为避免破坏封装,用反射不安全;改为走已有的 public 路径：
                // 由于 ConsumeCharge 只减不加,这里提供一个内部路径：
                skill.TickCharge(skill.Data != null ? skill.Data.chargeInterval : 0f);
            }
        }
    }

    /// <summary>
    /// 手动设置技能方向(供外部调用,比如来自输入系统的方向)
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.magnitude > 0.01f)
        {
            facingDirection = direction.normalized;
        }
    }

    /// <summary>
    /// 获取当前技能列表(供 UI 或调试使用)
    /// </summary>
    public List<SkillBase> GetAllSkills()
    {
        return skills;
    }

    public Vector2 GetFacingDirection()
    {
        return facingDirection;
    }
}