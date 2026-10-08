using ProjectBlood;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 袖剑专属处决时间线（持续型效果）：
    /// 真实时间(unscaled)驱动穿墙冲刺 → 命中帧单体伤害 → 击杀奖励/CD 刷新。
    /// 与列表中的 TimeScale/SetState/Invincible 效果并行运行，
    /// 目标冲刺途中死亡时通过 context.earlyFinish 通知执行器立即收尾。
    /// </summary>
    [CreateAssetMenu(fileName = "HiddenBladeExecutionEffect", menuName = "技能系统/效果/袖剑处决效果")]
    public class HiddenBladeExecutionEffect : SkillEffect
    {
        [Header("时间线（真实时间秒）")]
        [Tooltip("高速冲刺到落点的时长")]
        [SerializeField] private float dashDuration = 0.22f;
        [Tooltip("从施法起算的刺击命中时刻（应略晚于冲刺到位时刻）")]
        [SerializeField] private float hitTime = 0.28f;
        [Tooltip("技能总时长（SkillData.duration 为 0 时使用此值）")]
        [SerializeField] private float totalDurationFallback = 0.5f;

        [Header("伤害")]
        [Tooltip("刺击伤害（吃全局增伤+预留技能增伤，不吃武器增伤）")]
        [SerializeField] private float stabDamage = 25f;
        [Tooltip("处决伤害（固定大常量，不吃任何加成）")]
        [SerializeField] private float executionDamage = 9999f;

        [Header("击杀奖励")]
        [Tooltip("袖剑击杀时血库回复量（参考 DirtyBlood 单个 30）")]
        [SerializeField] private int bloodBankReward = 30;
        [Tooltip("袖剑吸血比例：PureBlood 总量 = 敌人 maxHealth × 此值（武器基础吸血 1%，袖剑为强化版）")]
        [Range(0f, 1f)]
        [SerializeField] private float lifestealPercent = 0.1f;
        [Tooltip("击杀后刷新充能的技能名（必须与 SkillData.skillName 完全一致）")]
        [SerializeField] private string skillName = "袖剑";

        [Header("落点墙体修正")]
        [Tooltip("落点检测的墙体 LayerMask（默认 0 = 自动使用 Wall 层）")]
        [SerializeField] private LayerMask wallMask = 0;
        [Tooltip("落点与墙体保留的半径（防止玩家卡进墙里）")]
        [SerializeField] private float landingCollisionRadius = 0.3f;

        // 运行时状态键名（SO 跨施法共享，数据一律存 context 状态字典）
        private const string KeyElapsed = "elapsed";
        private const string KeyStartPos = "startPos";
        private const string KeyLanding = "landing";
        private const string KeyHitDone = "hitDone";
        private const string KeyStarted = "started";

        private bool finished = true;
        public override bool IsDone => finished;

        public override void OnStart(EffectContext context)
        {
            finished = false;

            var state = context.GetOrCreateEffectState(this);
            state[KeyStarted] = true;
            state[KeyElapsed] = 0f;
            state[KeyHitDone] = false;

            if (wallMask.value == 0) wallMask = LayerMask.GetMask("Wall");

            if (context.caster == null || context.lockedTarget == null || context.lockedTarget.GameObject == null)
            {
                // 与目标搜索效果配合的兜底：无有效目标立即收
                context.earlyFinish = true;
                return;
            }

            Vector3 origin = context.caster.transform.position;
            Vector3 targetPosition = context.lockedTarget.GameObject.transform.position;
            Vector2 dir = context.dashDirection.sqrMagnitude > 0.0001f
                ? context.dashDirection
                : ((Vector2)(targetPosition - origin)).normalized;

            Vector3 rawLanding = context.plannedLanding;
            Vector3 correctedLanding = CorrectLanding(targetPosition, dir, HiddenBladeSettings.LandingOffset);
            context.plannedLanding = correctedLanding;

            state[KeyStartPos] = origin;
            state[KeyLanding] = correctedLanding;

            HiddenBladeSettings.RecordCast(origin, targetPosition, rawLanding, correctedLanding);
        }

        public override void OnUpdate(EffectContext context)
        {
            if (finished) return;

            var state = context.GetOrCreateEffectState(this);
            if (!state.ContainsKey(KeyStarted) || !(bool)state[KeyStarted])
            {
                finished = true;
                return;
            }

            float elapsed = (float)state[KeyElapsed] + Time.unscaledDeltaTime;
            state[KeyElapsed] = elapsed;

            float totalDuration = context.duration > 0f ? context.duration : totalDurationFallback;

            // 1) 目标存活检测只在"命中帧之前"有意义：
            //    - 命中前目标被流弹打死/对象被销毁 → 走"中途死亡"分支（滑到落点、不刺击）
            //    - 命中后（包括被袖剑自己击杀）→ 时间线正常走满，绝不再访问目标，
            //      否则敌人帧末销毁后这里会抛 MissingReferenceException 并卡死整个执行器
            bool hitDone = (bool)state[KeyHitDone];
            if (!hitDone && !context.targetDiedMidDash && !IsTargetAlive(context.lockedTarget))
            {
                context.targetDiedMidDash = true;
            }

            // 2) 穿墙冲刺位移（直接改 transform，无视碰撞；FixedUpdate 随 timeScale 变慢不能用）
            float moveT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, dashDuration));
            Vector3 startPos = (Vector3)state[KeyStartPos];
            Vector3 landing = (Vector3)state[KeyLanding];
            // ease-out：起步极快、到位收住
            float eased = 1f - (1f - moveT) * (1f - moveT);
            Vector3 nextPos = Vector3.Lerp(startPos, landing, eased);
            nextPos.z = startPos.z;
            context.caster.transform.position = nextPos;

            // 3) 命中帧（真实时间计时，不使用动画事件；VfxBridge 预留了动画事件桥接）
            //    目标已中途死亡则不刺击（设计：滑到落点立即结束）
            if (!hitDone && !context.targetDiedMidDash && elapsed >= hitTime)
            {
                state[KeyHitDone] = true;
                TryHit(context);
            }

            // 4) 结束判定：
            //    - 目标中途死亡：到位立即结束（执行器收到 earlyFinish 后统一恢复 timeScale/状态/无敌层）
            //    - 正常：走满总时长
            if (context.targetDiedMidDash)
            {
                if (moveT >= 1f)
                {
                    // 设计要求中途死亡也要刷新 CD（无损位移技巧）
                    RefreshCharge(context.caster);
                    context.earlyFinish = true;
                }
                return;
            }

            if (elapsed >= totalDuration)
            {
                finished = true;
            }
        }

        public override void OnEnd(EffectContext context)
        {
            finished = true;
            // timeScale/Executing 由 TimeScaleEffect 的单一出口恢复，这里不重复处理
        }

        // 命中帧：纯单体伤害 + 击杀奖励
        private void TryHit(EffectContext context)
        {
            IDamageable target = context.lockedTarget;
            if (!IsTargetAlive(target))
            {
                // 极端时序：到达命中帧的同一时刻目标刚好被销毁
                context.targetDiedMidDash = true;
                return;
            }

            // 先缓存奖励所需数据：TakeDamage 触发死亡管线后敌人会在帧末被销毁，
            // 后续帧不能再访问它；同一帧内缓存保证 GeneratePureBlood 拿到有效引用
            EnemyBase enemy = (EnemyBase)target;
            GameObject targetObject = enemy.gameObject;
            float targetMaxHealth = enemy.MaxHealth;

            // 命中帧实时方向：施法者当前位置 → 目标当前位置（取一次，O(1)，不每帧追踪）
            Vector2 stabDirection = ((Vector2)enemy.transform.position
                - (Vector2)context.caster.transform.position).normalized;
            if (stabDirection.sqrMagnitude < 0.0001f) stabDirection = context.dashDirection;

            // 刺击特效（纯演出）：处决/普通两个 trigger；目标中途死亡的分支不触发
            HiddenBladeVfxBridge bridge = context.caster.GetComponentInChildren<HiddenBladeVfxBridge>(true);
            if (bridge != null) bridge.TriggerStab(context.isExecutionTarget, stabDirection);

            bool killed = SkillDamageApplier.ApplyHiddenBladeHit(
                target,
                context.isExecutionTarget,
                stabDamage,
                executionDamage,
                context.dashDirection);

            context.targetKilledByBlade = killed;
            if (killed) GrantKillRewards(context.caster, targetObject, targetMaxHealth);
        }

        // 凡被袖剑杀死：加血库 + 强化吸血 PureBlood（不耗血库）+ 刷新 CD
        private void GrantKillRewards(GameObject caster, GameObject killedTargetObject, float killedMaxHealth)
        {
            BloodBank.Instance?.AddBlood(bloodBankReward);

            float pureBloodAmount = killedMaxHealth * lifestealPercent;
            Global.GeneratePureBlood(killedTargetObject, pureBloodAmount);

            RefreshCharge(caster);
        }

        private void RefreshCharge(GameObject caster)
        {
            if (caster == null) return;
            caster.GetComponent<SkillManager>()?.AddCharge(skillName);
        }

        // Unity 生命周期安全判空：
        // IDamageable 是 C# 接口，`target == null` 拦不住 Unity 销毁后的"伪 null"；
        // 必须先转成 EnemyBase(UnityEngine.Object) 用重载的 == 判空，
        // 且在确认存活前不能访问 gameObject/transform 等原生侧成员（会抛 MissingReferenceException）。
        private static bool IsTargetAlive(IDamageable target)
        {
            if (!(target is EnemyBase enemy)) return false;
            if (enemy == null) return false;   // 原生对象已销毁（Unity 重载 ==）
            return !enemy.IsDead;
        }

        // 落点墙体检测与沿墙修正：
        // 从目标位置沿冲刺方向射线，命中墙体则把落点收在墙体前 radius 处；
        // 再用 OverlapCircle 兜底，沿反方向逐步推出墙体。
        private Vector3 CorrectLanding(Vector3 targetPosition, Vector2 dir, float offset)
        {
            Vector3 raw = targetPosition + (Vector3)dir * offset;
            Vector3 corrected = raw;

            RaycastHit2D hit = Physics2D.Raycast(targetPosition, dir, offset + landingCollisionRadius, wallMask);
            if (hit.collider != null)
            {
                float allowedDistance = Mathf.Max(0f, hit.distance - landingCollisionRadius);
                corrected = targetPosition + (Vector3)dir * allowedDistance;
            }

            int guardSteps = 0;
            while (guardSteps < 6
                && Physics2D.OverlapCircle(corrected, landingCollisionRadius, wallMask) != null)
            {
                corrected -= (Vector3)dir * 0.25f;
                guardSteps++;
                // 极端情况（目标本身贴墙）：不允许被推到目标的另一侧之外，直接停在目标点
                if (Vector2.Dot((Vector2)(corrected - targetPosition), dir) <= 0f)
                {
                    corrected = targetPosition;
                    break;
                }
            }
            return corrected;
        }
    }
}