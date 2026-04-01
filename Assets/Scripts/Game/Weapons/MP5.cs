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

		public GunClip gunClip = new GunClip(30); // MP5的弹夹，最大弹药量为30
		private bool newClip = true;
		private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
		public void Start()
		{
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
			if(!gunClip.CanShoot()) return;
			Attack(shootDir);
			SelfShortAudioSource.PlayOneShot(MP5OneShot);
			SelfAudioSource.clip = ShootSounds[0];
			SelfAudioSource.loop = true;
			SelfAudioSource.Play();
			gunClip.Shoot();
			newClip = true;
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
			if (AttackInterval.CanAttack() && gunClip.CanShoot())
			{
				Attack(shootDir);
				AttackInterval.RecordAttackTime();
				gunClip.Shoot();
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
			
			// 为了避免在没有弹药时再次射击松开左键触发StopAttacking导致多余的音效播放，增加了判断条件
			if(SelfAudioSource.isPlaying)
			{
				// 停止攻击时的一些逻辑，比如停止播放射击声音等
				
				SelfShortAudioSource.PlayOneShot(MP5ShootEnd);
			}
			SelfAudioSource.Stop();
			SelfAudioSource.clip = null;
		}

		public override void Reload()
		{
			// 按R键换弹
			if (Input.GetKeyDown(KeyCode.R))
			{
				gunClip.Reload(); // 调用GunClip的reload方法进行换弹
			}
		}

        public override void SwitchFromSet()
        {
			// Debug.Log("MP5 Reset");
			AttackInterval.Reset();
			newClip = true;
			StopAttacking();
			gunClip.UpdateClipUI();
        }
		public override void SwitchToSet()
		{
			gunClip.UpdateClipUI();
			if (Input.GetMouseButton(0))
			{
				SelfAudioSource.clip = ShootSounds[0];
			}
		}
    }
}
