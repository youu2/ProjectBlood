using UnityEngine;

namespace ProjectBlood
{
    // 门控生命周期阶段（仅 ToggleKey 类门控使用）：
    //   Ready    就绪，可按键开启
    //   Active   变身/生效中，模块 GateEnabled=true；到期或再按键关闭
    //   Cooldown 冷却中，按键无效；到期回到 Ready
    public enum SigilGatePhase
    {
        Ready = 0,
        Active = 1,
        Cooldown = 2,
    }

    // 主动血印门控的三态生命周期状态机（血印级，同血印所有 gate 模块共享）。
    // 纯逻辑计时，不含任何游戏对象引用；时间基准 Time.time（暂停时随 timeScale 冻结）。
    // 转换规则：
    //   Ready    --按键(duration)-->  Active（duration<=0 为无限持续）
    //   Active   --到期/再按键-->     Cooldown（closeCooldown>0）或直接 Ready（closeCooldown=0）
    //   Cooldown --到期-->            Ready
    // 不持久化：继续游戏时由引擎按初始/就绪状态重建。
    public class SigilGateRuntime
    {
        public SigilGatePhase Phase { get; private set; } = SigilGatePhase.Ready;

        // 当前阶段是否有限时长（false = 无限持续 Active 或瞬时 Ready，无需计时）
        public bool Timed { get; private set; }

        // 当前阶段总时长（供 HUD 算进度比例；无计时段为 0）
        public float PhaseTotal { get; private set; }

        // 当前阶段截止时刻（Time.time）
        private float phaseEndTime;

        public bool IsEnabled => Phase == SigilGatePhase.Active;

        // 归位到就绪（血印重置/清理用）
        public void ResetToReady()
        {
            Phase = SigilGatePhase.Ready;
            Timed = false;
            PhaseTotal = 0f;
        }

        // Ready → Active；返回 false 表示当前不可开启（Cooldown 中）
        public bool TryActivate(float now, float duration)
        {
            if (Phase != SigilGatePhase.Ready) return false;
            EnterPhase(SigilGatePhase.Active, now, duration);
            return true;
        }

        // Active → Cooldown（closeCooldown>0）或直接 Ready（=0）；仅 Active 态有效（提前关闭用）
        public bool Deactivate(float now, float closeCooldown)
        {
            if (Phase != SigilGatePhase.Active) return false;
            EnterPhase(closeCooldown > 0f ? SigilGatePhase.Cooldown : SigilGatePhase.Ready, now, closeCooldown);
            return true;
        }

        // 阶段计时推进；发生转换返回 true（引擎需同步 GateEnabled 并发事件）
        public bool Tick(float now, float closeCooldown)
        {
            if (!Timed || now < phaseEndTime) return false;
            if (Phase == SigilGatePhase.Active)
            {
                // 持续到期 → 冷却（或 CD=0 直接回就绪）
                EnterPhase(closeCooldown > 0f ? SigilGatePhase.Cooldown : SigilGatePhase.Ready, now, closeCooldown);
                return true;
            }
            ResetToReady();   // 冷却到期 → 就绪
            return true;
        }

        // 当前阶段剩余秒数（无计时段恒 0）
        public float GetRemaining(float now)
            => Timed ? Mathf.Max(0f, phaseEndTime - now) : 0f;

        private void EnterPhase(SigilGatePhase phase, float now, float total)
        {
            Phase = phase;
            Timed = total > 0f;
            PhaseTotal = Mathf.Max(0f, total);
            phaseEndTime = now + PhaseTotal;
        }
    }
}
