using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;
using QFramework;

namespace ProjectBlood
{
    public partial class DE : IWeapon
    {
        // public PlayerBullet Bullet; QF架构bind功能生成的DEBullet替代，可在designer中直接绑定
        // public override float HitDamage => 0.5f;
        public AttackInterval AttackInterval = new AttackInterval(1.0f); // 攻击间隔，使用AttackInterval类来管理攻击间隔逻辑

        // 重构了攻击间隔功能，使用AttackInterval类来管理攻击间隔逻辑，避免在DE类中直接处理时间相关的逻辑，使代码更清晰和可维护
        // public float attackInterval = 0.5f; // 攻击间隔
        // private float lastAttackTime = 0f; // 上次攻击时间

        public List<AudioClip> ShootSounds = new List<AudioClip>();
        // public AudioSource shootAudioSource; 
        // 被QF架构other bind功能生成的SelfAudioSource替代，可在designer中直接绑定

        public override void Attack(Vector2 shootDir)
        {
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
            var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
            bullet.direction = shootDir;
            bullet.gameObject.SetActive(true);

            int randomIndex = Random.Range(0, ShootSounds.Count);
            SelfAudioSource.clip = ShootSounds[randomIndex];
            SelfAudioSource.Play();
        }
        public override void keepAttacking(Vector2 shootDir)
        {
            if (AttackInterval.CanAttack())
            {        
                Attack(shootDir);
                AttackInterval.RecordAttackTime();
            }
        }

        public override void StopAttacking(Vector2 shootDir)
        {
            // DE射速较慢，停止攻击时不需要额外逻辑
        }
    }
}