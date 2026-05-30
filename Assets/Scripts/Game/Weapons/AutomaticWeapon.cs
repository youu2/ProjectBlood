using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
    public abstract class AutomaticWeapon : WeaponBase
    {
        protected AttackInterval attackInterval;
        protected bool newClip = true;
        protected bool hasFired = false;
        protected bool reloadTextShown = false;
        protected AudioPlayer _shootLoopPlayer;
        
        protected abstract AudioClip OneShotSound { get; }
        protected abstract AudioClip ShootEndSound { get; }
        protected abstract List<AudioClip> ShootSounds { get; }
        
        public override void StartAttacking(Vector2 shootDir)
        {
            if (gunClip?.CanShoot() ?? false)
            {
                // 播放单发音效和循环音效
                AudioManager?.PlayOneShot(OneShotSound);
                _shootLoopPlayer = AudioManager?.PlayLoop(ShootSounds[0]);
                newClip = false;
                hasFired = true;
            }
        }
        
        public override void KeepAttacking(Vector2 shootDir)
        {
            // 弹夹换新后继续按住左键的处理
            if (newClip && gunClip != null && gunClip.CanShoot() )
            {
                StartAttacking(shootDir);
                newClip = false;
            }
            
            // 正常射击逻辑
            if (attackInterval.CanAttack() && gunClip != null && gunClip.CanShoot())
            {
                Attack(shootDir);
                attackInterval.RecordAttackTime();
                gunClip.Shoot();
                reloadTextShown = false;
            }
            else if (gunClip != null && !gunClip.CanShoot() && !reloadTextShown)
            {
                StopAttacking();
                newClip = true;
                if (!gunClip.isReloading)
                {
                    Player.DisplayText("[R] to Reload!");
                    AudioManager?.PlayOneShot(DryFireClick);
                    reloadTextShown = true;
                }
            }
            TryPlayDryFireClick();
        }
        
        public override void StopAttacking()
        {
            if (hasFired)
            {
                AudioManager?.PlayOneShot(ShootEndSound);
            }
            AudioManager?.Stop(_shootLoopPlayer);
            _shootLoopPlayer = null;
            hasFired = false;
        }
        
        protected void TryPlayDryFireClick()
        {
            if (Time.frameCount % 50 == 0 && attackInterval.CanAttack() && gunClip != null && !gunClip.isReloading)
            {
                AudioManager?.PlayOneShot("DryFireClick");
            }
        }
        
        public override void SwitchFromSet()
        {
            attackInterval?.Reset();
            newClip = true;
            reloadTextShown = false;
            StopAttacking();
            StopReload();
            Player.HideText();
        }

        public override void SwitchToSet()
		{
			gunClip.UpdateClipUI();
			newClip = true;
		}
        
        public override bool HasFired() => hasFired;
        
        // public override AudioClip GetShootEndSound() => ShootEndSound;
        
        public override bool IsPlayingShootEnd() => false;
    }
}