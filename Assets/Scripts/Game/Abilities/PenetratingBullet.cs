using ProjectBlood;
using UnityEngine;

public class PenetratingBullet : PlayerBullet
{
    public int maxPenetrationCount = 3; // 最大穿透数量
    private int currentPenetrationCount = 0; // 当前穿透数量

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collision.gameObject.GetComponent<Enemy>().TakeDamage(damage);
            currentPenetrationCount++;
            if (currentPenetrationCount >= maxPenetrationCount)
            {
                Destroy(gameObject); // 达到最大穿透数量后销毁子弹
            }
        }
        else
        {
            Destroy(gameObject); // 碰到非敌人时销毁子弹
        }
    }
}