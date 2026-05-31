using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;

namespace ProjectBlood
{
    public partial class DE : SemiAutomaticWeapon
    {
        public List<AudioClip> DEShootSounds = new List<AudioClip>();
        protected override List<AudioClip> ShootSounds => DEShootSounds;
        private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
        public override void Awake()
        {
            BloodRequired = 3;
            ReloadTime = 2.0f;
            MaxAmmo = 8;
			gunClip = new GunClip(MaxAmmo);
			attackInterval = new AttackInterval(1.0f);
            base.Awake();
        }

        public override void Attack(Vector2 shootDir)
        {
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
            var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
            bullet.direction = shootDir;
            bullet.gameObject.SetActive(true);

            int randomIndex = Random.Range(0, ShootSounds.Count);
            AudioManager.PlayOneShot(ShootSounds[randomIndex]);
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
        }
    }
}