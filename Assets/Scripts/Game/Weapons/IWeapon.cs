using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    [ ViewControllerChild ]
    public abstract class IWeapon : ViewController
    {
        // public abstract float HitDamage { get; }
        public AudioClip reloadSound;
        public abstract void Attack(Vector2 shootDir);
        public virtual  void StartAttacking(Vector2 shootDir)
        {
            // Default implementation - can be overridden by subclasses
        }
        public abstract void keepAttacking(Vector2 shootDir);
        public abstract void StopAttacking();
        public virtual void Reload(){}
        public virtual void SwitchFromSet(){}
        public virtual void SwitchToSet(){} // 切回武器时的特殊处理逻辑
    }
}