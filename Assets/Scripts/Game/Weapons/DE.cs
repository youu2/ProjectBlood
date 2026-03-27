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

        public GunClip gunClip = new GunClip(8); // DE的弹夹，最大弹药量为8

        //换弹功能：
        // public int MaxAmmo = 8; // DE的最大弹药量
        // private int currentAmmo; // 当前弹药量

        private void OnGUI()
        {
            // 在屏幕上显示当前弹药量
            //IMGUIHelper.SetDesignResolution(640,320); // 设置IMGUI的设计分辨率，确保在不同分辨率下UI元素位置和大小的一致性
            GUI.skin.label.fontSize = 40; // 设置字体大小
            GUI.Label(new Rect(1650, 900, 400, 100), $"Ammo: {gunClip.currentAmmo}/{gunClip.maxAmmo}");
        }

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
            if (AttackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
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