using QFramework;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectBlood
{
    public class SemiAutomaticWeapon : WeaponBase
    {
        protected AudioPlayer _shootLoopPlayer;

        public override void Attack(Vector2 shootDir)
        {
            base.Attack(shootDir);
            int randomIndex = Random.Range(0, ShootSounds.Count);
            AudioKitManager.Instance?.PlayOneShot(ShootSounds[randomIndex], FireVolume);
        }

        public override void StopAttacking()
        {
            // 射速较慢，无循环音效，停止攻击时不需要尾音
        }

    }
}