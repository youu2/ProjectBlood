using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectBlood
{
    /// <summary>
    /// 霰弹枪：开火瞬间从枪口向多个散射方向生成定长弹道矩形，
    /// 同帧用 OverlapBoxAll 完成全部伤害判定（判定与视觉解耦）：
    /// - 矩形不移动、长度约半屏，模拟短射程；
    /// - 被矩形覆盖的敌人立即受伤，被多个矩形同时覆盖则叠加结算（近距离爆发）；
    /// - 矩形内部按敌人去重（兼容分离结构的多碰撞体），矩形之间不去重；
    /// - 视觉矩形无碰撞体，0.5 秒淡出。
    /// </summary>
    public partial class ShotGun : SemiAutomaticWeapon
    {
        [FormerlySerializedAs("bulletCount")]
        [SerializeField] private int pelletCount = 5;       // 每次开火生成的弹道矩形数量
        [SerializeField] private float blastDamage = 20f;   // 单个矩形的基础伤害（沿用原霰弹单颗子弹伤害）
        [SerializeField] private float blastLength = 7f;    // 弹道矩形长度（约半个屏幕宽，Inspector 可调）
        [FormerlySerializedAs("blastWidth")]
        [SerializeField] private float hitboxWidth = 1.2f;  // 伤害判定框宽度（仅用于 OverlapBoxAll）
        [SerializeField] private float visualWidth = 1.2f;  // 视觉矩形宽度（仅用于淡出特效，可与判定宽度独立调整）
        [Tooltip("弹道矩形视觉预制体：SpriteRenderer(矩形精灵) + ShotgunBlastFx 组件，不含任何碰撞体")]
        [SerializeField] private ShotgunBlastFx blastFxPrefab;

        public override void Attack(Vector2 shootDir)
        {
            // 基准方向（准心瞄准方向）
            Vector2 baseDirection = shootDir.normalized;
            Vector2 origin = BulletSpawnPoint.position;
            var enemyMask = LayerMask.GetMask("Enemy"); // 判定只查敌人层：简单版穿墙，不做墙体裁剪
            var ctx = new InstantHitContext(0.7f, IsBulletEnhanced, WeaponType, Lifesteal.LifestealPercent);

            for (int i = 0; i < pelletCount; i++)
            {
                // 每个矩形独立随机散布角，判定与视觉共用同一方向，保证所见即所得
                Vector2 direction = SpreadMath.GetRandomDirection(baseDirection, spreadAngle);
                float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // 矩形中心在枪口前方半长处，使矩形从枪口"长出来"而不是居中跨在枪口上
                Vector2 center = origin + direction * (blastLength * 0.5f);

                // 伤害判定只在开火当帧完成（宽度独立配置，可与视觉宽度不同）
                var hits = Physics2D.OverlapBoxAll(center, new Vector2(blastLength, hitboxWidth), angleDeg, enemyMask);
                var damaged = new HashSet<IDamageable>();
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable == null || !damaged.Add(damageable)) continue;
                    InstantHitResolver.Apply(damageable, hit, blastDamage, ctx);
                }

                // 视觉：生成无碰撞体矩形，0.5 秒淡出（宽度独立配置）
                if (blastFxPrefab != null)
                {
                    var fx = Instantiate(blastFxPrefab, center, Quaternion.AngleAxis(angleDeg, Vector3.forward));
                    fx.Play(blastLength, visualWidth);
                }
            }

            // 播放射击声音，随机选择一个音效
            int randomIndex = Random.Range(0, ShootSounds.Count);
            AudioKitManager.Instance.PlayOneShot(ShootSounds[randomIndex], volume: FireVolume);
            fireFlash.Flash(BulletSpawnPoint.position, shootDir); // 显示枪口火焰特效
            CameraUtils.ShakeMainCamera(0.15f, 7);
            WeaponAnimator.SetTrigger("SingleShoot");
            CreateShell(baseDirection);
            TriggerWeaponFired(); // 触发父类武器射击事件
        }

        private void CreateShell(Vector2 baseDirection)
        {
            if (ShellPool.instance == null) return;
            GameObject shellObj = ShellPool.instance.shellPool.Get();
            shellObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
            shellObj.GetComponent<ShellManager>().PlayShellAnimation(baseDirection, transform);
        }
    }
}
