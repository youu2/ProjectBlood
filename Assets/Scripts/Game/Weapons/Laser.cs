using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class Laser : AutomaticWeapon
	{
		[Header("=== 激光攻击设置 ===")]
		[Tooltip("激光实际攻击宽度")]
		public float HitDamage = 1f;
		public float laserWidth = 0.5f;
		public LineRenderer SelfLineRenderer;
		public SpriteRenderer LaserPoint;

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
				if (damageable != null)
				{
					// 计算击退方向：从玩家到敌人的方向
					Vector2 playerToEnemyDir = (damageHit.collider.transform.position - Player.player1.transform.position).normalized;

					// 根据子弹是否被强化计算伤害
					float damageMultiplier = IsBulletEnhanced ? 1.0f : 0.8f; // 未强化时伤害降低到80%
					float finalDamage = HitDamage * damageMultiplier;

					// 判断本击是否致命
					float enemyCurrentHP = damageable.CurrentHealth;
					float enemyMaxHP = damageable.MaxHealth;
					bool isLethal = enemyCurrentHP - finalDamage <= 0f;

					damageable.TakeDamage(finalDamage, playerToEnemyDir);

					// 吸血：致命一击时按敌人总生命值换算为 PB 道具
					if (isLethal && Lifesteal.Level > 0 && Player.player1 != null && enemyMaxHP > 0f)
					{
						float totalLifesteal = Lifesteal.GetLifestealAmount(enemyMaxHP);
						Global.GeneratePureBlood(damageHit.collider.gameObject, totalLifesteal);
					}
				}
			}
			CameraUtils.ShakeMainCamera(0.04f, 5);
		}

		public override void KeepAttacking(Vector2 shootDir)
		{
			shootDir = transform.right; // 确保激光方向与武器枪口朝向一致, 避免在另一把枪开火导致武器枪口上抬时切枪导致激光方向错误

			// 为了让打空弹夹后继续按住左键同时换弹后能够正确触发循环开火音效
			if (newClip && gunClip.CanShoot())
			{
				StartAttacking();
				newClip = false;
			}

			if (gunClip.CanShoot())
			{
				DrawLaser(shootDir);
			}

			if (attackInterval.CanAttack() && gunClip.CanShoot())
			{
				Attack(shootDir); // 激光攻击：使用 BoxCast 直接造成伤害
				attackInterval.RecordAttackTime();
				gunClip.Shoot();
				reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
				WeaponAnimator.SetBool("isLaserShooting", true);
			}
			else if (!gunClip.CanShoot() && !reloadTextShown)
			{
				// 没有弹药时停止射击声音
				StopAttacking();
				newClip = true;
				if (!gunClip.isReloading)
				{
					Player.DisplayText("[R] to Reload!");
					AudioKitManager.Instance.PlayOneShot("DryFireClick", volume: 0.7f);
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
			if (hasFired)
			{
				if (SelfLineRenderer != null)
				{
					SelfLineRenderer.enabled = false; // 禁用 LineRenderer
					SelfLineRenderer.SetPosition(0, Vector3.zero);
					SelfLineRenderer.SetPosition(1, Vector3.zero);
				}
			}
			base.StopAttacking();
			WeaponAnimator.SetBool("isLaserShooting", false);
		}

	}
}
