using UnityEngine;

namespace ProjectBlood
{
    public interface IDamageable
    {
        GameObject GameObject { get; }
        float HitDamage { get; }
        void TakeDamage(float damage);
        bool IsDying { get; }
    }
}
