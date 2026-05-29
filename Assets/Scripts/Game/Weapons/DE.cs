using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;

namespace ProjectBlood
{
    public partial class DE : WeaponBase
    {
        // public PlayerBullet Bullet; QF架构bind功能生成的DEBullet替代，可在designer中直接绑定
        // public override float HitDamage => 0.5f;
        public AttackInterval AttackInterval = new AttackInterval(1.0f); // 攻击间隔，使用AttackInterval类来管理攻击间隔逻辑

        // 重构了攻击间隔功能，使用AttackInterval类来管理攻击间隔逻辑，避免在DE类中直接处理时间相关的逻辑，使代码更清晰和可维护
        // public float attackInterval = 0.5f; // 攻击间隔
        // private float lastAttackTime = 0f; // 上次攻击时间

        public List<AudioClip> ShootSounds = new List<AudioClip>();
        // public AudioSource shootAudioSource; 
        // 被QF架构other bind功能生成的SelfAudioSource替代，可在designer中直接绑定
        // private GunClip gunClip = new GunClip(8); // DE的弹夹，最大弹药量为8
        private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
        public override int BloodRequired { get; } = 1; // 每次换弹需要的血量
        private bool reloadTextShown = false; // 标记是否已经显示过 reload 文本，防止文本闪烁

        //换弹功能：
        // public int MaxAmmo = 8; // DE的最大弹药量
        // private int currentAmmo; // 当前弹药量
        public override void Awake()
        {
            base.Awake();
            gunClip = new GunClip(8); // DE的弹夹，最大弹药量为8
            gunClip.UpdateClipUI(); // 初始化UI显示的弹药信息
        }

        public override void Reload()
        {
            base.Reload();
        }

        public override void Attack(Vector2 shootDir)
        {
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
            var bullet = Instantiate(DEBullet, DEBullet.transform.position, bulletRotation);
            bullet.direction = shootDir;
            bullet.gameObject.SetActive(true);

            int randomIndex = Random.Range(0, ShootSounds.Count);
            SelfAudioSource.clip = ShootSounds[randomIndex];
            SelfAudioSource.Play();
            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效
            
            // 标记最近开火过
            recentlyFired = true;
            lastFireTime = Time.time;
        }
        public override void KeepAttacking(Vector2 shootDir)
        {
            if (AttackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
            {        
                Attack(shootDir);
                AttackInterval.RecordAttackTime();
                gunClip.Shoot(); // 射击时减少弹药量
                reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
            }else if(!gunClip.CanShoot() && !reloadTextShown){
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
            // DE射速较慢，停止攻击时不需要额外逻辑
        }

        public override void SwitchFromSet()
        {
            StopReload();  // 调用 WeaponBase 的方法，内部会处理 gunClip.CancelReload()
            recentlyFired = false; // 切出武器时重置开火标志
            reloadTextShown = false; // 切出武器时重置 reload 文本显示标记
           	Player.HideText(); // 切换武器时隐藏 reload 文本
        }

        public override void SwitchToSet()
		{
            if (gunClip == null)  // 检查是否需要初始化
			{
				gunClip = new GunClip(8);
			}
			gunClip.UpdateClipUI();
			Sprite.enabled = true; // 重新启用sprite
		}
		
		public override AudioClip GetCurrentlyPlayingSound()
		{
			return SelfAudioSource.isPlaying ? SelfAudioSource.clip : null;
		}
		
		public override void HideSprite()
		{
			Sprite.enabled = false;
		}
    }
}