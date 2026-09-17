namespace ProjectBlood
{
    // 血印模块触发条件类型（枚举驱动，便于 Inspector 配置与 JSON/表格导入）。
    // 新增触发类型时：在末尾追加枚举 -> BloodSigilState 增加对应 Notify/查询入口 -> 游戏事件点调用。
    public enum BloodSigilTriggerType
    {
        // 获得血印后立即触发一次（常驻属性/光环类，结束条件配 Unlimited）
        OnAcquire = 0,
        // 武器单次开火时触发（对应 WeaponBase.OnWeaponFired）
        WeaponFired = 1,
        // 受到致命伤害（本次伤害将导致死亡）时触发；由 Player.TakeDamage 在死亡判定前查询
        LethalDamage = 2,
        // 受到伤害（实际扣血）时触发
        DamageTaken = 3,
        // 击杀敌人单位时触发（Enemy.Death）
        UnitKilled = 4,
        // 使用特定技能时触发（SkillManager.OnSkillCasted，可按技能名过滤）
        SkillCast = 5,
        // 切换武器时触发
        WeaponSwitched = 6,
    }
}
