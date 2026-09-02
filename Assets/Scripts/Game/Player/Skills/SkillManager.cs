using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能管理器：挂载在角色身上，负责装载、触发、更新技能
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("技能配置")]
    [Tooltip("将你创建好的技能数据资源拖入此列表，运行时将自动生成对应的技能实例")]
    [SerializeField] private List<SkillData> skillDataList = new List<SkillData>();

    [Header("输入设置（临时）")]
    [SerializeField] private KeyCode rollKey = KeyCode.Space;  // 翻滚快捷键，后续可换成 Input System
    [SerializeField] private KeyCode skill2Key = KeyCode.Q;    // 第二个技能快捷键，示例用

    // 运行时技能实例列表
    private List<SkillBase> skills = new List<SkillBase>();

    // 通过技能名称快速查找
    private Dictionary<string, SkillBase> skillDict = new Dictionary<string, SkillBase>();

    // 角色状态引用
    private PlayerState playerState;

    // 记录角色面向方向（由移动输入或最后方向决定）
    private Vector2 facingDirection = Vector2.right;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();

        // 根据数据列表创建技能实例
        InitializeSkills();
    }

    private void Update()
    {
        // 1. 更新所有技能的冷却
        foreach (var skill in skills)
        {
            if (!skill.IsCooldownReady)
            {
                skill.RemainingCooldown -= Time.deltaTime;
            }
        }

        // 2. 更新所有正在运行的技能
        foreach (var skill in skills)
        {
            if (skill.IsRunning)
            {
                skill.OnSkillUpdate();
            }
        }

        // 3. 更新角色朝向（临时：根据水平输入判断）
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
            skill.Init(data, this);   // this 是 MonoBehaviour，用于启动协程

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
    /// 尝试使用指定技能实例（内部方法）
    /// </summary>
    private bool TryUseSkill(SkillBase skill)
    {
        // 检查冷却
        if (!skill.IsCooldownReady)
        {
            Debug.Log($"技能 {skill.Data.skillName} 冷却中，剩余 {skill.RemainingCooldown:F1} 秒");
            return false;
        }

        // 检查是否已在运行
        if (skill.IsRunning)
        {
            Debug.Log($"技能 {skill.Data.skillName} 已在运行中");
            return false;
        }

        // 检查角色状态
        if (playerState != null && !playerState.CanUseSkill(skill.Data.skillType))
        {
            Debug.Log($"当前状态不允许使用技能 {skill.Data.skillName}");
            return false;
        }

        // 如果是 GenericSkill，设置方向
        if (skill is GenericSkill genericSkill)
        {
            genericSkill.SetDirection(facingDirection);
        }

        // 开始技能
        skill.OnSkillStart();

        // 设置冷却
        skill.RemainingCooldown = skill.Data.cooldown;

        // 如果技能是瞬时的（持续时间为 0），下一帧就会自行结束
        // 对于持续技能，OnSkillUpdate 会驱动它直到完成

        Debug.Log($"使用技能：{skill.Data.skillName}");
        return true;
    }

    /// <summary>
    /// 更新角色朝向（临时实现：根据水平轴输入判断）
    /// </summary>
    private void UpdateFacingDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 如果有任意输入，直接组合成方向向量，不做归一化到四方向
        Vector2 inputDirection = new Vector2(horizontal, vertical);

        if (inputDirection.magnitude > 0.01f)
        {
            // 归一化，确保斜向移动速度不会比正向快
            facingDirection = inputDirection.normalized;
        }
    }

    /// <summary>
    /// 获取技能冷却进度（0~1），供 UI 使用
    /// </summary>
    public float GetCooldownPercent(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            if (skill.Data.cooldown <= 0f) return 1f;
            return Mathf.Clamp01(1f - (skill.RemainingCooldown / skill.Data.cooldown));
        }
        return 1f;
    }

    /// <summary>
    /// 获取技能剩余冷却时间（秒）
    /// </summary>
    public float GetRemainingCooldown(string skillName)
    {
        if (skillDict.TryGetValue(skillName, out var skill))
        {
            return Mathf.Max(0f, skill.RemainingCooldown);
        }
        return 0f;
    }

    /// <summary>
    /// 手动设置技能方向（供外部调用，比如来自输入系统的方向）
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.magnitude > 0.01f)
        {
            facingDirection = direction.normalized;
        }
    }

    /// <summary>
    /// 获取当前技能列表（供 UI 或调试使用）
    /// </summary>
    public List<SkillBase> GetAllSkills()
    {
        return skills;
    }
}