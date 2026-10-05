using UnityEngine;

namespace ProjectBlood
{
    // 启用门控类型
    public enum BloodSigilGateType
    {
        // 按键翻转：每按一次槽位键，使能状态在 开/关 之间切换（类似技能开关）
        ToggleKey = 0,
        // 按住使能：按住槽位键期间使能，松开即失能
        HoldKey = 1,
    }

    // 血印模块启用门控（第五要素，可选）：
    // 与触发条件正交——触发条件回答"何时发生事件"，门控回答"当前是否允许 Fire"。
    // 门控关闭时，引擎在 Fire 资格判定处直接跳过该模块：
    //   不触发任何 Outcome、不消耗 MaxStacks、不写触发冷却时间戳。
    // 不挂门控资产的模块永远使能（被动血印，零影响）。
    //
    // 按键不由本资产配置：挂了 gate 的血印即为"主动血印"，由 BloodSigilState
    // 按主动血印获取顺序自动分配槽位键（数字键 1~4，槽位不压缩）。
    // 同一血印内所有 gate 模块共享其槽位键。
    //
    // 典型配置：WeaponFired 触发 + 切枪结算 + ToggleKey 门控 + 0.2s 触发CD
    //          → 该血印获得槽位（如键 1）后，按 1 开关"开火切枪"。
    [CreateAssetMenu(fileName = "SigilGate_", menuName = "血印系统/启用门控", order = 12)]
    public class BloodSigilGateSO : ScriptableObject
    {
        [Tooltip("门控类型：按槽位键翻转开关 / 按住槽位键期间使能")]
        public BloodSigilGateType gateType = BloodSigilGateType.ToggleKey;

        [Tooltip("解锁血印时门控的初始状态：false=需按一下槽位键才开启（开关类血印的典型配置）")]
        public bool startEnabled = false;

        [Header("生命周期（仅 ToggleKey 生效；HoldKey 为纯按住电平，不受影响）")]
        [Tooltip("开启后的最长持续时间（秒，变身类技能）：到期自动关闭；0=无限持续，再按一次键关闭")]
        [Min(0f)] public float activeDurationSeconds = 0f;

        [Tooltip("关闭后的冷却时间（秒）：从关闭瞬间（含提前手动关闭）起算，冷却中按键无效；0=无冷却，随时可再开启")]
        [Min(0f)] public float closeCooldownSeconds = 0f;
    }
}
