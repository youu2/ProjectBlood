using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
    public class AutomaticWeapon : WeaponBase
    {
        protected bool newClip = true;
        protected bool hasFired = false;
        protected bool reloadTextShown = false;
        protected AudioPlayer _shootLoopPlayer;
        // [Range(0, 360)][SerializeField] protected float spreadAngle = 2f;
        [SerializeField] protected AudioClip OneShotSound;
        [SerializeField] protected AudioClip ShootEndSound;
        [SerializeField] protected float OnShotVolume = 0.65f;
        [SerializeField] protected float ShootEndVolume = 0.65f;

        public override void StartAttacking(Vector2 shootDir)
        {
            if (gunClip?.CanShoot() ?? false)
            {
                // 播放单发音效和循环音效
                AudioKitManager.Instance?.PlayOneShot(OneShotSound, volume: OnShotVolume);
                int randomIndex = Random.Range(0, ShootSounds.Count);
                _shootLoopPlayer = AudioKitManager.Instance?.PlayLoop(ShootSounds[randomIndex], volume: FireVolume);
                newClip = false;
                hasFired = true;
            }
        }

        public override void KeepAttacking(Vector2 shootDir)
        {
            // 全程按住左键换弹后，要重新开始播放射击循环音效
            if (newClip && gunClip != null && gunClip.CanShoot())
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
                    PlayFirstDryFireClick();
                    reloadTextShown = true;
                }
            }
            TryPlayDryFireClick();
        }

        public override void StopAttacking()
        {
            if (hasFired)
            {
                AudioKitManager.Instance?.PlayOneShot(ShootEndSound, volume: ShootEndVolume);
            }
            AudioKitManager.Instance?.Stop(_shootLoopPlayer);
            _shootLoopPlayer = null;
            hasFired = false;
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
            InitGunClip();
            gunClip.UpdateClipUI();
            newClip = true;
        }
    }
}