using ProjectBlood;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 袖剑伤害结算助手（静态）。
    /// 关键约定：奖励判定必须以"死亡管线实际执行"为准——
    /// 伤害前记录 IsDead，TakeDamage 后再读一次，避免无敌/减伤导致
    /// "大数字没打死却错误发奖励"。
    /// </summary>
    public static class SkillDamageApplier
    {
        /// <summary>
        /// 对袖剑锁定目标施加一次单体伤害。
        /// </summary>
        /// <param name="execute">true=处决伤害（固定大常量，不吃加成）；false=刺击伤害（吃技能增伤乘区）</param>
        /// <returns>本次伤害是否实际导致目标进入死亡管线</returns>
        public static bool ApplyHiddenBladeHit(IDamageable target, bool execute,
            float stabDamage, float executionDamage, Vector2 hitDirection)
        {
            // 先转 UnityEngine.Object 做 Unity 伪 null 判空（不能先访问 target.GameObject）
            if (!(target is EnemyBase enemy) || enemy == null || enemy.IsDead) return false;

            float damage = execute
                ? Mathf.Max(0f, executionDamage)
                : Mathf.Max(0f, stabDamage) * PlayerUpgradeState.GetFinalSkillDamageRatio();

            target.TakeDamage(damage, hitDirection);

            // 死亡管线（EnemyBase/BossBase.Death）实际执行过才算袖剑击杀
            return enemy.IsDead;
        }
    }
}
