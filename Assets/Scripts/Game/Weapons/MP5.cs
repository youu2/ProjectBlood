using UnityEngine;
using QFramework;
using System.Collections.Generic;
using Unity.VisualScripting;
namespace ProjectBlood
{
	public partial class MP5 : IWeapon
	{
		public PlayerBullet Bullet;
		// public override float HitDamage => 0.5f;

		public AttackInterval AttackInterval = new AttackInterval(0.08f);

		// public float attackInterval = 0.2f; // 攻击间隔
		// private float lastAttackTime = 0f; // 上次攻击时间

		public List<AudioClip> ShootSounds = new List<AudioClip>();
		// public AudioClip MP5OneShotSound; other bind取代
		// public AudioSource shootAudioSource;

		public GunClip gunClip = new GunClip(30, null); // MP5的弹夹，最大弹药量为30; // MP5的弹夹，最大弹药量为30
		private bool newClip = true; // false表示新的弹夹还没开火过，true表示已经开火过
		private bool hasFired = false; // 标记是否真正开火过
		private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
		public void Start()
		{
			gunClip = new GunClip(30, SelfShortAudioSource); // MP5的弹夹，最大弹药量为30
			gunClip.UpdateClipUI();
		}

		public override void Attack(Vector2 shootDir)
		{
			// 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
			Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
			var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
			bullet.direction = shootDir;
			bullet.gameObject.SetActive(true);
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
		}
		public override void StartAttacking(Vector2 shootDir)
		{
			if(gunClip.CanShoot()){
				// Attack(shootDir);
				SelfShortAudioSource.PlayOneShot(MP5OneShot);
				SelfAudioSource.clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				// gunClip.Shoot();
				newClip = false;
				hasFired = true; // 标记已经开火过
			}else{
				Reload();
			}
		}
		public override void keepAttacking(Vector2 shootDir)
		{
			// 为了让打空弹夹后继续按住左键同时换弹后 ->
			// 能够正确触发循环开火音效
			if (newClip)
			{
				StartAttacking(shootDir);
				newClip = false;
			}
			if (AttackInterval.CanAttack() && gunClip.CanShoot())
			{
				Attack(shootDir);
				AttackInterval.RecordAttackTime();
				gunClip.Shoot();
			}else if (!gunClip.CanShoot())
			{
				// 没有弹药时停止射击声音
				StopAttacking();
				newClip = true;
				Reload();
			}			
		}

		public override void StopAttacking()
		{
			// 只有在真正开火过的情况下才播放结束音效
			// hasFired 为 true 表示已经开火过
			if(SelfAudioSource.isPlaying && hasFired)
			{				
				SelfShortAudioSource.PlayOneShot(MP5ShootEnd);
			}
			SelfAudioSource.Stop();
			SelfAudioSource.clip = null;
			hasFired = false; // 重置开火标记
		}

		public override void Reload(System.Action onReloadComplete = null)
		{
			gunClip.Reload(reloadSound, this, () => 
			{
				// 换弹完成后消耗血液
				if (BloodBank != null && BloodBank.CurrentBloodAmount >= BloodRequired)
				{
					BloodBank.RemoveBlood(BloodRequired);
				}
				// 调用外部传入的回调
				onReloadComplete?.Invoke();
			}); // 调用GunClip的reload方法进行换弹	
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
			return MP5ShootEnd;
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
