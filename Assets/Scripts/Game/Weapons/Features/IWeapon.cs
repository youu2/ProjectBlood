using QFramework;
using UnityEngine;

namespace ProjectBlood{
    public interface IWeapon
    {
        void Attack(Vector2 shootDir);
        void StartAttacking(Vector2 shootDir);
        void KeepAttacking(Vector2 shootDir);
        void StopAttacking();
        void Reload();
    }
}