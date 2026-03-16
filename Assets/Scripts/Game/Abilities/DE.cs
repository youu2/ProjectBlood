using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;
using QFramework;

namespace ProjectBlood
{
    public partial class DE : IWeapon
    {
        // public PlayerBullet Bullet; QF架构bind功能生成的DEBullet替代，可在designer中直接绑定
        public override float HitDamage => 0.5f;

        public float attackInterval = 0.5f; // 攻击间隔
        private float lastAttackTime = 0f; // 上次攻击时间

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
        }
        public override void keepAttacking(Vector2 shootDir)
        {
            if (Time.time - lastAttackTime >= attackInterval)
            {
                int randomIndex = Random.Range(0, ShootSounds.Count);
                SelfAudioSource.clip = ShootSounds[randomIndex];
                SelfAudioSource.Play();
                
                Attack(shootDir);
                lastAttackTime = Time.time;
            }
        }

        public override void StopAttacking(Vector2 shootDir)
        {
            // 停止攻击时的一些逻辑，比如停止播放射击声音等
            SelfAudioSource.Stop();
        }
    }
}