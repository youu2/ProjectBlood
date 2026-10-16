using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 矩形弹道即时攻击的参数包：由各武器以自身 Inspector 字段组装。
    /// 保持各武器字段独立序列化（不迁移 ShotGun 既有字段，避免场景/预制体丢值），
    /// 差异仅在数值（矩形数量、伤害、长度、宽度、散布角）。
    /// </summary>
    public readonly struct BlastAttackParams
    {
        public readonly int PelletCount;       // 每次开火生成的弹道矩形数量
        public readonly float Damage;          // 单个矩形的基础伤害
        public readonly float Length;          // 矩形最大长度（被墙截短前）
        public readonly float HitboxWidth;     // 伤害判定框宽度
        public readonly float VisualWidth;     // 视觉矩形宽度
        public readonly float SpreadAngle;     // 随机散布角度
        public readonly LayerMask WallMask;    // 阻挡层级；为 0 时执行器回退为 "Wall"
        public readonly ShotgunBlastFx FxPrefab; // 淡出视觉预制体（无碰撞体）

        public BlastAttackParams(int pelletCount, float damage, float length,
            float hitboxWidth, float visualWidth, float spreadAngle,
            LayerMask wallMask, ShotgunBlastFx fxPrefab)
        {
            PelletCount = pelletCount;
            Damage = damage;
            Length = length;
            HitboxWidth = hitboxWidth;
            VisualWidth = visualWidth;
            SpreadAngle = spreadAngle;
            WallMask = wallMask;
            FxPrefab = fxPrefab;
        }
    }

    /// <summary>
    /// 矩形弹道即时判定执行器：霰弹枪与狙击枪共用同一套判定逻辑。
    /// 开火当帧对每个散射矩形：
    /// 1. 独立随机散布角（SpreadMath）；
    /// 2. 中心线 Raycast 测墙距并截短矩形（零宽检测，接受门框/墙角边缘的轻微误判）；
    /// 3. OverlapBoxAll 区域全命中，矩形内按敌人去重，矩形之间叠加；
    /// 4. 同位置/同截短长度生成无碰撞体视觉矩形，0.5 秒淡出。
    /// 敌人不加入阻挡 mask，墙前纵深内的敌人全部命中。
    /// </summary>
    public static class BlastAttackExecutor
    {
        public static void Fire(in BlastAttackParams p, Vector2 baseDirection, Vector2 origin, in InstantHitContext ctx)
        {
            var enemyMask = LayerMask.GetMask("Enemy");
            LayerMask wallMask = p.WallMask;
            if (wallMask.value == 0) wallMask = LayerMask.GetMask("Wall"); // 未配置时回退（与 EnemyBase 同约定）

            for (int i = 0; i < p.PelletCount; i++)
            {
                // 每个矩形独立随机散布角，判定与视觉共用同一方向，保证所见即所得
                Vector2 direction = SpreadMath.GetRandomDirection(baseDirection, p.SpreadAngle);
                float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // 中心线 Raycast 测墙距并截短矩形
                float effectiveLength = p.Length;
                var wallHit = Physics2D.Raycast(origin, direction, p.Length, wallMask);
                if (wallHit.collider != null) effectiveLength = wallHit.distance;

                // 贴墙开枪守卫：枪口已在墙内时不判定也不生成视觉，避免零尺寸对象
                if (effectiveLength <= 0.01f) continue;

                // 矩形从中心生成，在枪口前方半长处
                Vector2 center = origin + direction * (effectiveLength * 0.5f);

                // 伤害判定只在开火当帧完成（宽度独立配置，可与视觉宽度不同）
                var hits = Physics2D.OverlapBoxAll(center, new Vector2(effectiveLength, p.HitboxWidth), angleDeg, enemyMask);
                var damaged = new HashSet<IDamageable>();
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable == null || !damaged.Add(damageable)) continue;
                    InstantHitResolver.Apply(damageable, hit, p.Damage, ctx);
                }

                // 视觉：生成无碰撞体矩形，0.5 秒淡出（与判定共用截短长度，宽度独立配置）
                if (p.FxPrefab != null)
                {
                    var fx = Object.Instantiate(p.FxPrefab, center, Quaternion.AngleAxis(angleDeg, Vector3.forward));
                    fx.Play(effectiveLength, p.VisualWidth);
                }
            }
        }
    }
}
