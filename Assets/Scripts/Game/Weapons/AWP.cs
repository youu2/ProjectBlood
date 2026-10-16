using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// AWP 狙击枪：与霰弹枪完全相同的矩形即时判定（单发长矩形），不再生成真实飞行子弹。
    /// 判定/截短/淡出逻辑全部复用 BlastAttackExecutor；触发（半自动攻速/弹药/换弹）
    /// 与开火反馈走 SemiAutomaticWeapon / WeaponBase 的同一套流程。
    /// 与霰弹枪的差异仅为数值：单矩形（pelletCount=1）、长射程、高伤害；
    /// 未强化伤害系数沿用历史口径 0.8（霰弹枪为 0.7）。
    /// </summary>
    public class AWP : SemiAutomaticWeapon, IDirectDamageWeapon
    {
        [SerializeField] private int pelletCount = 1;        // 狙击枪固定单发矩形
        [SerializeField] private float blastDamage = 120f;  // 沿用原 AWPBullet.damage
        [SerializeField] private float blastLength = 20f;   // 长矩形，约一屏以上（Inspector 可调）
        [SerializeField] private float hitboxWidth = 0.42f; // 判定宽度，沿用原子弹碰撞体宽度
        [SerializeField] private float visualWidth = 0.42f; // 视觉宽度（可独立调粗）
        [Tooltip("阻挡弹道的层级（默认 Wall）。在 Inspector 配置后可加入门、可破坏箱子等层")]
        [SerializeField] private LayerMask wallMask;        // 为 0 时运行时回退为 "Wall" 层
        [Tooltip("弹道矩形视觉预制体：SpriteRenderer(矩形精灵) + ShotgunBlastFx 组件，不含任何碰撞体")]
        [SerializeField] private ShotgunBlastFx blastFxPrefab;

        // 暂停页伤害面板读取直伤口径（无子弹预制体可读取）
        public float BaseDamage => blastDamage;

        public override void Attack(Vector2 shootDir)
        {
            Vector2 baseDirection = shootDir.normalized;
            var ctx = new InstantHitContext(0.8f, IsBulletEnhanced, WeaponType, Lifesteal.LifestealPercent);
            var parameters = new BlastAttackParams(pelletCount, blastDamage, blastLength,
                hitboxWidth, visualWidth, spreadAngle, wallMask, blastFxPrefab);
            BlastAttackExecutor.Fire(parameters, baseDirection, BulletSpawnPoint.position, ctx);

            // 开火反馈与霰弹枪完全一致（音效/枪口火/震屏/动画/抛壳/事件）
            PlayHitscanFireFeedback(baseDirection);
        }
    }
}
