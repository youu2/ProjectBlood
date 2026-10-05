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

        // 输入支持：所有已解锁模块实际配置的监听键并集（触发器/结束条件/门控三类来源）。
        // 每帧只轮询这些键，不做全键盘扫描；在解锁/移除/读档后增量重建（低频操作，全量扫描即可）。
        private static readonly HashSet<KeyCode> watchedKeys = new HashSet<KeyCode>();

        // ---- 主动血印槽位注册表 ----
        // 主动血印 = 任一模块挂了 gate 的血印。按获取顺序自动分配数字键槽位（1~4），
        // 移除/献祭后槽位释放但不压缩（已有槽位含义不变，新主动血印填最低空槽）。
        public const int MaxSigils = 10;        // 血印装备总数上限（含主动+被动）
        public const int MaxActiveSigils = 4;   // 主动血印上限（对应数字键 1~4）

        private static readonly KeyCode[] slotKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        };
        // 索引 0..3 对应数字键 1~4；null=空槽
        private static readonly BloodSigilSO[] activeSigilSlots = new BloodSigilSO[MaxActiveSigils];
        // 血印 → 槽位索引反查
        private static readonly Dictionary<BloodSigilSO, int> sigilSlot
            = new Dictionary<BloodSigilSO, int>();

        // 主动血印槽位变动事件（供未来 HUD 显示"[1] xxx 开/关"）：slot 0..3=分配，-1=释放
        public static event Action<BloodSigilSO, int> ActiveSigilSlotChanged;

        // 门控生命周期阶段转换事件（供 HUD 刷新；解锁/读档的静默创建不触发）
        public static event Action<BloodSigilSO, SigilGatePhase> GatePhaseChanged;

        // ---- 主动血印门控生命周期（仅 ToggleKey 类，血印级共享一个状态机） ----
        // 三态：Ready →(按键)→ Active →(到期/再按键)→ Cooldown →(到期)→ Ready。
        // 不持久化：继续游戏时按初始/就绪状态重建（存档不含剩余 CD/持续时间）。
        private static readonly Dictionary<BloodSigilSO, SigilGateRuntime> gateRuntimes
            = new Dictionary<BloodSigilSO, SigilGateRuntime>();
        // 有限持续（Active）或冷却（Cooldown）中的门控，进 TickGates 推进；无限持续/就绪不进列表
        private static readonly List<BloodSigilSO> timedGates = new List<BloodSigilSO>();

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

        // 事件分发快照：每次分发独立分配，禁止复用静态缓冲。
        // 原因：切枪等结算会触发嵌套事件（开火回调→切枪→切枪事件），
        // 共享静态缓冲会在内层 Clear 时破坏外层 foreach 的枚举器，抛 Collection was modified。
        private static List<BloodSigilSO> CreateSnapshot()
        {
            var snapshot = new List<BloodSigilSO>(unlocked.Count);
            foreach (var sigil in unlocked)
                snapshot.Add(sigil);
            return snapshot;
        }

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
            watchedKeys.Clear();
            for (int i = 0; i < MaxActiveSigils; i++) activeSigilSlots[i] = null;
            sigilSlot.Clear();
            gateRuntimes.Clear();
            timedGates.Clear();
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

            var snapshot = CreateSnapshot();
            foreach (var sigil in snapshot)
            {
                if (!runtimes.TryGetValue(sigil, out var runtime)) continue;
                for (int i = 0; i < runtime.Modules.Count; i++)
                {
                    var rt = runtime.Modules[i];
                    if (rt.Module.trigger != null
                        && rt.Module.trigger.Matches(ctx)
                        && IsGateOpen(rt)
                        && rt.CanFireByCooldown(rt.Module.triggerCooldownSeconds, Time.time)
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

        // 每帧驱动：先轮询输入型条件（触发/结束/门控），再推进门控生命周期与 Duration 计时。
        // 暂停时三者都不执行（与武器开火的暂停策略一致；TickGates 依赖 Time.time 冻结，天然暂停）
        public static void Tick(float deltaTime)
        {
            PollInput();
            TickGates();

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

        // ============================== 输入轮询（按键触发/结束/门控） ==============================

        // 每帧对实际监听的键采样（触发/结束条件的 watchKeys + 已占用主动血印槽位键）：
        //   GetKeyDown → 翻转槽位 ToggleKey 门控 + 派发 KeyPress 触发事件
        //   GetKey     → 派发 KeyHold 触发事件 + 按住槽位 HoldKey 门控
        //   GetKeyUp   → 派发 KeyRelease 触发事件 + 评估 InputRelease 结束条件 + 释放 HoldKey 门控
        // 门控处理与事件分发顺序：同帧同键先处理门控再分发，便于"一键开关自身触发"类配置。
        private static void PollInput()
        {
            if (watchedKeys.Count == 0 || Global.IsGamePaused) return;

            foreach (var key in watchedKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    ToggleSlotGates(key);
                    DispatchKeyEvent(BloodSigilTriggerType.KeyPress, key);
                }
                if (Input.GetKey(key))
                {
                    DispatchKeyEvent(BloodSigilTriggerType.KeyHold, key);
                    ApplyHoldSlotGates(key, true);
                }
                if (Input.GetKeyUp(key))
                {
                    DispatchKeyEvent(BloodSigilTriggerType.KeyRelease, key);
                    LatchKeyReleaseAndEnd(key);
                    ApplyHoldSlotGates(key, false);
                }
            }
        }

        private static void DispatchKeyEvent(BloodSigilTriggerType type, KeyCode key)
            => DispatchEvent(new BloodSigilFireContext
            {
                TriggerType = type,
                PressedKey = key,
            });

        // 槽位键 → 该槽主动血印；非槽位键/空槽返回 false
        private static bool TryGetSigilBySlotKey(KeyCode key, out BloodSigilSO sigil)
        {
            for (int i = 0; i < MaxActiveSigils; i++)
            {
                if (slotKeys[i] == key)
                {
                    sigil = activeSigilSlots[i];
                    return sigil != null;
                }
            }
            sigil = null;
            return false;
        }

        // 翻转槽位血印的 ToggleKey 门控：三态生命周期
        //   Ready   →按键→ Active（开启，有限持续则进 timedGates 计时）
        //   Active  →按键→ 提前关闭 → Cooldown（closeCooldown>0）或直接 Ready（=0）
        //   Cooldown →按键忽略
        // 持续/冷却时长取主 gate 配置；两值均为 0 时退化为普通开关（旧行为逐字节一致）。
        private static void ToggleSlotGates(KeyCode key)
        {
            if (!TryGetSigilBySlotKey(key, out var sigil)) return;
            if (!gateRuntimes.TryGetValue(sigil, out var gr)) return; // HoldKey 门控由电平驱动，无按键生命周期

            var gate = GetPrimaryGate(sigil);
            float now = Time.time;
            if (gr.Phase == SigilGatePhase.Active)
            {
                // 再按一次 = 提前结束，从关闭瞬间起算冷却
                if (gr.Deactivate(now, gate != null ? gate.closeCooldownSeconds : 0f))
                {
                    SyncGateEnabled(sigil, false);
                    if (!gr.Timed) timedGates.Remove(sigil);   // CD=0 直接回就绪，退出计时列表
                    GatePhaseChanged?.Invoke(sigil, gr.Phase);
                }
            }
            else if (gr.Phase == SigilGatePhase.Ready)
            {
                float duration = gate != null ? gate.activeDurationSeconds : 0f;
                if (gr.TryActivate(now, duration))
                {
                    SyncGateEnabled(sigil, true);
                    if (gr.Timed && !timedGates.Contains(sigil)) timedGates.Add(sigil);
                    GatePhaseChanged?.Invoke(sigil, SigilGatePhase.Active);
                }
            }
        }

        // 门控生命周期计时：仅有限持续（Active）与冷却（Cooldown）中的血印进入本列表，
        // 到期才发生转换，零空转；Time.time 在暂停时冻结，无需额外暂停判定
        private static void TickGates()
        {
            if (timedGates.Count == 0) return;
            float now = Time.time;
            for (int i = timedGates.Count - 1; i >= 0; i--)
            {
                var sigil = timedGates[i];
                if (!gateRuntimes.TryGetValue(sigil, out var gr))
                {
                    timedGates.RemoveAt(i);   // 血印已被移除/献祭，清理残留
                    continue;
                }
                var gate = GetPrimaryGate(sigil);
                if (!gr.Tick(now, gate != null ? gate.closeCooldownSeconds : 0f)) continue;

                SyncGateEnabled(sigil, gr.IsEnabled);
                if (!gr.Timed) timedGates.RemoveAt(i);   // 回到就绪（冷却结束或 CD=0）
                GatePhaseChanged?.Invoke(sigil, gr.Phase);
            }
        }

        // 主 gate 配置：该血印第一个挂 gate 的模块（与"主动血印单 gate 模块"约定配套）
        private static BloodSigilGateSO GetPrimaryGate(BloodSigilSO so)
        {
            if (so == null || so.effects == null) return null;
            for (int i = 0; i < so.effects.Count; i++)
            {
                var e = so.effects[i];
                if (e != null && e.gate != null) return e.gate;
            }
            return null;
        }

        // 生命周期阶段统一向下同步到该血印所有 gate 模块的 GateEnabled
        // （Fire 资格判定仍读模块状态，引擎判定路径零改动）
        private static void SyncGateEnabled(BloodSigilSO so, bool enabled)
        {
            if (!runtimes.TryGetValue(so, out var ctx)) return;
            for (int i = 0; i < ctx.Modules.Count; i++)
            {
                if (ctx.Modules[i].Module.gate != null) ctx.Modules[i].GateEnabled = enabled;
            }
        }

        // 创建门控生命周期状态机（解锁/读档时调用，静默不发事件）：
        // 仅 ToggleKey 类主 gate 创建；startEnabled=true 直接进入 Active（无限或有限持续）
        private static void CreateGateRuntime(BloodSigilSO so)
        {
            var gate = GetPrimaryGate(so);
            if (gate == null || gate.gateType != BloodSigilGateType.ToggleKey) return;

            var gr = new SigilGateRuntime();
            gateRuntimes[so] = gr;
            if (gate.startEnabled && gr.TryActivate(Time.time, gate.activeDurationSeconds))
            {
                SyncGateEnabled(so, true);
                if (gr.Timed && !timedGates.Contains(so)) timedGates.Add(so);
            }
        }

        // 槽位键电平直接映射 HoldKey 门控（一个血印只有一个槽位键，无需多键任意按住计算）
        private static void ApplyHoldSlotGates(KeyCode key, bool held)
        {
            if (!TryGetSigilBySlotKey(key, out var sigil)) return;
            if (!runtimes.TryGetValue(sigil, out var ctx)) return;
            for (int i = 0; i < ctx.Modules.Count; i++)
            {
                var rt = ctx.Modules[i];
                if (rt.Module.gate != null && rt.Module.gate.gateType == BloodSigilGateType.HoldKey)
                {
                    rt.GateEnabled = held;
                }
            }
        }

        // 按键松开结束条件：锁存所有激活模块中监听该键的 InputRelease 条件，满足匹配模式即结束
        private static void LatchKeyReleaseAndEnd(KeyCode key)
        {
            for (int i = activeModules.Count - 1; i >= 0; i--)
            {
                var rt = activeModules[i];
                if (!rt.HasEndType(BloodSigilEndConditionType.InputRelease)) continue;
                rt.LatchKeyRelease(key);
                if (rt.IsEndSatisfied(rt.Module.endMatchMode))
                {
                    EndModule(rt);
                }
            }
        }

        // 重建监听键并集（解锁/移除/献祭/读档后调用；低频，直接全量扫描）。
        // 来源：触发器/结束条件上配置的 watchKeys（任意键）+ 已占用主动血印槽位键（1~4）
        private static void RebuildWatchedKeys()
        {
            watchedKeys.Clear();
            foreach (var runtime in runtimes.Values)
            {
                foreach (var rt in runtime.Modules)
                {
                    var trigger = rt.Module.trigger;
                    if (trigger != null
                        && (trigger.triggerType == BloodSigilTriggerType.KeyPress
                            || trigger.triggerType == BloodSigilTriggerType.KeyHold
                            || trigger.triggerType == BloodSigilTriggerType.KeyRelease)
                        && trigger.watchKeys != null)
                    {
                        foreach (var k in trigger.watchKeys) watchedKeys.Add(k);
                    }

                    var ends = rt.Module.endConditions;
                    if (ends != null)
                    {
                        foreach (var end in ends)
                        {
                            if (end != null
                                && end.endConditionType == BloodSigilEndConditionType.InputRelease
                                && end.watchKeys != null)
                            {
                                foreach (var k in end.watchKeys) watchedKeys.Add(k);
                            }
                        }
                    }
                }
            }

            // 已占用槽位键加入轮询（空槽不监听）
            for (int i = 0; i < MaxActiveSigils; i++)
            {
                if (activeSigilSlots[i] != null) watchedKeys.Add(slotKeys[i]);
            }
        }

        // 门控资格：无门控恒开；有门控看 GateEnabled。
        // 门控关闭时事件被整体跳过：不 Fire、不消耗次数、不写触发CD时间戳。
        private static bool IsGateOpen(BloodSigilModuleRuntime rt)
            => rt.Module.gate == null || rt.GateEnabled;

        // ============================== 解锁 / 移除 ==============================

        public static bool IsUnlocked(BloodSigilSO so) => so != null && unlocked.Contains(so);

        public static IReadOnlyCollection<BloodSigilSO> UnlockedSigils => unlocked;

        public static int UnlockedCount => unlocked.Count;

        // 当前已装备的主动血印数（占用槽位数）
        public static int ActiveSigilCount => sigilSlot.Count;

        // 主动血印判定：任一效果模块挂了 gate
        public static bool IsActiveSigil(BloodSigilSO so)
        {
            if (so == null || so.effects == null) return false;
            for (int i = 0; i < so.effects.Count; i++)
            {
                if (so.effects[i] != null && so.effects[i].gate != null) return true;
            }
            return false;
        }

        // 查询主动血印槽位（供 HUD）：未装备返回 false
        public static bool TryGetActiveSlot(BloodSigilSO so, out int slot)
            => sigilSlot.TryGetValue(so, out slot);

        public static BloodSigilSO GetSigilAtSlot(int slot)
            => (slot >= 0 && slot < MaxActiveSigils) ? activeSigilSlots[slot] : null;

        // 槽位对应的开关按键（供 HUD 显示 1~4）
        public static KeyCode GetSlotKey(int slot)
            => (slot >= 0 && slot < MaxActiveSigils) ? slotKeys[slot] : KeyCode.None;

        // ============================== 门控 HUD 查询 ==============================

        // 门控生命周期 HUD 快照：一次取齐阶段、使能态与当前阶段的总/剩余时长
        public struct SigilGateHud
        {
            public SigilGatePhase Phase;   // 就绪 / 持续中 / 冷却中
            public bool Enabled;           // 门控当前使能（仅 Active 为 true）
            public float PhaseTotal;       // 当前阶段总时长（无限持续/就绪为 0）
            public float PhaseRemaining;   // 当前阶段剩余秒数（无计时段为 0）
        }

        // 按槽位取主动血印的门控 HUD 数据；空槽/非 ToggleKey 主动血印返回 false
        public static bool TryGetGateHudAtSlot(int slot, out SigilGateHud hud)
        {
            hud = default;
            var sigil = GetSigilAtSlot(slot);
            if (sigil == null || !gateRuntimes.TryGetValue(sigil, out var gr)) return false;
            hud.Phase = gr.Phase;
            hud.Enabled = gr.IsEnabled;
            hud.PhaseTotal = gr.PhaseTotal;
            hud.PhaseRemaining = gr.GetRemaining(Time.time);
            return true;
        }

        // 分配最低空槽（槽位不压缩）；已满返回 -1
        private static int AllocateSlot(BloodSigilSO so)
        {
            for (int i = 0; i < MaxActiveSigils; i++)
            {
                if (activeSigilSlots[i] == null)
                {
                    activeSigilSlots[i] = so;
                    sigilSlot[so] = i;
                    ActiveSigilSlotChanged?.Invoke(so, i);
                    return i;
                }
            }
            return -1;
        }

        // 释放槽位（不压缩其他槽位）
        private static void ReleaseSlot(BloodSigilSO so)
        {
            if (sigilSlot.TryGetValue(so, out int slot))
            {
                activeSigilSlots[slot] = null;
                sigilSlot.Remove(so);
                ActiveSigilSlotChanged?.Invoke(so, -1);
            }
        }

        public static bool Unlock(BloodSigilSO so)
        {
            if (so == null || unlocked.Contains(so)) return false;

            // 容量防线（掉落池已先行过滤，这里兜底任何解锁途径）
            if (unlocked.Count >= MaxSigils)
            {
                Debug.LogWarning($"[BloodSigil] 血印装备已达上限 {MaxSigils}，无法再解锁 {so.name}");
                return false;
            }
            bool isActive = IsActiveSigil(so);
            if (isActive && sigilSlot.Count >= MaxActiveSigils)
            {
                Debug.LogWarning($"[BloodSigil] 主动血印已达上限 {MaxActiveSigils}，无法再解锁 {so.name}");
                return false;
            }

            var ctx = new BloodSigilRuntimeContext(so);
            unlocked.Add(so);
            runtimes[so] = ctx;

            // 主动血印：按获取顺序分配数字键槽位，并创建门控生命周期状态机
            //（startEnabled=true 时立即进入 Active，保证 OnAcquire 模块能通过门控资格）
            if (isActive)
            {
                AllocateSlot(so);
                CreateGateRuntime(so);
            }

            // OnAcquire 型模块在解锁时立即触发（常驻效果）
            var acquireCtx = new BloodSigilFireContext { TriggerType = BloodSigilTriggerType.OnAcquire };
            foreach (var rt in ctx.Modules)
            {
                if (rt.Module.trigger != null
                    && rt.Module.trigger.Matches(acquireCtx)
                    && IsGateOpen(rt)
                    && rt.CanFireByCooldown(rt.Module.triggerCooldownSeconds, Time.time)
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

            RebuildWatchedKeys();

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
            ReleaseSlot(so);   // 槽位释放但不压缩，新主动血印可填入该空槽
            gateRuntimes.Remove(so);
            timedGates.Remove(so);
            RebuildWatchedKeys();
        }

        // 批量移除（献祭语义）：快照后逐个移除，返回被移除列表
        public static List<BloodSigilSO> RemoveAll(bool removableOnly = true)
        {
            var snapshot = CreateSnapshot();
            var removed = new List<BloodSigilSO>();
            foreach (var sigil in snapshot)
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
            ReleaseSlot(so);
            gateRuntimes.Remove(so);
            timedGates.Remove(so);
            RebuildWatchedKeys();
        }

        public static void AddPermanentDamageBonus(float additiveRatio)
            => permanentDamageBonus += additiveRatio;

        // 存档导出
        public static void ExportTo(RunSaveData data)
        {
            data.permanentDamageBonus = permanentDamageBonus;
            data.damageImmunityCharges = damageImmunityCharges;

            data.sigils.Clear();
            foreach (var sigil in unlocked)
            {
                if (!runtimes.TryGetValue(sigil, out var ctx)) continue;
                var entry = new SigilSaveEntry
                {
                    sigilId = string.IsNullOrEmpty(sigil.id) ? sigil.name : sigil.id,
                };
                foreach (var rt in ctx.Modules)
                {
                    entry.modules.Add(new SigilModuleSaveEntry
                    {
                        firedCount = rt.FiredCount,
                        active = rt.Active,
                        triggerLatched = rt.TriggerLatched,
                        endRemaining = rt.EndRemaining,
                        endLatched = rt.EndLatched,
                    });
                }
                data.sigils.Add(entry);
            }

            // 槽位表按位置导出（固定长度 4，空槽为空字符串）
            data.activeSigilSlotIds = new List<string>(MaxActiveSigils);
            for (int i = 0; i < MaxActiveSigils; i++)
            {
                var s = activeSigilSlots[i];
                data.activeSigilSlotIds.Add(s == null ? string.Empty : (string.IsNullOrEmpty(s.id) ? s.name : s.id));
            }
        }

        // 从存档恢复：静默重建（不触发 OnAcquire 事件、不驱动 SigilUnlocked UI），仅还原模块状态机。
        // 持续型结算效果无需在此重新 OnApply：
        //   - PersistentState 类（属性/层数/CD 等）由 Global / PlayerUpgradeState 的 ImportFrom 恢复其台账；
        //   - PlayerInstance 类（自动瞄准等）由还原流程末尾的 BloodSigilState.OnPlayerSpawned 补回。
        public static void ImportFrom(RunSaveData data, Func<string, BloodSigilSO> soLookup)
        {
            permanentDamageBonus = data.permanentDamageBonus;
            damageImmunityCharges = data.damageImmunityCharges;

            foreach (var entry in data.sigils)
            {
                var sigil = soLookup?.Invoke(entry.sigilId);
                if (sigil == null) continue;
                if (unlocked.Contains(sigil)) continue;

                var ctx = new BloodSigilRuntimeContext(sigil);
                unlocked.Add(sigil);
                runtimes[sigil] = ctx;

                // 恢复模块状态
                for (int i = 0; i < ctx.Modules.Count && i < entry.modules.Count; i++)
                {
                    var rt = ctx.Modules[i];
                    var saved = entry.modules[i];
                    rt.FiredCount = saved.firedCount;
                    rt.TriggerLatched = saved.triggerLatched;

                    // 恢复结束条件数组（长度按配置重建，取存档值）
                    if (saved.endRemaining != null && saved.endRemaining.Length == rt.EndRemaining.Length)
                        Array.Copy(saved.endRemaining, rt.EndRemaining, rt.EndRemaining.Length);
                    if (saved.endLatched != null && saved.endLatched.Length == rt.EndLatched.Length)
                        Array.Copy(saved.endLatched, rt.EndLatched, rt.EndLatched.Length);

                    // 恢复激活态：重新加入激活/计时列表，但不重新触发 OnApply
                    //（数值类效果已在 Global/PlayerUpgradeState 中还原，这里只负责状态机）
                    if (saved.active)
                    {
                        rt.Active = true;
                        activeModules.Add(rt);
                        if (HasDurationEnd(rt.Module)) timedModules.Add(rt);
                    }
                }
            }

            // 主动血印门控生命周期：不持久化（不存剩余持续/CD），统一按初始状态重建——
            // startEnabled=false 的开关类门控还原为就绪态，startEnabled=true 还原为开启态
            foreach (var sigil in unlocked)
            {
                if (IsActiveSigil(sigil)) CreateGateRuntime(sigil);
            }

            RestoreSlots(data, soLookup);

            RebuildWatchedKeys();
        }

        // 恢复主动血印槽位（静默，不发槽位事件，与 ImportFrom 整体语义一致）：
        // 按存档位置恢复，资产缺失/未解锁/非主动血印的槽位留空，不压缩。
        // 旧档（无槽位表字段）主动血印无槽位可开关，需新开一局。
        private static void RestoreSlots(RunSaveData data, Func<string, BloodSigilSO> soLookup)
        {
            if (data.activeSigilSlotIds == null || data.activeSigilSlotIds.Count != MaxActiveSigils) return;

            for (int i = 0; i < MaxActiveSigils; i++)
            {
                var id = data.activeSigilSlotIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                var sigil = soLookup?.Invoke(id);
                if (sigil == null || !unlocked.Contains(sigil) || !IsActiveSigil(sigil)) continue;
                activeSigilSlots[i] = sigil;
                sigilSlot[sigil] = i;
            }
        }

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

        // ============================== 玩家生成后补回 ==============================

        // 玩家场景重建后补回"玩家实例作用域"的激活效果（如自动瞄准开关）。
        // 场景切换/继续游戏时 Player 实例销毁重建，但 BloodSigilState 作为静态状态机存活。
        // 注意：只补 OutcomeScope.PlayerInstance 的效果——PersistentState 类效果
        // （技能层数/CD 减免/属性修改等）写入的是静态台账与 Global 数值，
        // 跨场景本来就存活、读档时也由 PlayerUpgradeState/Global 的 ImportFrom 恢复，
        // 重复 OnApply 会导致每次过场景/读档无限叠加（曾出现翻滚层数每次继续游戏 +4 的事故）。
        // 本方法对同一玩家实例调用多次是安全的：实例作用域的 OnApply 均为幂等设置。
        public static void OnPlayerSpawned()
        {
            for (int i = 0; i < activeModules.Count; i++)
            {
                var rt = activeModules[i];
                if (!rt.Active) continue;
                var outcomes = rt.Module.outcomes;
                if (outcomes == null) continue;
                for (int j = 0; j < outcomes.Count; j++)
                {
                    var outcome = outcomes[j];
                    if (outcome != null && outcome.Scope == BloodSigilOutcomeSO.OutcomeScope.PlayerInstance)
                        outcome.OnApply(rt, BloodSigilFireContext.Empty);
                }
            }
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
            rt.LastFireTime = Time.time; // 记录触发时刻，驱动模块级触发冷却

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
            var snapshot = CreateSnapshot();
            foreach (var sigil in snapshot)
            {
                if (!runtimes.TryGetValue(sigil, out var runtime)) continue;
                for (int i = 0; i < runtime.Modules.Count; i++)
                {
                    var rt = runtime.Modules[i];
                    var trigger = rt.Module.trigger;
                    if (trigger == null) continue;

                    bool met = trigger.Matches(ctx);
                    // 模块级触发冷却（CD=0 时短路恒真，行为与无冷却一致）
                    bool cooldownReady = rt.CanFireByCooldown(rt.Module.triggerCooldownSeconds, Time.time);
                    // 启用门控（无门控恒开）
                    bool gateOpen = IsGateOpen(rt);

                    // 边沿触发（血量阈值）：仅在条件由假变真的跨越瞬间 Fire；
                    // 每次评估都刷新锁存，离开区间后再次跨越可重新触发
                    if (trigger.IsEdgeTrigger)
                    {
                        if (!met)
                        {
                            rt.TriggerLatched = false;
                            continue;
                        }
                        // 门控关闭：不 Fire 也不动锁存，使能后本次区间仍可跨越触发
                        if (!gateOpen) continue;
                        if (rt.TriggerLatched) continue;
                        // CD 未过时本次跨越不算数：不锁存，CD 过后下一次事件仍在区间内可再尝试
                        if (!cooldownReady) continue;
                        rt.TriggerLatched = true;
                        if (!rt.CanFire(rt.Module.maxStacks)) continue;
                    }
                    else if (!met || !gateOpen || !cooldownReady || !rt.CanFire(rt.Module.maxStacks))
                    {
                        continue;
                    }

                    FireModule(rt, ctx);
                    // 结算中若血印被消耗（献祭），停止处理该血印剩余模块
                    if (!runtimes.ContainsKey(sigil)) break;
                }
            }
        }

    }
}
