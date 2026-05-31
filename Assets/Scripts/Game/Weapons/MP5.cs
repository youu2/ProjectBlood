using UnityEngine;
using QFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
namespace ProjectBlood
{
	public partial class MP5 : AutomaticWeapon
	{
		public List<AudioClip> MP5ShootSounds = new List<AudioClip>();
		protected override AudioClip OneShotSound => MP5OneShotSound;
		protected override List<AudioClip> ShootSounds => MP5ShootSounds;
		protected override AudioClip ShootEndSound => MP5ShootEnd;
		private FireFlash fireFlash = new FireFlash(); // 枪口火焰特效组件
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
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(MP5Bullet, MP5Bullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
		}
		
	}
}
