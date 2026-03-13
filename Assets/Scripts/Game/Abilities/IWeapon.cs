using UnityEngine;

namespace ProjectBlood
{
    public interface IWeapon
    {
        float HitDamage { get; }
        public void Attack(Vector2 shootDir);
    }
}