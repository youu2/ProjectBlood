using ProjectBlood;
using UnityEngine;

public class PenetratingBullet : PlayerBullet
{
    public int maxPenetrationCount = 3; // 最大穿透数量
    private int currentPenetrationCount = 0; // 当前穿透数量

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 计算击退方向：从玩家到敌人的方向
            Vector2 playerToEnemyDir = (collision.transform.position - Player.player1.transform.position).normalized;

            // 根据子弹是否被强化计算伤害
            float damageMultiplier = isEnhanced ? 1.0f : 0.8f; // 未强化时伤害降低到80%
            float finalDamage = damage * damageMultiplier;

            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            // 如果敌人正在死亡，跳过（防止多弹丸同时命中时重复触发）
            // if (damageable.IsDying)
            // {
            //     return;
            // }

            // 判断本击是否致命
            float enemyCurrentHP = damageable.CurrentHealth;
            float enemyMaxHP = damageable.MaxHealth;
            bool isLethal = enemyCurrentHP - finalDamage <= 0f;

            damageable.TakeDamage(finalDamage, playerToEnemyDir);

            // 吸血：致命一击时按敌人总生命值换算为 PB 道具
            if (isLethal && lifestealPercent > 0f && Player.player1 != null && enemyMaxHP > 0f)
            {
                float totalLifesteal = enemyMaxHP * (lifestealPercent / 100f);
                Global.GeneratePureBlood(collision.gameObject, totalLifesteal);
            }

            currentPenetrationCount++;
            if (currentPenetrationCount >= maxPenetrationCount)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
