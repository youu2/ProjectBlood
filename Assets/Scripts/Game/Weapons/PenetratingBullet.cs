using ProjectBlood;
using UnityEngine;

public class PenetratingBullet : PlayerBullet
{
    public int maxPenetrationCount = 3; // 最大穿透数量
    private int currentPenetrationCount = 0; // 当前穿透数量

    // 池复用时必须重置穿透计数，否则第二次借出会沿用上一颗子弹的计数
    protected override void ResetPooledState()
    {
        base.ResetPooledState();
        currentPenetrationCount = 0;
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        // 同一物理帧的多个碰撞回调可能已在之前的回调中回收本子弹
        if (isRecycled) return;

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
                Recycle();
            }
        }
        else
        {
            Recycle();
        }
    }
}
