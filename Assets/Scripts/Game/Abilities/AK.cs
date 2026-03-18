using UnityEngine;
using QFramework;
using System.Collections.Generic;
namespace ProjectBlood
{
	public partial class AK : ProjectBlood.IWeapon
	{
		// public PlayerBullet Bullet;
		public override float HitDamage => 0.5f;

		public float attackInterval = 0.2f; // 攻击间隔
		private float lastAttackTime = 0f; // 上次攻击时间

		public List<AudioClip> ShootSounds = new List<AudioClip>();

		public AudioClip AKOneShotSound;
		// public AudioSource shootAudioSource;

		public override void Attack(Vector2 shootDir)
		{
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(AKBullet, AKBullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
		}
		public override void StartAttacking(Vector2 shootDir)
		{
			// 第一次按下鼠标时播放一次单发音效，继续按住也能播放循环持续开火音效
			Attack(shootDir);
			SelfShortAudioSource.PlayOneShot(AKOneShotSound);
			SelfAudioSource .clip = ShootSounds[0];
			SelfAudioSource.loop = true;
			SelfAudioSource.Play();
		}
		public override void keepAttacking(Vector2 shootDir)
		{
			if (Time.time - lastAttackTime >= attackInterval)
			{
				Attack(shootDir);
				lastAttackTime = Time.time;
			}
		}

		public override void StopAttacking(Vector2 shootDir)
		{
			// 停止攻击时的一些逻辑
			SelfAudioSource .Stop();
			SelfShortAudioSource.PlayOneShot(AKShootEnd);
		}
	}
}
