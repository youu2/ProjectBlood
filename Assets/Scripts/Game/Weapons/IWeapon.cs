using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    [ ViewControllerChild ]
    public abstract class IWeapon : ViewController
    {
        // public abstract float HitDamage { get; }
        public AudioClip reloadSound;
        protected bool recentlyFired = false; // 标记是否最近开火过（用于半自动武器延迟隐藏）
        protected float lastFireTime = 0f; // 上次开火时间
        protected const float FIRE_SOUND_DURATION_THRESHOLD = 0.8f; // 开火后多久内算作"正在播放枪声"
        public virtual int BloodRequired { get; } = 1; // 每次换弹需要的血量
        public abstract void Attack(Vector2 shootDir);
        public virtual void StartAttacking(Vector2 shootDir)
        {
            // Default implementation - can be overridden by subclasses
        }
        public abstract void keepAttacking(Vector2 shootDir);
        public abstract void StopAttacking();
        public virtual void Reload(System.Action onReloadComplete = null) {}
        public virtual void SwitchFromSet(){}
        public virtual void SwitchToSet(){} // 切回武器时的特殊处理逻辑
        public virtual AudioClip GetShootEndSound() { return null; } // 获取shootEnd音效，用于切换武器时播放(全自动武器)
        public virtual AudioClip GetCurrentlyPlayingSound() { return null; } // 获取当前正在播放的音效（用于半自动武器）
        public virtual bool ShouldDelayHide() { return recentlyFired && (Time.time - lastFireTime) < FIRE_SOUND_DURATION_THRESHOLD; } // 是否应该延迟隐藏
        public virtual float GetHideDelayTime() { return FIRE_SOUND_DURATION_THRESHOLD; } // 获取延迟隐藏时间
        public virtual void HideSprite() {} // 隐藏武器的sprite，子类需要重写
        public virtual bool HasFired() { return false; } // 检查武器是否真正开火过
        public virtual bool IsPlayingShootEnd() { return false; } // 检查是否正在播放 shootEnd 音效
    }
}