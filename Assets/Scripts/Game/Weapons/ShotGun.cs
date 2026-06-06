using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class ShotGun : SemiAutomaticWeapon
	{
        public List<AudioClip> ShotGunSounds = new List<AudioClip>();
		protected override List<AudioClip> ShootSounds => ShotGunSounds;
		[SerializeField] private float spreadAngle = 30f; // 圆锥散射角度，例如30度，可调节
		[SerializeField] private int bulletCount = 5; // 每次攻击生成的子弹数量
		private FireFlash fireFlash = new FireFlash(); // 枪口火焰特效组件

		public override void Awake()
		{
			BloodRequired = 5;
            ReloadTime = 2.1f;
            MaxAmmo = 6;
			gunClip = new GunClip(MaxAmmo);
			attackInterval = new AttackInterval(1.0f);
			base.Awake();
		}
        public override void Attack(Vector2 shootDir)
		{
			// 基准方向（准心瞄准方向）
			Vector2 baseDirection = shootDir.normalized;

			// 生成5发子弹
			for (int i = 0; i < bulletCount; i++)
			{
				// 在圆锥范围内随机生成一个偏转角度 [-spreadAngle/2, spreadAngle/2]
				float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);

				// 构造一个旋转，围绕Z轴旋转randomAngle度
				Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);

				// 将基准方向转换为方向向量，并应用旋转，得到最终发射方向
				Vector2 finalDirection = randomRotation * baseDirection;

				// 计算飞行方向的角度，并生成对应的旋转
				float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
				Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);

				// 生成子弹，并设置其飞行方向
				var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
				bullet.direction = finalDirection;
				bullet.gameObject.SetActive(true);

				// 播放射击声音，随机选择一个音效
				int randomIndex = Random.Range(0, ShootSounds.Count);
				AudioKitManager.Instance.PlayOneShot(ShootSounds[randomIndex]);
			}
			fireFlash.Flash(DEBullet.transform.position, shootDir); // 显示枪口火焰特效
			CameraUtils.ShakeMainCamera(0.15f, 7);
		}
	}
}
