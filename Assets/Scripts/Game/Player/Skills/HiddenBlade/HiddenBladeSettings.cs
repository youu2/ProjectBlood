using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 最近一次袖剑施法的调试信息（仅 Gizmos 使用）。
    /// 由处决效果在出手/落点修正时写入，编辑器下画出连线与落点。
    /// </summary>
    public struct HiddenBladeCastDebug
    {
        public bool hasCast;
        public Vector3 origin;
        public Vector3 targetPosition;
        public Vector3 rawLanding;
        public Vector3 correctedLanding;
        public float expireRealtime;
    }

    /// <summary>
    /// 袖剑的全局运行时配置（静态）。
    /// 由 TargetSelectEffect 的 SO 参数在 OnEnable/施法时注册，供以下模块共享：
    ///   - 敌人头顶"危"判定（EnemyBase.TakeDamage）
    ///   - 红色目标标记（ExecutionTargetMarker）
    ///   - 暂停屏蔽（PausePageController 读 IsExecuting）
    /// 设计文档：Assets/Scripts/Game/Player/Skills/HiddenBlade.Design.md
    /// </summary>
    public static class HiddenBladeSettings
    {
        /// <summary>当前是否正处于袖剑处决流程中（慢动作+位移+刺击全程）。</summary>
        public static bool IsExecuting { get; set; }

        /// <summary>目标搜索半径（单位：世界单位）。</summary>
        public static float Range { get; private set; } = 3f;

        /// <summary>处决血量阈值百分比（0~1）：currentHealth/maxHealth 低于此值且 canBeExecuted 才可处决。</summary>
        public static float ExecutionThresholdPercent { get; private set; } = 0.3f;

        /// <summary>越过目标后多移动的距离（落点偏移）。</summary>
        public static float LandingOffset { get; private set; } = 1f;

        /// <summary>由目标搜索效果在资产加载/参数变化时注册当前数值。</summary>
        public static void Register(float range, float executionThresholdPercent, float landingOffset)
        {
            Range = range;
            ExecutionThresholdPercent = executionThresholdPercent;
            LandingOffset = landingOffset;
        }

        /// <summary>目标是否处于可处决的濒死状态。供敌人指示器与目标搜索统一调用。</summary>
        public static bool IsInExecutionRange(float currentHealth, float maxHealth, bool canBeExecuted)
        {
            if (!canBeExecuted || maxHealth <= 0f) return false;
            return currentHealth / maxHealth <= ExecutionThresholdPercent;
        }

        // ===== 最近一次施法的 Gizmo 调试信息 =====
        public static HiddenBladeCastDebug LastCast { get; private set; }

        public static void RecordCast(Vector3 origin, Vector3 targetPosition, Vector3 rawLanding, Vector3 correctedLanding)
        {
            LastCast = new HiddenBladeCastDebug
            {
                hasCast = true,
                origin = origin,
                targetPosition = targetPosition,
                rawLanding = rawLanding,
                correctedLanding = correctedLanding,
                // 真实时间 5 秒后停止绘制，避免旧轨迹长期残留
                expireRealtime = Time.realtimeSinceStartup + 5f
            };
        }

        public static void ClearCast()
        {
            var info = LastCast;
            info.hasCast = false;
            LastCast = info;
        }
    }
}
