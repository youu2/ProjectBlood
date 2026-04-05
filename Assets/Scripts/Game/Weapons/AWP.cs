using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class AWP : ProjectBlood.IWeapon
	{
        // public PlayerBullet Bullet; QF架构bind功能生成的DEBullet替代，可在designer中直接绑定
        // public override float HitDamage => 0.5f;
        public AttackInterval AttackInterval = new AttackInterval(1.6f);
        // public float attackInterval = 0.5f; // 攻击间隔
        // private float lastAttackTime = 0f; // 上次攻击时间

        public List<AudioClip> ShootSounds = new List<AudioClip>();
        // public AudioSource shootAudioSource; 
        // 被QF架构other bind功能生成的SelfAudioSource替代，可在designer中直接绑定
        public GunClip gunClip = new GunClip(10, null); // AWP的弹夹，最大弹药量为10
        private FireFlash fireFlash = new FireFlash(); // 枪口火焰特效组件
        public void Start()
        {
            gunClip = new GunClip(10, SelfAudioSource); // AWP的弹夹，最大弹药量为10
			gunClip.UpdateClipUI();
        }

        public override void Attack(Vector2 shootDir)
        {
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, shootDir);
            var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
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
            }
        }

        public override void StopAttacking()
        {
            // DE射速较慢，停止攻击时不需要额外逻辑
        }

        public override void Reload(System.Action onReloadComplete = null)
        {
            // 按R键换弹
            if (Input.GetKeyDown(KeyCode.R))
            {
                gunClip.Reload(reloadSound, this, onReloadComplete); // 调用GunClip的reload方法进行换弹
            }
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
			Sprite.enabled = false;
		}
	}
}
