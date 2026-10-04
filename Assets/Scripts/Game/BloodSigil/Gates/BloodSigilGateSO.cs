using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 启用门控类型
    public enum BloodSigilGateType
    {
        // 按键翻转：每按一次监听键，使能状态在 开/关 之间切换（类似技能开关）
        ToggleKey = 0,
        // 按住使能：按住任一听键期间使能，全部松开即失能
        HoldKey = 1,
    }

    // 血印模块启用门控（第五要素，可选）：
    // 与触发条件正交——触发条件回答"何时发生事件"，门控回答"当前是否允许 Fire"。
    // 门控关闭时，引擎在 Fire 资格判定处直接跳过该模块：
    //   不触发任何 Outcome、不消耗 MaxStacks、不写触发冷却时间戳。
    // 不挂门控资产的模块永远使能（现有全部血印默认行为，零影响）。
    //
    // 典型配置：WeaponFired 触发 + 切枪结算 + ToggleKey(Space) 门控 + 0.2s 触发CD
    //          → 按空格开启"开火切枪"，再按关闭。
    [CreateAssetMenu(fileName = "SigilGate_", menuName = "血印系统/启用门控", order = 12)]
    public class BloodSigilGateSO : ScriptableObject
    {
        [Tooltip("门控类型：按键翻转开关 / 按住期间使能")]
        public BloodSigilGateType gateType = BloodSigilGateType.ToggleKey;

        [Tooltip("监听按键（任一键命中即生效；含鼠标键 Mouse0~6）")]
        public List<KeyCode> watchKeys = new List<KeyCode>();

        [Tooltip("解锁血印时门控的初始状态：false=需按一下才开启（开关类血印的典型配置）")]
        public bool startEnabled = false;

        // 是否监听指定键：空列表视为任意键
        public bool IsWatchingKey(KeyCode key)
            => watchKeys == null || watchKeys.Count == 0 || watchKeys.Contains(key);
    }
}
