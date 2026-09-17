namespace ProjectBlood
{
    // 一次游戏事件触发时的上下文（传给触发条件匹配与结算效果）。
    // struct + 可空字段语义：未使用的信息保持默认值，由触发类型决定读取哪些字段。
    public struct BloodSigilFireContext
    {
        // 本次事件类型
        public BloodSigilTriggerType TriggerType;

        // SkillCast：技能名（SkillData.skillName）
        public string SkillName;

        // WeaponFired / WeaponSwitched / Reload：相关武器
        public WeaponBase Weapon;

        // DamageTaken / LethalDamage：伤害值
        public float Damage;
    }
}
