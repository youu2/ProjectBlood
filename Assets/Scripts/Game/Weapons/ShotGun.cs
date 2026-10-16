using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectBlood
{
    /// <summary>
    /// 霰弹枪：开火瞬间从枪口向多个散射方向生成定长弹道矩形，判定与视觉解耦。
    /// 判定逻辑全部收敛在 BlastAttackExecutor（与 AWP 狙击枪共用），
    /// 本类只负责参数配置、伤害口径与开火反馈；矩形内去重、矩形间叠加、
    /// 墙体中心线截短、0.5 秒淡出等行为见执行器。
    /// </summary>
    public partial class ShotGun : SemiAutomaticWeapon, IDirectDamageWeapon
    {
        [FormerlySerializedAs("bulletCount")]
        [SerializeField] private int pelletCount = 5;       // 每次开火生成的弹道矩形数量
        [SerializeField] private float blastDamage = 20f;   // 单个矩形的基础伤害（沿用原霰弹单颗子弹伤害）
        [SerializeField] private float blastLength = 7f;    // 弹道矩形长度（约半个屏幕宽，Inspector 可调）
        [FormerlySerializedAs("blastWidth")]
        [SerializeField] private float hitboxWidth = 1.2f;  // 伤害判定框宽度（仅用于 OverlapBoxAll）
        [SerializeField] private float visualWidth = 1.2f;  // 视觉矩形宽度（仅用于淡出特效，可与判定宽度独立调整）
        [Tooltip("阻挡弹道的层级（默认 Wall）。在 Inspector 配置后可加入门、可破坏箱子等层")]
        [SerializeField] private LayerMask wallMask;        // 为 0 时运行时回退为 "Wall" 层（与 EnemyBase 同约定）
        [Tooltip("弹道矩形视觉预制体：SpriteRenderer(矩形精灵) + ShotgunBlastFx 组件，不含任何碰撞体")]
        [SerializeField] private ShotgunBlastFx blastFxPrefab;

        // 暂停页伤害面板读取直伤口径（无子弹预制体可读取）
        public float BaseDamage => blastDamage;

        public override void Attack(Vector2 shootDir)
        {
            Vector2 baseDirection = shootDir.normalized;
            var ctx = new InstantHitContext(0.7f, IsBulletEnhanced, WeaponType, Lifesteal.LifestealPercent);
            BlastAttackExecutor.Fire(BuildParams(), baseDirection, BulletSpawnPoint.position, ctx);

            // 开火反馈与所有即时命中武器共用（音效/枪口火/震屏/动画/抛壳/事件）
            PlayHitscanFireFeedback(baseDirection);
        }

        private BlastAttackParams BuildParams()
            => new BlastAttackParams(pelletCount, blastDamage, blastLength,
                hitboxWidth, visualWidth, spreadAngle, wallMask, blastFxPrefab);
    }
}
