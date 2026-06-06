using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Laser : AutomaticWeapon
	{
		public float HitDamage = 12f;

		[Header("=== 激光攻击设置 ===")]
		[Tooltip("激光实际攻击宽度")]
		public float laserWidth = 0.5f;
		public List<AudioClip> LaserShootSounds = new List<AudioClip>();
		// public GunClip gunClip = new GunClip(120); // 激光的弹夹，最大弹药量为120
		protected override AudioClip OneShotSound => LaserStart;
		protected override List<AudioClip> ShootSounds => LaserShootSounds;
        protected override AudioClip ShootEndSound => LaserEnd;
		public override void Awake()
		{
			BloodRequired = 8;
            ReloadTime = 2.8f;
			MaxAmmo = 120;
			gunClip = new GunClip(MaxAmmo);
			attackInterval = new AttackInterval(0.2f);
			base.Awake();
		}

		public override void Attack(Vector2 shootDir)
		{
			// 真正的激光逻辑：使用 BoxCast 或多条 Raycast 检测第一个敌人，直接造成伤害
			if (!gunClip.CanShoot()) return;

			Vector2 startPos = LaserPoint.Position2D();
			var targetLayer = LayerMask.GetMask("Enemy", "Wall"); // 所有可以阻挡激光的物体层级
			
			//  伤害检测：使用 BoxCast（有宽度）
			var damageHit = Physics2D.BoxCast(startPos, new Vector2(laserWidth, laserWidth), 0f, shootDir, Mathf.Infinity, targetLayer);

			// 如果击中了敌人，造成伤害
			if (damageHit.collider != null)
			{
				var damageable = damageHit.collider.GetComponent<IDamageable>();
				if (damageable != null && !damageable.IsDying)
				{
					// 计算击退方向：从玩家到敌人的方向
					Vector2 playerToEnemyDir = (damageHit.collider.transform.position - Player.player1.transform.position).normalized;
					damageable.TakeDamage(HitDamage, playerToEnemyDir);
				}
			}
			CameraUtils.ShakeMainCamera(0.06f, 5);
		}
		
		public override void KeepAttacking(Vector2 shootDir)
		{
			// 为了让打空弹夹后继续按住左键同时换弹后能够正确触发循环开火音效
			if (newClip && gunClip.CanShoot())
			{
				StartAttacking(shootDir);
				newClip = false;
			}

			if (gunClip.CanShoot()){
				DrawLaser(shootDir);
			}

			if (attackInterval.CanAttack() && gunClip.CanShoot())
			{
				Attack(shootDir); // 激光攻击：使用 BoxCast 直接造成伤害
				attackInterval.RecordAttackTime();
				gunClip.Shoot();
				reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
			}else if (!gunClip.CanShoot() && !reloadTextShown)
			{
				// 没有弹药时停止射击声音
				StopAttacking();
				newClip = true;
				if(!gunClip.isReloading)
				{
					Player.DisplayText("[R] to Reload!");
                    AudioKitManager.Instance.PlayOneShot("DryFireClick");
					reloadTextShown = true; // 标记已经显示过 reload 文本
				}
			}
			TryPlayDryFireClick();
		}

		private void DrawLaser(Vector2 shootDir)
		{
			Vector2 startPos = LaserPoint.Position2D();
			var targetLayer = LayerMask.GetMask("Enemy", "Wall");
			var renderHit = Physics2D.Raycast(startPos, shootDir, Mathf.Infinity, targetLayer);

			if (SelfLineRenderer != null)
			{
				SelfLineRenderer.enabled = true; // 启用 LineRenderer
				SelfLineRenderer.SetPosition(0, startPos);
				SelfLineRenderer.SetPosition(1, renderHit.collider != null ? (Vector3)renderHit.point : (startPos + shootDir * 100f));
			}
		}

		public override void StopAttacking()
		{
			// 只有在真正开火过的情况下才播放结束音效
			// hasFired 为 true 表示已经开火过
			if(hasFired)
			{
				if (SelfLineRenderer != null)
				{
					SelfLineRenderer.enabled = false; // 禁用 LineRenderer
					SelfLineRenderer.SetPosition(0, Vector3.zero);
					SelfLineRenderer.SetPosition(1, Vector3.zero);
				}
			}
			base.StopAttacking();
		}

	}
}
