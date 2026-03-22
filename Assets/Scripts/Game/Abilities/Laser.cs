using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Laser : ProjectBlood.IWeapon
	{
		// public PlayerBullet Bullet;
		// public override float HitDamage => 0.5f;

		public float attackInterval = 0.2f; // 攻击间隔
		private float lastAttackTime = 0f; // 上次攻击时间

		public List<AudioClip> ShootSounds = new List<AudioClip>();

		public override void Attack(Vector2 shootDir)
		{
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
		}
		public override void StartAttacking(Vector2 shootDir)
		{
			Attack(shootDir);
			SelfShortAudioSource.PlayOneShot(LaserStart);
			SelfAudioSource .clip = ShootSounds[0];
			SelfAudioSource.loop = true;
			SelfAudioSource.Play();
		}
		public override void keepAttacking(Vector2 shootDir)
		{
			//Attack(shootDir);
			if (Time.time - lastAttackTime >= attackInterval)
			{
				Attack(shootDir);
				lastAttackTime = Time.time;
			}
			var targetLayer = LayerMask.GetMask("Enemy", "Wall"); // 所有可以阻挡激光的物体层级
			var hit = Physics2D.Raycast(Bullet.Position2D(), shootDir, Mathf.Infinity, targetLayer); // 获得碰到的第一个物体的位置
			SelfLineRenderer.SetPosition(0, Bullet.Position2D()); // 设置激光的起始点和结束点，没碰到就默认绘制100单位长度的激光
			SelfLineRenderer.SetPosition(1, hit.collider != null ? (Vector3)hit.point : (Bullet.Position2D() + shootDir * 100f));
			
		}

		public override void StopAttacking(Vector2 shootDir)
		{
			// 停止攻击时的一些逻辑，比如停止播放射击声音等
			SelfAudioSource .Stop();
			SelfShortAudioSource.PlayOneShot(LaserEnd);
			SelfLineRenderer.SetPosition(0, Vector3.zero);
			SelfLineRenderer.SetPosition(1, Vector3.zero);
		}
	}
}
