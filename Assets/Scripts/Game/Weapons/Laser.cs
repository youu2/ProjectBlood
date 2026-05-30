using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Laser : WeaponBase
	{
		// public PlayerBullet Bullet;
		public float HitDamage = 0.5f;

		// public float attackInterval = 0.2f; // 攻击间隔
		// private float lastAttackTime = 0f; // 上次攻击时间
		public AttackInterval AttackInterval = new AttackInterval(0.2f);

		[Header("=== 激光攻击设置 ===")]
		[Tooltip("激光实际攻击宽度")]
		public float laserWidth = 0.5f;
		public List<AudioClip> ShootSounds = new List<AudioClip>();
		// public GunClip gunClip = new GunClip(120); // 激光的弹夹，最大弹药量为120
		private bool newClip = true; // false表示新的弹夹还没开火过，true表示已经开火过
		private bool hasFired = false; // 标记是否真正开火过
		private bool reloadTextShown = false; // 标记是否已经显示过 reload 文本
		public override void Awake()
		{
			base.Awake();
			gunClip = new GunClip(120); // 激光的弹夹，最大弹药量为120
			gunClip.UpdateClipUI();
		}

		public override void Attack(Vector2 shootDir)
		{
			// 真正的激光逻辑：使用 BoxCast 或多条 Raycast 检测第一个敌人，直接造成伤害
			if (!gunClip.CanShoot()) return;

			Vector2 startPos = Bullet.Position2D();
			var targetLayer = LayerMask.GetMask("Enemy", "Wall"); // 所有可以阻挡激光的物体层级
			
			// ========== 伤害检测：使用 BoxCast（有宽度） ==========
			var damageHit = Physics2D.BoxCast(startPos, new Vector2(laserWidth, laserWidth), 0f, shootDir, Mathf.Infinity, targetLayer);
			
			// ========== 激光绘制：使用 Raycast（严格沿瞄准方向） ==========
			var renderHit = Physics2D.Raycast(startPos, shootDir, Mathf.Infinity, targetLayer);
			
			// 更新 LineRenderer 显示 - 严格沿瞄准方向
			if (SelfLineRenderer != null)
			{
				SelfLineRenderer.SetPosition(0, startPos); // 设置激光的起始点
				SelfLineRenderer.SetPosition(1, renderHit.collider != null ? (Vector3)renderHit.point : (startPos + shootDir * 100f)); // 结束点沿瞄准方向
			}

			// 如果击中了敌人，造成伤害
			if (damageHit.collider != null)
			{
				var damageable = damageHit.collider.GetComponent<IDamageable>();
				if (damageable != null && !damageable.IsDying)
				{
					damageable.TakeDamage(HitDamage); // 直接造成伤害，不需要子弹
				}
			}
		}
		public override void StartAttacking(Vector2 shootDir)
		{
			if (gunClip.CanShoot())
			{
				// Attack(shootDir); // 激光不需要每帧都生成子弹
				SelfShortAudioSource.PlayOneShot(LaserStart);
				SelfAudioSource.clip = ShootSounds[0];
				SelfAudioSource.loop = true;
				SelfAudioSource.Play();
				// gunClip.Shoot();
				newClip = false;
				hasFired = true; // 标记已经开火过
			}else{
				// Reload();
			}
		}
		
		public override void KeepAttacking(Vector2 shootDir)
		{
			// 为了让打空弹夹后继续按住左键同时换弹后 ->
			// 能够正确触发循环开火音效
			if (newClip && gunClip.CanShoot())
			{
				StartAttacking(shootDir);
				newClip = false;
			}

			if (gunClip.CanShoot()){
				// 激光特殊逻辑：发射时持续检测激光碰到的第一个物体，并将激光绘制到碰撞点位置
				Vector2 startPos = Bullet.Position2D();
				var targetLayer = LayerMask.GetMask("Enemy", "Wall"); // 所有可以阻挡激光的物体层级
				// ========== 激光绘制：使用 Raycast（严格沿瞄准方向） ==========
				var renderHit = Physics2D.Raycast(startPos, shootDir, Mathf.Infinity, targetLayer);
				
				// 绘制激光 - 严格沿瞄准方向
				if (SelfLineRenderer != null)
				{
					SelfLineRenderer.SetPosition(0, startPos); // 设置激光的起始点和结束点，没碰到就默认绘制100单位长度的激光
					SelfLineRenderer.SetPosition(1, renderHit.collider != null ? (Vector3)renderHit.point : (startPos + shootDir * 100f));
				}
			}

			if (AttackInterval.CanAttack() && gunClip.CanShoot())
			{
				Attack(shootDir); // 激光攻击：使用 BoxCast 直接造成伤害
				AttackInterval.RecordAttackTime();
				gunClip.Shoot();
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
                    SelfAudioSource.PlayOneShot(DryFireClick);
					reloadTextShown = true; // 标记已经显示过 reload 文本
				}
			}
			TryPlayDryFireClick();
		}

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
            base.Reload();
        }
		
		// public override void Reload(System.Action onReloadComplete = null)
        // {
		// 	gunClip.Reload(reloadSound, this, () => 
		// 	{
		// 		// 换弹完成后消耗血液
		// 		if (BloodBank != null && BloodBank.CurrentBloodAmount >= BloodRequired)
		// 		{
		// 			BloodBank.RemoveBlood(BloodRequired);
		// 		}
		// 		// 调用外部传入的回调
		// 		onReloadComplete?.Invoke();
		// 	}); // 调用GunClip的reload方法进行换弹
        // }

		public override void SwitchFromSet()
		{
			// Debug.Log("MP5 Reset");
			AttackInterval.Reset();
			newClip = true;
			StopAttacking();
			reloadTextShown = false; // 切出武器时重置 reload 文本显示标记
			StopReload();  // 调用 WeaponBase 的方法，内部会处理 gunClip.CancelReload()
			Player.HideText(); // 切换武器时隐藏 reload 文本
		}

		public override void SwitchToSet()
		{
			if (gunClip == null)  // 检查是否需要初始化
			{
				gunClip = new GunClip(120);
			}
			gunClip.UpdateClipUI();
			Sprite.enabled = true; // 重新启用sprite
			if (Input.GetMouseButton(0))
			{
				SelfAudioSource.clip = ShootSounds[0];
			}
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
