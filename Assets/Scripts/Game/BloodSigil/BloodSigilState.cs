using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印运行时引擎（静态，对标 PlayerUpgradeState）。
    //
    // 职责：
    //   1. 维护单局已解锁血印集合与每个效果模块的状态机（触发计数、激活态、结束条件计时/锁存）；
    //   2. 统一接收游戏事件（开火/技能/切枪/换弹/受伤/击杀/致命），按"触发条件匹配 + 次数未满"
    //      驱动模块 Fire，把结算分发给各 Outcome；
    //   3. 提供伤害乘区聚合、伤害免疫充能、致命伤害拦截查询；
    //   4. 维护献祭产生的永久增伤台账。
    //
    // 性能模型：
    //   - 开火/技能/切枪/换弹/受伤/击杀全部事件驱动，无轮询；
    //   - 只有含 Duration 结束条件且处于激活态的模块进入 Tick，且仅做减法；
    //   - 切枪/换弹类结束在事件回调内当场判定，不等帧；
    //   - 分发快照使用复用缓冲列表，避免每次事件分配新 List。
    public static class BloodSigilState
    {
        // 已解锁血印（同一血印不可重复叠加）
        private static readonly HashSet<BloodSigilSO> unlocked = new HashSet<BloodSigilSO>();
        private static readonly Dictionary<BloodSigilSO, BloodSigilRuntimeContext> runtimes
            = new Dictionary<BloodSigilSO, BloodSigilRuntimeContext>();

        // 当前激活中的模块（持续型结算效果生效中）
        private static readonly List<BloodSigilModuleRuntime> activeModules = new List<BloodSigilModuleRuntime>();
        // activeModules 的子集：含 Duration 结束条件、需要每帧推进计时的模块
        private static readonly List<BloodSigilModuleRuntime> timedModules = new List<BloodSigilModuleRuntime>();

        // 献祭永久增伤台账（来源血印消失后仍保留至单局结束）
        private static float permanentDamageBonus;

        // "免疫下一次伤害"充能总数（由伤害免疫类结算效果充入）
        private static int damageImmunityCharges;

        // 局外养成：游戏开始时额外解锁的随机血印数量（跨局保留，不随 Reset 清空）
        public static int GlobalRandomSigilUnlockCount { get; private set; }

        // 重置代次防护：Reset 后允许应用一次，防止同局重复解锁
        private static int resetGeneration;
        private static int appliedGeneration = -1;

        public static event Action<BloodSigilSO> SigilUnlocked;

        private static bool initialized;

        // 复用缓冲：事件分发时的血印快照（献祭等会在遍历中增删集合）
        private static readonly List<BloodSigilSO> snapshotBuffer = new List<BloodSigilSO>();

        // ============================== 初始化 / 重置 ==============================

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            WeaponBase.OnWeaponFired += NotifyWeaponFired;
            WeaponBase.OnWeaponReloadStarted += NotifyReloadStarted;
            SkillManager.OnSkillCasted += NotifySkillCast;
        }

        public static void Reset()
        {
            unlocked.Clear();
            runtimes.Clear();
            activeModules.Clear();
            timedModules.Clear();
            snapshotBuffer.Clear();
            permanentDamageBonus = 0f;
            damageImmunityCharges = 0;
            resetGeneration++;   // 允许新一局应用一次全局随机血印
        }

        // 设置全局随机血印解锁数量（供 LegacyUpgradeState.ApplyEffect 调用，绝对值）
        public static void SetGlobalRandomSigilUnlockCount(int count)
        {
            GlobalRandomSigilUnlockCount = Mathf.Max(0, count);
        }

        // 游戏开始时（BloodSigilManager.Awake 调用）按数量解锁随机不重复血印。
        // GetRandomSigil 已过滤已解锁血印，天然不重复；池抽空时返回 null 提前结束。
        public static void ApplyGlobalRandomSigils()
        {
            if (appliedGeneration == resetGeneration) return;
            appliedGeneration = resetGeneration;
            if (GlobalRandomSigilUnlockCount <= 0) return;

            var manager = BloodSigilManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[BloodSigilState] BloodSigilManager 未就绪，跳过全局随机血印解锁");
                return;
            }

            for (int i = 0; i < GlobalRandomSigilUnlockCount; i++)
            {
                var sigil = manager.GetRandomSigil();
                if (sigil == null) break;   // 池中已无未解锁血印
                Unlock(sigil);
            }
        }

        // ============================== 游戏事件入口 ==============================

        // 武器开火事件（WeaponBase.OnWeaponFired 订阅入口；测试可直接调用，weapon 允许为 null）
        public static void NotifyWeaponFired(WeaponBase weapon)
            => DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.WeaponFired,
                Weapon = weapon,
            });

        // 换弹开始事件：仅作为结束条件事件（非触发类型），锁存并当场结束命中的模块
        public static void NotifyReloadStarted(WeaponBase weapon)
        {
            LatchAndEnd(BloodSigilEndConditionType.Reload);
        }

        // 技能施放事件（SkillManager.OnSkillCasted 订阅入口）
        public static void NotifySkillCast(string skillName)
            => DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.SkillCast,
                SkillName = skillName,
            });

        // 切枪：先结束旧枪上的模块，再分发切枪触发
        public static void NotifyWeaponSwitched(WeaponType type)
        {
            LatchAndEnd(BloodSigilEndConditionType.WeaponSwitched);
            DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.WeaponSwitched,
            });
        }

        // 受到伤害（实际扣血后由 Player.TakeDamage 调用）
        public static void NotifyDamageTaken(float damage)
            => DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.DamageTaken,
                Damage = damage,
            });

        // 击杀敌人单位（Enemy.Death 调用）
        public static void NotifyUnitKilled()
            => DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.UnitKilled,
            });

        // 玩家血量发生变化（实际扣血/治疗结算后调用）：
        //   1) 先评估含 HealthThreshold 结束条件的激活模块（电平语义，满足即结束，与切枪事件同序）；
        //   2) 再分发血量阈值触发事件（边沿语义，仅条件由假变真瞬间 Fire）。
        // 百分比取变化后的当前值；边沿锁存保证停留在阈值区间内时不会因后续伤害/治疗重复触发，
        // 血量离开区间（锁存复位）后再次跨越可重新触发。
        public static void NotifyHealthChanged()
        {
            float percent = GetCurrentHealthPercent();
            EvaluateHealthEnds(percent);
            DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.HealthThreshold,
                HealthPercent = percent,
            });
        }

        // 当前血量百分比（0~1）
        private static float GetCurrentHealthPercent()
        {
            float max = Global.INGAME_MAX_HP.Value;
            return max > 0f ? Mathf.Clamp01(Global.currentHP.Value / max) : 0f;
        }

        // 血量阈值结束条件评估：仅遍历含该类条件的激活模块，满足匹配模式即结束（事件驱动，不轮询）
        private static void EvaluateHealthEnds(float percent)
        {
            for (int i = activeModules.Count - 1; i >= 0; i--)
            {
                var rt = activeModules[i];
                if (!rt.HasEndType(BloodSigilEndConditionType.HealthThreshold)) continue;
                if (rt.IsEndSatisfied(rt.Module.endMatchMode, percent))
                {
                    EndModule(rt);
                }
            }
        }

        // 致命伤害拦截：在 Player.TakeDamage 判定本次伤害将致命时调用。
        // 找到第一个"触发条件=致命伤害且次数未满"的模块并 Fire（其结算可能抵消本次伤害/给予护盾），
        // 返回 true 表示本次伤害已被处理（玩家不死亡）。
        public static bool TryHandleLethalDamage(float damage)
        {
            var ctx = new BloodSigilFireContext
            {
                TriggerType = BloodSigilTriggerType.LethalDamage,
                Damage = damage,
            };

            BuildSnapshot();
            foreach (var sigil in snapshotBuffer)
            {
                if (!runtimes.TryGetValue(sigil, out var runtime)) continue;
                for (int i = 0; i < runtime.Modules.Count; i++)
                {
                    var rt = runtime.Modules[i];
                    if (rt.Module.trigger != null
                        && rt.Module.trigger.Matches(ctx)
                        && rt.CanFire(rt.Module.maxStacks))
                    {
                        FireModule(rt, ctx);
                        return true;
                    }
                }
            }
            return false;
        }

        // 消耗一次"免疫下一次伤害"充能（Player.TakeDamage 扣血前调用）
        public static bool ConsumeNextDamageImmunity()
        {
            if (damageImmunityCharges <= 0) return false;
            damageImmunityCharges--;
            return true;
        }

        // ============================== 每帧驱动 ==============================

        // 仅推进含 Duration 条件的激活模块（暂停时 dt=0，与旧被动计时一致）
        public static void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || timedModules.Count == 0) return;

            for (int i = timedModules.Count - 1; i >= 0; i--)
            {
                var rt = timedModules[i];
                rt.TickEndTimers(deltaTime);
                if (rt.IsEndSatisfied(rt.Module.endMatchMode))
                {
                    EndModule(rt);
                }
            }
        }

        // ============================== 解锁 / 移除 ==============================

        public static bool IsUnlocked(BloodSigilSO so) => so != null && unlocked.Contains(so);

        public static IReadOnlyCollection<BloodSigilSO> UnlockedSigils => unlocked;

        public static int UnlockedCount => unlocked.Count;

        public static bool Unlock(BloodSigilSO so)
        {
            if (so == null || unlocked.Contains(so)) return false;

            var ctx = new BloodSigilRuntimeContext(so);
            unlocked.Add(so);
            runtimes[so] = ctx;

            // OnAcquire 型模块在解锁时立即触发（常驻效果）
            var acquireCtx = new BloodSigilFireContext { TriggerType = BloodSigilTriggerType.OnAcquire };
            foreach (var rt in ctx.Modules)
            {
                if (rt.Module.trigger != null
                    && rt.Module.trigger.Matches(acquireCtx)
                    && rt.CanFire(rt.Module.maxStacks))
                {
                    FireModule(rt, acquireCtx);
                    // 献祭类模块可能已在结算中 Consume 掉本血印
                    if (!runtimes.ContainsKey(so)) break;
                }
            }

            // 解锁即激活的常驻模块：若其 HealthThreshold 结束条件在获得时就已满足
            // （如低血时获得"血量低于30%即结束"的增益），按电平语义立即结束
            if (runtimes.ContainsKey(so))
            {
                EvaluateHealthEnds(GetCurrentHealthPercent());
            }

            SigilUnlocked?.Invoke(so);
            return true;
        }

        // 移除血印：激活模块全部走 OnRemove 对称撤销
        public static void Remove(BloodSigilSO so)
        {
            if (so == null || !unlocked.Contains(so)) return;

            if (runtimes.TryGetValue(so, out var ctx))
            {
                foreach (var rt in ctx.Modules)
                {
                    EndModule(rt);
                }
                runtimes.Remove(so);
            }
            unlocked.Remove(so);
        }

        // 批量移除（献祭语义）：快照后逐个移除，返回被移除列表
        public static List<BloodSigilSO> RemoveAll(bool removableOnly = true)
        {
            BuildSnapshot();
            var removed = new List<BloodSigilSO>();
            foreach (var sigil in snapshotBuffer)
            {
                if (removableOnly && !sigil.removable) continue;
                Remove(sigil);
                removed.Add(sigil);
            }
            return removed;
        }

        // 自我消耗（献祭血印用）：不触发 OnRemove 直接摘除
        public static void Consume(BloodSigilSO so)
        {
            if (so == null || !unlocked.Contains(so)) return;
            if (runtimes.TryGetValue(so, out var ctx))
            {
                foreach (var rt in ctx.Modules)
                {
                    // 仅摘状态，不做对称撤销（献祭收益需保留）
                    rt.Active = false;
                    activeModules.Remove(rt);
                    timedModules.Remove(rt);
                }
                runtimes.Remove(so);
            }
            unlocked.Remove(so);
        }

        public static void AddPermanentDamageBonus(float additiveRatio)
            => permanentDamageBonus += additiveRatio;

        public static void AddDamageImmunityCharges(int charges)
        {
            if (charges > 0) damageImmunityCharges += charges;
        }

        // 扣减"免疫下一次伤害"充能（模块结束时结算效果做对称撤销）。
        // 充能池在多来源间共享、消耗时无法区分归属，因此夹到 [0, 当前余量]：
        // 已在持续期间被消耗的次数无法也不应恢复，剩余部分由结束模块撤下，不会扣成负数。
        public static void RemoveDamageImmunityCharges(int charges)
        {
            if (charges <= 0) return;
            damageImmunityCharges = Mathf.Max(0, damageImmunityCharges - charges);
        }

        // ============================== 查询 ==============================

        // 输出伤害乘法系数：所有激活模块的结算效果系数连乘 ×（1 + 永久台账）
        public static float GetOutgoingDamageMultiplier(WeaponType weapon)
        {
            float ratio = 1f;
            for (int i = 0; i < activeModules.Count; i++)
            {
                var rt = activeModules[i];
                var outcomes = rt.Module.outcomes;
                if (outcomes == null) continue;
                foreach (var outcome in outcomes)
                {
                    if (outcome == null) continue;
                    float m = outcome.GetDamageMultiplier(rt, weapon);
                    if (m > 0f) ratio *= m;
                }
            }
            ratio *= (1f + permanentDamageBonus);
            return ratio;
        }

        // ============================== 内部：模块状态机 ==============================

        private static void FireModule(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx)
        {
            var module = rt.Module;
            rt.FiredCount++;

            if (!rt.Active)
            {
                rt.ResetEndTracking();
                rt.Active = true;
                activeModules.Add(rt);
                if (HasDurationEnd(module)) timedModules.Add(rt);

                for (int i = 0; i < module.outcomes.Count; i++)
                    module.outcomes[i]?.OnApply(rt, ctx);
            }
            else
            {
                // 已激活：刷新结束条件计时/锁存，并驱动累加型结算
                rt.ResetEndTracking();
                for (int i = 0; i < module.outcomes.Count; i++)
                    module.outcomes[i]?.OnRefresh(rt, ctx);
            }

            // 瞬时结算（每次触发都执行；献祭可能在此处移除血印）
            for (int i = 0; i < module.outcomes.Count; i++)
                module.outcomes[i]?.OnImmediate(rt, ctx);
        }

        private static void EndModule(BloodSigilModuleRuntime rt)
        {
            if (!rt.Active) return;
            rt.Active = false;
            activeModules.Remove(rt);
            timedModules.Remove(rt);

            var outcomes = rt.Module.outcomes;
            if (outcomes == null) return;
            foreach (var outcome in outcomes)
            {
                outcome?.OnRemove(rt);
            }
        }

        // 游戏事件类结束条件：锁存所有激活模块后立即判定（事件驱动，不等 Tick）
        private static void LatchAndEnd(BloodSigilEndConditionType eventType)
        {
            for (int i = activeModules.Count - 1; i >= 0; i--)
            {
                var rt = activeModules[i];
                rt.LatchEndEvent(eventType);
                if (rt.IsEndSatisfied(rt.Module.endMatchMode))
                {
                    EndModule(rt);
                }
            }
        }

        private static bool HasDurationEnd(BloodSigilEffect module)
        {
            if (module.endConditions == null) return false;
            foreach (var end in module.endConditions)
            {
                if (end != null && end.endConditionType == BloodSigilEndConditionType.Duration)
                    return true;
            }
            return false;
        }

        private static void DispatchEvent(in BloodSigilFireContext ctx)
        {
            BuildSnapshot();
            foreach (var sigil in snapshotBuffer)
            {
                if (!runtimes.TryGetValue(sigil, out var runtime)) continue;
                for (int i = 0; i < runtime.Modules.Count; i++)
                {
                    var rt = runtime.Modules[i];
                    var trigger = rt.Module.trigger;
                    if (trigger == null) continue;

                    bool met = trigger.Matches(ctx);

                    // 边沿触发（血量阈值）：仅在条件由假变真的跨越瞬间 Fire；
                    // 每次评估都刷新锁存，离开区间后再次跨越可重新触发
                    if (trigger.IsEdgeTrigger)
                    {
                        if (!met)
                        {
                            rt.TriggerLatched = false;
                            continue;
                        }
                        if (rt.TriggerLatched) continue;
                        rt.TriggerLatched = true;
                        if (!rt.CanFire(rt.Module.maxStacks)) continue;
                    }
                    else if (!met || !rt.CanFire(rt.Module.maxStacks))
                    {
                        continue;
                    }

                    FireModule(rt, ctx);
                    // 结算中若血印被消耗（献祭），停止处理该血印剩余模块
                    if (!runtimes.ContainsKey(sigil)) break;
                }
            }
        }

        private static void BuildSnapshot()
        {
            snapshotBuffer.Clear();
            foreach (var sigil in unlocked)
                snapshotBuffer.Add(sigil);
        }
    }
}
