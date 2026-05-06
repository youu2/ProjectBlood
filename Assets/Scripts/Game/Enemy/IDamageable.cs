using UnityEngine;

namespace ProjectBlood
{
    public interface IDamageable
    {
        Room Room { get; set; }
        GameObject GameObject { get; }
        float HitDamage { get; }
        void TakeDamage(float damage);
        bool IsDying { get; }
    }
}
