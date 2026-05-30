using UnityEngine;
using QFramework;
using System.Collections.Generic;
using System.Buffers.Text;
namespace ProjectBlood
{
	public partial class AK : AutomaticWeapon
	{
		public List<AudioClip> AkShootSounds = new List<AudioClip>();
        protected override AudioClip OneShotSound => AKOneShotSound;
		protected override List<AudioClip> ShootSounds => AkShootSounds;
        protected override AudioClip ShootEndSound => AKShootEnd;
		private FireFlash fireFlash = new FireFlash(); // 枪口火焰特效组件
        public override void Awake()
        {
			MaxAmmo = 30;
			gunClip = new GunClip(MaxAmmo);
			attackInterval = new AttackInterval(0.12f);
			base.Awake();
        }
        public override void Attack(Vector2 shootDir)
		{
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(AKBullet, AKBullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
			fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
		}
		
	}
}
