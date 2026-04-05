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

		public GunClip gunClip = new GunClip(30, null); // AK的弹夹，最大弹药量为30
		private bool newClip = true; // false表示新的弹夹还没开火过，true表示已经开火过
		private bool hasFired = false; // 标记是否真正开火过
		private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
        public void Start()
        {
			gunClip = new GunClip(30, SelfShortAudioSource); // AK的弹夹，最大弹药量为30
			gunClip.UpdateClipUI();
			
        }
        // public void Update()
        // {
        //     if(!gunClip.CanShoot())
		// 	{
		// 		// 用于防止player在没有子弹的时候进入keep Attacking
		// 		canShoot = false;
		// 	}else
		// 	{
		// 		canShoot = true;
		// 	}
        // } 
        public override void Attack(Vector2 shootDir)
		{
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(AKBullet, AKBullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
			fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
		}
		public override void StartAttacking(Vector2 shootDir)
		{
			if (gunClip.CanShoot())
			{
				// 第一次按下鼠标时播放一次单发音效，继续按住也能播放循环持续开火音效
				// Attack(shootDir);
				SelfShortAudioSource.PlayOneShot(AKOneShotSound);
				SelfAudioSource .clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				// gunClip.Shoot();会导致第一枪消耗两发弹药
				newClip = false;
				hasFired = true; // 标记已经开火过
			}
			
		}
		public override void keepAttacking(Vector2 shootDir)
		{
			// 为了让打空弹夹后继续按住左键同时换弹后, 能够正确触发循环开火音效
			if (newClip && gunClip.CanShoot())
			{
				StartAttacking(shootDir);
				newClip = false;
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
				newClip = true;
				return;
			}	
		}

		public override void StopAttacking()
		{
			// 只有在真正开火过的情况下才播放结束音效
			// hasFired 为 true 表示已经开火过
			if(SelfAudioSource.isPlaying && hasFired)
			{
				SelfShortAudioSource.PlayOneShot(AKShootEnd);
			}
			SelfAudioSource.Stop();
			SelfAudioSource.clip = null;
			hasFired = false; // 重置开火标记
		}

		public override void Reload()
        {
            // 按R键换弹
            if (Input.GetKeyDown(KeyCode.R))
            {
                gunClip.Reload(reloadSound, this); // 调用GunClip的reload方法进行换弹
            }
        }

		public override void SwitchFromSet()
		{
			// Debug.Log("MP5 Reset");
			AttackInterval.Reset();
			newClip = true;
			StopAttacking();
			gunClip.StopReload(this); // 切出武器时停止换弹流程
			gunClip.isReloading = false; // 切出武器时重置换弹状态，确保下次切回时可以正常换弹
		}

		public override void SwitchToSet()
		{
			gunClip.UpdateClipUI();
			Sprite.enabled = true; // 重新启用sprite
			if (Input.GetMouseButton(0))
			{
				SelfAudioSource.clip = ShootSounds[0];
			}
		}
		
		public override AudioClip GetShootEndSound()
		{
			return AKShootEnd;
		}
		
		public override bool HasFired()
		{
			return hasFired;
		}
		
		public override bool IsPlayingShootEnd()
		{
			return SelfShortAudioSource.isPlaying;
		}
		
		public override void HideSprite()
		{
			Sprite.enabled = false;
		}
	}
}
