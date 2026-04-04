using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Laser : ProjectBlood.IWeapon
	{
		// public PlayerBullet Bullet;
		// public override float HitDamage => 0.5f;

		// public float attackInterval = 0.2f; // 攻击间隔
		// private float lastAttackTime = 0f; // 上次攻击时间
		public AttackInterval AttackInterval = new AttackInterval(0.02f);

		public List<AudioClip> ShootSounds = new List<AudioClip>();
		public GunClip gunClip = new GunClip(500, null); // 激光的弹夹，最大弹药量为500
		private bool newClip = true; // false表示新的弹夹还没开火过，true表示已经开火过
		private bool hasFired = false; // 标记是否真正开火过
		public void Start()
		{
			gunClip = new GunClip(500, SelfShortAudioSource); // 激光的弹夹，最大弹药量为500
			gunClip.UpdateClipUI();
		}

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
			if (gunClip.CanShoot())
			{
				// Attack(shootDir);
				SelfShortAudioSource.PlayOneShot(LaserStart);
				SelfAudioSource .clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				// gunClip.Shoot();
				newClip = true;
				hasFired = true; // 标记已经开火过
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
				return;
			}

			// 激光特殊逻辑：发射时持续检测激光碰到的第一个物体，并将激光绘制到碰撞点位置
			var targetLayer = LayerMask.GetMask("Enemy", "Wall"); // 所有可以阻挡激光的物体层级
			var hit = Physics2D.Raycast(Bullet.Position2D(), shootDir, Mathf.Infinity, targetLayer); // 获得碰到的第一个物体的位置
			SelfLineRenderer.SetPosition(0, Bullet.Position2D()); // 设置激光的起始点和结束点，没碰到就默认绘制100单位长度的激光
			SelfLineRenderer.SetPosition(1, hit.collider != null ? (Vector3)hit.point : (Bullet.Position2D() + shootDir * 100f));
			
		}

		public override void StopAttacking()
		{
			// 只有在真正开火过的情况下才播放结束音效
			// hasFired 为 true 表示已经开火过
			if(SelfAudioSource.isPlaying && hasFired)
			{
				SelfShortAudioSource.PlayOneShot(LaserEnd);
				SelfLineRenderer.SetPosition(0, Vector3.zero);
				SelfLineRenderer.SetPosition(1, Vector3.zero);
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
			gunClip.isReloading = false; // 切出武器时重置换弹状态，确保下次切回时可以正常换弹
        }

		public override void SwitchToSet()
		{
			gunClip.UpdateClipUI();
			if (Input.GetMouseButton(0))
			{
				SelfAudioSource.clip = ShootSounds[0];
			}
		}
		
		public override AudioClip GetShootEndSound()
		{
			return LaserEnd;
		}
	}
}
