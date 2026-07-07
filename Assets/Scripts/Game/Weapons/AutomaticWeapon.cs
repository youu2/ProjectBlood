using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
    public class AutomaticWeapon : WeaponBase
    {
        protected bool hasFired = false;    // 记录是否真的射击过，避免无弹药时也会播放射击结束音效的问题
        protected AudioPlayer _shootLoopPlayer;
        [SerializeField] protected AudioClip OneShotSound;// 开火时播放一次的音效
        [SerializeField] protected AudioClip ShootEndSound;// 射击结束时播放一次的音效
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
            base.KeepAttacking(shootDir);
        }

        public override void StopAttacking()
        {
            if (hasFired)   // 避免没弹药时松开左键也会播放射击结束音效的问题
            {
                AudioKitManager.Instance?.PlayOneShot(ShootEndSound, volume: ShootEndVolume);
            }
            AudioKitManager.Instance?.Stop(_shootLoopPlayer);
            _shootLoopPlayer = null;
            hasFired = false;
        }
    }
}