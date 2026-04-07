using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class ShotGun : IWeapon
	{
		// public override float HitDamage => 0.5f;

        public float attackInterval = 0.5f; // 攻击间隔
        private float lastAttackTime = 0f; // 上次攻击时间

		public AttackInterval AttackInterval = new AttackInterval(1.0f);

        public List<AudioClip> ShootSounds = new List<AudioClip>();
        // public AudioSource shootAudioSource; 
        // 被QF架构other bind功能生成的SelfAudioSource替代，可在designer中直接绑定

		[SerializeField] private float spreadAngle = 30f; // 圆锥散射角度，例如30度，可调节
		[SerializeField] private int bulletCount = 5; // 每次攻击生成的子弹数量

		public GunClip gunClip = new GunClip(6, null); // 喷子弹夹，最大弹药量为8
		private FireFlash fireFlash = new FireFlash(); // 枪口火焰特效组件
		public void Start()
		{
			gunClip = new GunClip(6, SelfAudioSource); // 喷子弹夹，最大弹药量为8
			gunClip.UpdateClipUI();
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

        public override void Attack(Vector2 shootDir)
		{
			// 基准方向（准心瞄准方向）
			Vector2 baseDirection = shootDir.normalized;

			// 生成5发子弹
			for (int i = 0; i < bulletCount; i++)
			{
				// 在圆锥范围内随机生成一个偏转角度 [-spreadAngle/2, spreadAngle/2]
				float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);

				// 构造一个旋转，围绕Z轴旋转randomAngle度
				Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);

				// 将基准方向转换为方向向量，并应用旋转，得到最终发射方向
				Vector2 finalDirection = randomRotation * baseDirection;

				// 计算飞行方向的角度，并生成对应的旋转
				float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
				Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);

				// 生成子弹，并设置其飞行方向
				var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
				bullet.direction = finalDirection;
				bullet.gameObject.SetActive(true);

				// 播放射击声音，随机选择一个音效
				int randomIndex = Random.Range(0, ShootSounds.Count);
                SelfAudioSource.clip = ShootSounds[randomIndex];
                SelfAudioSource.Play();
				
			}
			fireFlash.Flash(DEBullet.transform.position, shootDir); // 显示枪口火焰特效
			
			// 标记最近开火过
			recentlyFired = true;
			lastFireTime = Time.time;
		}
        public override void keepAttacking(Vector2 shootDir)
        {
            if (AttackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
            {   
                Attack(shootDir);
                AttackInterval.RecordAttackTime();
				gunClip.Shoot(); // 射击时减少弹药量
            }else if(!gunClip.CanShoot()){
				Reload();
			}
        }

        public override void StopAttacking()
        {
			// 喷子射速较慢(音频是单段射击，时长很短，可以播放完全)，停止攻击时不需要额外逻辑
        }

		public override void SwitchFromSet()
		{
			gunClip.StopReload(this); // 切出武器时停止换弹流程
			gunClip.isReloading = false; // 切出武器时重置换弹状态，确保下次切回时可以正常换弹
			recentlyFired = false; // 切出武器时重置开火标志
		}

		public override void SwitchToSet()
		{
			gunClip.UpdateClipUI();
			Sprite.enabled = true; // 重新启用sprite
		}
		
		public override AudioClip GetCurrentlyPlayingSound()
		{
			return SelfAudioSource.isPlaying ? SelfAudioSource.clip : null;
		}
		
		public override void HideSprite()
		{
			if (Sprite != null)
			{
				Sprite.enabled = false;
			}
		}
	}
}
