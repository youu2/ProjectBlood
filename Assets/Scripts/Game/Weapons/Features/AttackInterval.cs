using UnityEngine;

namespace ProjectBlood
{
    public class AttackIntervalFeature
    {
        public float lastAttackTime = 0f;

        // 判定是否可以攻击:间隔由调用方传入(WeaponBase.EffectiveAttackInterval,
        // 已聚合强化系统的射速加成),本类只负责计时比较
        public bool CanAttack(float interval)
        {
            return Time.time - lastAttackTime >= interval;
        }

        public void RecordAttackTime()
        {
            lastAttackTime = Time.time;
        }

        public void Reset()
        {
            lastAttackTime = 0f;
        }
    }
}
