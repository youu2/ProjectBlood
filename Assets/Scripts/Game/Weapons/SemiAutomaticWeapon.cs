using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public abstract class SemiAutomaticWeapon : WeaponBase
    {
        protected bool canFire = true;
        protected float fireRate = 0.5f;  // 开火间隔
        
        public override void StartAttacking(Vector2 shootDir)
        {
            if (canFire && gunClip != null && gunClip.CanShoot())
            {
                Attack(shootDir);
                gunClip.Shoot();
                canFire = false;
                Invoke(nameof(ResetFire), fireRate);
            }
        }
        
        public override void KeepAttacking(Vector2 shootDir)
        {
            // 半自动武器只在按下时开火一次，不需要持续攻击逻辑
        }
        
        public override void StopAttacking()
        {
            // 半自动武器不需要停止逻辑
        }
        
        private void ResetFire()
        {
            canFire = true;
        }
    }
}