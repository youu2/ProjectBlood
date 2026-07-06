using UnityEngine;

namespace ProjectBlood
{
    public class AttackIntervalFeature
    {
        private float attackInterval;

        public float lastAttackTime = 0f;

        public AttackIntervalFeature(float attackInterval = 0.5f)
        {
            this.attackInterval = attackInterval;
        }

        public bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackInterval;
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