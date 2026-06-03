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
            // 计算玩家到敌人的方向
            Vector2 playerToEnemyDir = (collision.transform.position - Player.player1.transform.position).normalized;
            
            collision.gameObject.GetComponent<IDamageable>()
                .TakeDamage(damage, playerToEnemyDir);
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
