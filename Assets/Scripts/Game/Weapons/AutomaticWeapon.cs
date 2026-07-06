using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
    public abstract class AutomaticWeapon : WeaponBase
    {
        protected bool newClip = true;
        protected bool hasFired = false;
        protected bool reloadTextShown = false;
        protected AudioPlayer _shootLoopPlayer;
        [Range(0, 360)][SerializeField] protected float spreadAngle = 2f;
        protected abstract AudioClip OneShotSound { get; }
        protected abstract AudioClip ShootEndSound { get; }
        protected abstract List<AudioClip> ShootSounds { get; }

        // 全自动武器支持随机散布
        protected Vector2 ApplySpread(Vector2 baseDirection)
        {
            if (spreadAngle <= 0f)
            {
                return baseDirection;
            }
            float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
            Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
            return randomRotation * baseDirection;
        }

        public override void StartAttacking(Vector2 shootDir)
        {
            if (gunClip?.CanShoot() ?? false)
            {
                // 播放单发音效和循环音效
                AudioKitManager.Instance?.PlayOneShot(OneShotSound, volume: 0.55f);
                _shootLoopPlayer = AudioKitManager.Instance?.PlayLoop(ShootSounds[0], volume: 0.65f);
                newClip = false;
                hasFired = true;
            }
        }

        public override void KeepAttacking(Vector2 shootDir)
        {
            // 弹夹换新后继续按住左键的处理，重新开始播放射击循环音效
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
                    Player.DisplayText("[R] to Reload!");
                    AudioKitManager.Instance?.PlayOneShot("DryFireClick", volume: 0.7f);
                    reloadTextShown = true;
                }
            }
            TryPlayDryFireClick();
        }

        public override void StopAttacking()
        {
            if (hasFired)
            {
                AudioKitManager.Instance?.PlayOneShot(ShootEndSound, volume: 0.7f);
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