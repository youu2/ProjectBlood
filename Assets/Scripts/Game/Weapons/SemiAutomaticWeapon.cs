using QFramework;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectBlood
{
    public abstract class SemiAutomaticWeapon : WeaponBase
    {
        protected bool newClip = true;
        protected bool hasFired = false;
        protected bool reloadTextShown = false;
        protected AudioPlayer _shootLoopPlayer;
        protected abstract List<AudioClip> ShootSounds { get; }
        public override void KeepAttacking(Vector2 shootDir)
        {
            if (attackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
            {
                Attack(shootDir);
                attackInterval.RecordAttackTime();
                gunClip.Shoot(); // 射击时减少弹药量
                reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
            }
            else if (!gunClip.CanShoot() && !reloadTextShown)
            {
                if (!gunClip.isReloading)
                {
                    Player.DisplayText("[R] to Reload!");
                    AudioKitManager.Instance.PlayOneShot("DryFireClick", volume: 0.7f);
                    reloadTextShown = true; // 标记已经显示过 reload 文本
                }
            }
            TryPlayDryFireClick();
        }

        public override void StopAttacking()
        {
            // 射速较慢，无循环音效，停止攻击时不需要尾音
        }

        public override void SwitchFromSet()
        {
            StopReload();  // 调用 WeaponBase 的方法，内部会处理 gunClip.CancelReload()
            reloadTextShown = false; // 切出武器时重置 reload 文本显示标记
            Player.HideText(); // 切换武器时隐藏 reload 文本
        }

        public override void SwitchToSet()
        {
            InitGunClip();
            gunClip.UpdateClipUI();
        }
    }
}