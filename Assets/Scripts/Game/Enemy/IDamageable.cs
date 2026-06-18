using UnityEngine;

namespace ProjectBlood
{
    public interface IDamageable
    {
        Room Room { get; set; }
        GameObject GameObject { get; }
        float HitDamage { get; }
        void TakeDamage(float damage, Vector2 HitDir);
        bool IsDying { get; }
        // 敌人当前血量
        float CurrentHealth { get; }
        // 敌人总生命值（用于吸血 PB 计算）
        float MaxHealth { get; }
    }
}
