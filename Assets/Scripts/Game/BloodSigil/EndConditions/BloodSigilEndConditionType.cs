namespace ProjectBlood
{
    // 血印模块结束条件类型。
    // Unlimited / 空结束条件列表：不主动结束，持续到血印被移除（常驻效果）。
    public enum BloodSigilEndConditionType
    {
        // 不主动结束（持续到血印移除）
        Unlimited = 0,
        // 固定持续时间后自动结束（时长由 BloodSigilEndConditionSO.duration 配置）
        Duration = 1,
        // 切枪时结束
        WeaponSwitched = 2,
        // 换弹（开始换弹）时结束
        Reload = 3,
        // 当前血量百分比满足阈值条件（大于/等于/小于，比较参数复用触发条件）时结束；
        // 电平语义：条件持续满足即结束，由每次血量变化事件驱动评估（非边沿）
        HealthThreshold = 4,
    }
}
