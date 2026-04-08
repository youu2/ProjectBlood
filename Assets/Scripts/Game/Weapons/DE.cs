using UnityEngine;
using ProjectBlood;
using System.Collections.Generic;
using QFramework;

namespace ProjectBlood
{
    public partial class DE : IWeapon
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
        private GunClip gunClip = new GunClip(8, null); // DE的弹夹，最大弹药量为8
        private FireFlash fireFlash = new FireFlash(); // DE的枪口火焰特效组件
        public override int BloodRequired { get; } = 1; // 每次换弹需要的血量
        private bool reloadTextShown = false; // 标记是否已经显示过 reload 文本，防止文本闪烁

        //换弹功能：
        // public int MaxAmmo = 8; // DE的最大弹药量
        // private int currentAmmo; // 当前弹药量
        private void Start()
        {
            // currentAmmo = MaxAmmo; // 初始时弹药量为最大值
            gunClip = new GunClip(8, SelfAudioSource); // DE的弹夹，最大弹药量为8
            gunClip.UpdateClipUI(); // 初始化UI显示的弹药信息
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
        public override void keepAttacking(Vector2 shootDir)
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
					reloadTextShown = true; // 标记已经显示过 reload 文本
				}
			}
        }

        public override void StopAttacking()
        {
            // DE射速较慢，停止攻击时不需要额外逻辑
        }

        public override void SwitchFromSet()
        {
            gunClip.StopReload(this); // 切出武器时停止换弹流程
            gunClip.isReloading = false; // 切出武器时重置换弹状态，确保下次切回时可以正常换弹
            recentlyFired = false; // 切出武器时重置开火标志
            reloadTextShown = false; // 切出武器时重置 reload 文本显示标记
           	Player.HideText(); // 切换武器时隐藏 reload 文本
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
			Sprite.enabled = false;
		}
    }
}