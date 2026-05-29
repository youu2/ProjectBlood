using UnityEngine;
using QFramework;
using System.Collections.Generic;
namespace ProjectBlood
{
	public partial class AK : WeaponBase
	{
		// public PlayerBullet Bullet;
		// public override float HitDamage => 0.5f;

		public AttackInterval AttackInterval = new AttackInterval(0.12f);

		// public float attackInterval = 0.2f; // 攻击间隔
		// private float lastAttackTime = 0f; // 上次攻击时间

		public List<AudioClip> ShootSounds = new List<AudioClip>();

		public AudioClip AKOneShotSound;
        // public AudioSource shootAudioSource;

		// public GunClip gunClip = new GunClip(30); // AK的弹夹，最大弹药量为30
		private bool newClip = true; // false表示新的弹夹还没开火过，true表示已经开火过
		private bool hasFired = false; // 标记是否真正开火过
		private bool reloadTextShown = false; // 标记是否已经显示过 reload 文本
		private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
        public override void Awake()
        {
			base.Awake();
			gunClip = new GunClip(30); // AK的弹夹，最大弹药量为30
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
				SelfAudioSource.clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				// gunClip.Shoot();会导致第一枪消耗两发弹药
				newClip = false;
				hasFired = true; // 标记已经开火过
			}
		}
		public override void KeepAttacking(Vector2 shootDir)
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
				reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
			}else if (!gunClip.CanShoot() && !reloadTextShown)
			{
				// 没有弹药时停止射击声音
				StopAttacking();
				newClip = true;
				// Reload();
				if(!gunClip.isReloading)
				{
					Player.DisplayText("[R] to Reload!");
					reloadTextShown = true; // 标记已经显示过 reload 文本
				}
			}
			TryPlayDryFireClick();
		}

		// 空挂音效,提示玩家换弹
		public void TryPlayDryFireClick()
		{
			if(Time.frameCount % 50 == 0 && AttackInterval.CanAttack() && !gunClip.isReloading)
			{
				SelfAudioSource.PlayOneShot(DryFireClick);
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
            base.Reload();
        }

		public override void SwitchFromSet()
		{
			if (gunClip == null)  // 检查是否需要初始化
			{
				gunClip = new GunClip(30);
			}
			AttackInterval.Reset();
			newClip = true;
			reloadTextShown = false; // 切换武器时重置 reload 文本显示标记
			StopAttacking();
			StopReload();  // 调用 WeaponBase 的方法，内部会处理 gunClip.CancelReload()
			Player.HideText(); // 切换武器时隐藏 reload 文本
		}

		public override void SwitchToSet()
		{
			if (gunClip == null)  // 检查是否需要初始化
			{
				gunClip = new GunClip(30);
			}
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
