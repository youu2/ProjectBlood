using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
    public class Slime1 : Enemy
    {
        protected override void Awake()
        {
            body = FxManager.Instance.Enemy1Body;
            base.Awake();
        }
        void OnTriggerEnter2D(Collider2D collision)
        {
            // 检测是否碰撞到玩家
            if (collision.gameObject.CompareTag("Player") && !isDying)
            {
                // 调用玩家的受伤方法
                Player.player1?.TakeDamage(HitDamage);
            }
        }
    }
}
