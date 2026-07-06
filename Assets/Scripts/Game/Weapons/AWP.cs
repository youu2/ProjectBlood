using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
    public partial class AWP : SemiAutomaticWeapon
    {
        public List<AudioClip> AWPShootSounds = new List<AudioClip>();
        public override void Awake()
        {
            BloodRequired = 5;
            ReloadTime = 2.4f;
            MaxAmmo = 10;
            gunClip = new GunClip(MaxAmmo);
            attackInterval = new AttackIntervalFeature(1.6f);
            base.Awake();
        }

        public override void Attack(Vector2 shootDir)
        {
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
            var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
            bullet.direction = shootDir;
            bullet.gameObject.SetActive(true);
            ApplyLifestealToBullet(bullet);

            int randomIndex = Random.Range(0, ShootSounds.Count);
            AudioKitManager.Instance.PlayOneShot(ShootSounds[randomIndex], volume: 0.6f);
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效

            // 镜头震动, 触发单射击动画
            CameraUtils.ShakeMainCamera(0.15f, 7);
            WeaponAnimator.SetTrigger("SingleShoot");
        }
    }
}