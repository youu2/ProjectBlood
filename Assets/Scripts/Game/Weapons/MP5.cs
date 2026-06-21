using UnityEngine;
using System.Collections.Generic;
namespace ProjectBlood
{
    public partial class MP5 : AutomaticWeapon
    {
        public List<AudioClip> MP5ShootSounds = new();
        protected override AudioClip OneShotSound => MP5OneShotSound;
        protected override List<AudioClip> ShootSounds => MP5ShootSounds;
        protected override AudioClip ShootEndSound => MP5ShootEnd;
        private readonly FireFlash fireFlash = new(); // 枪口火焰特效组件
        public override void Awake()
        {
            BloodRequired = 5;
            ReloadTime = 1.5f;
            MaxAmmo = 30;
            gunClip = new GunClip(MaxAmmo);
            attackInterval = new AttackInterval(0.08f);
            base.Awake();
        }

        public override void Attack(Vector2 shootDir)
        {
            Vector2 finalDirection = ApplySpread(shootDir);
            // 计算旋转：根据 finalDirection 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, finalDirection);
            PlayerBullet bullet = Instantiate(MP5Bullet, MP5Bullet.transform.position, bulletRotation);
            bullet.direction = finalDirection;
            bullet.gameObject.SetActive(true);
            ApplyLifestealToBullet(bullet);
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
            CameraUtils.ShakeMainCamera(0.1f, 7);
        }

    }
}
