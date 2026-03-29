using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    [ ViewControllerChild ]
    public abstract class IWeapon : ViewController
    {
        // public abstract float HitDamage { get; }
        public abstract void Attack(Vector2 shootDir);
        public virtual  void StartAttacking(Vector2 shootDir)
        {
            // Default implementation - can be overridden by subclasses
        }
        public abstract void keepAttacking(Vector2 shootDir);
        public abstract void StopAttacking(Vector2 shootDir);
        public virtual void Reload(){}
    }
}