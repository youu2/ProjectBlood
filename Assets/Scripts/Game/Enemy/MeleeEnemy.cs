using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
    // 近战敌人
    public class MeleeEnemy : Enemy
    {
        [SerializeField] protected float AttackInterval = 1.5f;
        protected override void Awake()
        {
            Damage = 2f;
            body = FxManager.Instance.Enemy1Body;
            base.Awake();
        }

        protected override void UpdateFire(float deltaTime)
        {

        }

        protected override void StartFire()
        {
            currentState = State.Fire;
            StartCoroutine(FireSequence());
        }

        IEnumerator FireSequence()
        {
            MakeDamage();
            yield return new WaitForSeconds(1f);
            currentState = State.Chase;
        }

    }
}
