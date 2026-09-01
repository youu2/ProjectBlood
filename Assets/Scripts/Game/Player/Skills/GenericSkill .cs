using UnityEngine;

/// <summary>
/// 通用技能：使用 SkillExecutor 执行技能数据中配置的效果列表
/// </summary>
public class GenericSkill : SkillBase
{
    // 技能执行器
    private SkillExecutor executor;

    // 技能施放方向（由角色朝向或输入决定，后续可调整）
    private Vector2 skillDirection;

    public override void Init(SkillData skillData, MonoBehaviour skillOwner)
    {
        base.Init(skillData, skillOwner);
        // 默认方向朝右，实际使用时由管理器设置
        skillDirection = Vector2.right;
    }

    /// <summary>
    /// 设置技能释放方向（由外部在调用技能前设置）
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        skillDirection = direction;
    }

    public override void OnSkillStart()
    {
        base.OnSkillStart(); // 会播放音效等

        // 创建执行器并启动
        executor = new SkillExecutor(data, owner.gameObject, skillDirection, data.duration);
        executor.Start();
    }

    public override void OnSkillUpdate()
    {
        if (executor != null)
        {
            executor.Update();

            // 如果执行器报告完成，则主动结束技能
            if (executor.IsFinished)
            {
                // 调用结束逻辑，但注意避免递归
                // 我们可以在下一帧由 SkillManager 调用 OnSkillEnd，
                // 但这里直接调用也可以，但要注意 base.OnSkillEnd 会设置 IsRunning = false
                OnSkillEnd();
            }
        }
    }

    public override void OnSkillEnd()
    {
        // 确保执行器结束所有效果
        if (executor != null)
        {
            executor.End();
            executor = null;
        }

        base.OnSkillEnd(); // 播放结束音效，设置 IsRunning = false
    }

    public override void Interrupt()
    {
        // 调用 OnSkillEnd 强制结束
        OnSkillEnd();
    }
}