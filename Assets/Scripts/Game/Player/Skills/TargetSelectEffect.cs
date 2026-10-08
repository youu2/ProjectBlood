using ProjectBlood;
using UnityEngine;

/// <summary>
/// 袖剑目标搜索效果（可复用于未来的"选定目标"类技能）。
/// 职责：
/// 1. CheckCanCast：范围内无敌人时拦截释放（不耗 CD/不进演出），并给最小反馈；
/// 2. OnStart：确定目标（濒死优先、取最近，含墙后敌人），把目标引用/冲刺方向/
///    预定落点写入 EffectContext。
/// 搜索范围限定当前房间 Global.currentRoom.GetEnemies()。
/// </summary>
[CreateAssetMenu(fileName = "TargetSelectEffect", menuName = "技能系统/效果/袖剑目标搜索效果")]
public class TargetSelectEffect : SkillEffect
{
    [Header("目标搜索")]
    [Tooltip("目标搜索半径（世界单位），360° 圆形范围，包含墙后敌人")]
    [SerializeField] private float range = 3f;

    [Tooltip("处决血量阈值百分比：当前血量/最大血量 ≤ 此值且 canBeExecuted 才判定为濒死")]
    [Range(0.05f, 1f)]
    [SerializeField] private float executionThresholdPercent = 0.3f;

    [Tooltip("越过目标后的落点偏移距离（世界单位）")]
    [SerializeField] private float landingOffset = 1f;

    [Header("无目标反馈")]
    [Tooltip("\"无目标\"提示的最小间隔（真实时间秒），防止连按 F 刷屏")]
    [SerializeField] private float noTargetPromptInterval = 0.5f;

    [Tooltip("无目标时播放的音效名（复用空仓点击音）")]
    [SerializeField] private string noTargetSfxName = "DryFireClick";

    [SerializeField] private float noTargetSfxVolume = 0.5f;

    // 节流时间戳跨实例共享（同一 SO 资产，静态即可）
    private static float lastNoTargetRealtime = -999f;

    private void OnEnable() => RegisterSettings();

    // Inspector 改值后立刻同步给全局设置（指示器判定/红色标记都读它）
    private void OnValidate() => RegisterSettings();

    private void RegisterSettings()
    {
        HiddenBladeSettings.Register(
            Mathf.Max(0.1f, range),
            Mathf.Clamp01(executionThresholdPercent),
            Mathf.Max(0f, landingOffset));
    }

    /// <summary>释放前置：范围内必须至少有一个存活敌人。</summary>
    public override bool CheckCanCast(GameObject caster)
    {
        RegisterSettings();

        if (caster == null) return false;
        if (!TryFindTarget(caster.transform.position, HiddenBladeSettings.Range, out _, out _))
        {
            NotifyNoTarget();
            return false;
        }
        return true;
    }

    public override void OnStart(EffectContext context)
    {
        RegisterSettings();

        Vector3 origin = context.caster != null ? context.caster.transform.position : Vector3.zero;
        if (!TryFindTarget(origin, HiddenBladeSettings.Range, out IDamageable target, out bool isExecutionTarget))
        {
            // 正常不会走到（CheckCanCast 已拦截），兜底防止空目标演出
            Debug.LogWarning("[袖剑] OnStart 时未找到目标，中止本次施法");
            context.earlyFinish = true;
            return;
        }

        Vector3 targetPosition = target.GameObject.transform.position;
        Vector2 dashDirection = ((Vector2)(targetPosition - origin)).normalized;
        // 零距离保护：目标与玩家重合时沿面朝方向（默认右）
        if (dashDirection.sqrMagnitude < 0.0001f) dashDirection = Vector2.right;

        context.lockedTarget = target;
        context.isExecutionTarget = isExecutionTarget;
        context.dashDirection = dashDirection;
        // 此时先写原始预定落点；墙体修正由处决效果完成后回写
        context.plannedLanding = targetPosition + (Vector3)dashDirection * HiddenBladeSettings.LandingOffset;
    }

    /// <summary>
    /// 在当前房间、指定半径内选择目标：
    /// 优先"濒死且可处决"中最近者；否则取任意存活敌人中最近者（含 Boss）。
    /// 不做墙体遮挡剔除（墙后敌人可选）。
    /// </summary>
    public static bool TryFindTarget(Vector3 origin, float searchRange,
        out IDamageable target, out bool isExecutionTarget)
    {
        target = null;
        isExecutionTarget = false;

        var enemies = Global.currentRoom != null ? Global.currentRoom.GetEnemies() : null;
        if (enemies == null) return false;

        float rangeSqr = searchRange * searchRange;
        IDamageable nearestDying = null;
        float nearestDyingDistSqr = float.MaxValue;
        IDamageable nearestAny = null;
        float nearestAnyDistSqr = float.MaxValue;

        foreach (IDamageable damageable in enemies)
        {
            // 目前敌人唯一实现是 EnemyBase；先转类型并用 Unity 伪 null 判空，
            // 再访问 transform（死亡敌人理论上已被 Death 移出集合，此为时序双保险）
            if (!(damageable is EnemyBase enemy) || enemy == null || enemy.IsDead) continue;

            float distSqr = ((Vector2)(enemy.transform.position - origin)).sqrMagnitude;
            if (distSqr > rangeSqr) continue;

            if (distSqr < nearestAnyDistSqr)
            {
                nearestAnyDistSqr = distSqr;
                nearestAny = damageable;
            }

            if (HiddenBladeSettings.IsInExecutionRange(enemy.CurrentHealth, enemy.MaxHealth, enemy.canBeExecuted)
                && distSqr < nearestDyingDistSqr)
            {
                nearestDyingDistSqr = distSqr;
                nearestDying = damageable;
            }
        }

        if (nearestDying != null)
        {
            target = nearestDying;
            isExecutionTarget = true;
        }
        else
        {
            target = nearestAny;
        }
        return target != null;
    }

    private void NotifyNoTarget()
    {
        // 真实时间节流（可能在冷却中等缩放时间口径下被调用，统一用 unscaled）
        float now = Time.unscaledTime;
        if (now - lastNoTargetRealtime < noTargetPromptInterval) return;
        lastNoTargetRealtime = now;

        Player.DisplayText("无目标");
        AudioKitManager.Instance?.PlayOneShot(noTargetSfxName, volume: noTargetSfxVolume);
    }
}
