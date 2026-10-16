using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 一次即时命中的伤害参数集：由武器在开火时组装，Resolver 据此统一结算。
    /// 各武器只需声明差异项（未强化系数、吸血比例等），强化加成与吸血规则全局一致。
    /// </summary>
    public readonly struct InstantHitContext
    {
        /// <summary>未强化（血库未付血）时的伤害系数，如普通子弹/霰弹 0.7、激光/狙击 0.8。</summary>
        public readonly float UnenhancedRatio;
        /// <summary>当前弹夹是否被血库强化。</summary>
        public readonly bool IsEnhanced;
        /// <summary>伤害来源武器类型，供强化系统统一乘区。</summary>
        public readonly WeaponType WeaponType;
        /// <summary>吸血比例(%)，0 表示无吸血；敌人侧调用时恒传 0。</summary>
        public readonly float LifestealPercent;

        public InstantHitContext(float unenhancedRatio, bool isEnhanced, WeaponType weaponType, float lifestealPercent)
        {
            UnenhancedRatio = unenhancedRatio;
            IsEnhanced = isEnhanced;
            WeaponType = weaponType;
            LifestealPercent = lifestealPercent;
        }
    }

    /// <summary>
    /// 即时命中结算：统一"取接口 → 死亡守卫 → 强化乘区 → 结算伤害 → 致命吸血"流程。
    /// 当前调用方：PlayerBullet、Laser、BlastAttackExecutor（霰弹枪/AWP 矩形弹道）。
    /// 历史上 PenetratingBullet 曾是复制副本之一（且漏掉强化乘区与 Boss 吸血条件，
    /// 属复制粘贴漂移），该武器已改为矩形即时判定，副本随之一并移除。
    /// </summary>
    public static class InstantHitResolver
    {
        /// <summary>
        /// 结算一次命中。击中方向由命中点相对玩家的位置推导，与历史行为一致。
        /// </summary>
        /// <returns>true 表示伤害已实际结算；false 表示目标无 IDamageable 或已死亡。</returns>
        public static bool Apply(Component hitTarget, float baseDamage, in InstantHitContext ctx)
        {
            // 用 GetComponentInParent 兼容分离结构敌人（碰撞体在 WholeBody 子物体，伤害接口在根节点）
            var damageable = hitTarget.GetComponentInParent<IDamageable>();
            return damageable != null && Apply(damageable, hitTarget, baseDamage, ctx);
        }

        /// <summary>调用方已自行解析出 IDamageable 时使用（如霰弹枪需按敌人去重后再结算）。</summary>
        public static bool Apply(IDamageable damageable, Component hitTarget, float baseDamage, in InstantHitContext ctx)
        {
            // 死亡守卫：敌人已死则跳过，防止重复结算与尸体触发吸血
            if (damageable == null || damageable.CurrentHealth <= 0f) return false;

            // 计算玩家到敌人的方向（供受击表现/击退使用）
            Vector2 hitDir = ((Vector2)hitTarget.transform.position - (Vector2)Player.player1.transform.position).normalized;

            // 伤害乘区：血库强化系数 × 强化系统统一加成
            float damageMultiplier = (ctx.IsEnhanced ? 1.0f : ctx.UnenhancedRatio) * PlayerUpgradeState.GetFinalDamageRatio(ctx.WeaponType);
            float finalDamage = baseDamage * damageMultiplier;

            // 判断本击是否致命（扣血前采样）
            float enemyMaxHP = damageable.MaxHealth;
            bool isLethal = damageable.CurrentHealth - finalDamage <= 0f;

            damageable.TakeDamage(finalDamage, hitDir);

            // 吸血：致命一击时按敌人总生命值换算为 PB 道具；
            // 未强化子弹只对 Boss 生效（小怪吸血仅在强化弹夹下开放，与历史口径一致）
            if (isLethal && ctx.LifestealPercent > 0f && Player.player1 != null && enemyMaxHP > 0f
                && (ctx.IsEnhanced || damageable is BossBase))
            {
                float totalLifesteal = enemyMaxHP * (ctx.LifestealPercent / 100f);
                Global.GeneratePureBlood(hitTarget.gameObject, totalLifesteal);
            }

            return true;
        }
    }
}
