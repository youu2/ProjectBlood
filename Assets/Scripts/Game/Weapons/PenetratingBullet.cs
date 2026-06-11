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
            
            collision.gameObject.GetComponent<IDamageable>()
                .TakeDamage(finalDamage, playerToEnemyDir);

            // 应用吸血
            if (lifestealPercent > 0 && Player.player1 != null)
            {
                float lifestealAmount = finalDamage * (lifestealPercent / 100f);
                float newHP = Global.currentHP.Value + lifestealAmount;
                Global.currentHP.Value = Mathf.Min(newHP, Global.MAX_HP.Value);
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
