using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    ///  所有可对玩家造成伤害的实体必须实现的接口（包括普通敌人和可能会实现的陷阱和中立敌人）
    /// </summary>
    public interface IDamageable
    {
        Room Room { get; set; }
        GameObject GameObject { get; }
        float HitDamage { get; }
        void TakeDamage(float damage, Vector2 HitDir);
        // bool IsDying { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }     // 实体总生命值（用于吸血 PB 计算）
    }
}
