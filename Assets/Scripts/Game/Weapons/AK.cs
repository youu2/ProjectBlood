using UnityEngine;
using QFramework;
using System.Collections.Generic;
namespace ProjectBlood
{
	public partial class AK : ProjectBlood.IWeapon
	{
		// public PlayerBullet Bullet;
		// public override float HitDamage => 0.5f;

		public AttackInterval AttackInterval = new AttackInterval(0.12f);

		// public float attackInterval = 0.2f; // 攻击间隔
		// private float lastAttackTime = 0f; // 上次攻击时间

		public List<AudioClip> ShootSounds = new List<AudioClip>();

		public AudioClip AKOneShotSound;
        // public AudioSource shootAudioSource;

		public GunClip gunClip = new GunClip(30); // AK的弹夹，最大弹药量为30
		private bool newClip = true;
        public void Start()
        {
			gunClip.UpdateClipUI();
        }

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
			if (gunClip.CanShoot())
			{
				// 第一次按下鼠标时播放一次单发音效，继续按住也能播放循环持续开火音效
				Attack(shootDir);
				SelfShortAudioSource.PlayOneShot(AKOneShotSound);
				SelfAudioSource .clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				gunClip.Shoot();
				newClip = true;
			}
			
		}
		public override void keepAttacking(Vector2 shootDir)
		{
			// 为了让打空弹夹后继续按住左键同时换弹后 ->
			// 能够正确触发循环开火音效
			if (!newClip)
			{
				StartAttacking(shootDir);
				newClip = true;
			}
			if (AttackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
			{
				Attack(shootDir);
				AttackInterval.RecordAttackTime();
				gunClip.Shoot(); // 射击时减少弹药量
			}else if (!gunClip.CanShoot())
			{
				// 没有弹药时停止射击声音
				StopAttacking();
				newClip = false;
				return;
			}	
		}

		public override void StopAttacking()
		{
			// 为了避免在没有弹药时松开左键触发StopAttacking导致多余的音效播放，增加了判断条件
			if(SelfAudioSource.isPlaying)
			{
				// 停止攻击时的一些逻辑
				SelfAudioSource.Stop();
				SelfShortAudioSource.PlayOneShot(AKShootEnd);
			}
		}

		public override void Reload()
        {
            // 按R键换弹
            if (Input.GetKeyDown(KeyCode.R))
            {
                gunClip.Reload(); // 调用GunClip的reload方法进行换弹
            }
        }
	}
}
