using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 10.0f;
    public float damage = 20.0f;
    public float lifestealPercent = 0f; // 吸血比例(%)
    public bool isEnhanced = true; // 当前子弹是否被血库强化

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 移动
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);

        // 让子弹图案跟着方向转
        // 默认朝右是 0 度，所以直接用 Atan2 算角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0, 0, angle);
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 根据子弹是否被强化计算伤害
            float damageMultiplier = (isEnhanced ? 1.0f : 0.7f) * PlayerUpgrade.DamageRatio; // 未强化时伤害降低到70%
            float finalDamage = damage * damageMultiplier;

            // 计算玩家到敌人的方向
            Vector2 playerToEnemyDir = (collision.transform.position - Player.player1.transform.position).normalized;

            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable == null)
            {
                Destroy(gameObject);
                return;
            }

            // 防止多弹丸同时命中时重复触发吸血）
            if (damageable.CurrentHealth <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 读取敌人当前/最大血量，判断本击是否致命
            // 吸血机制：致命一击时改为从敌人死亡位置生成 PB 道具，不再直接加血
            float enemyCurrentHP = damageable.CurrentHealth;
            float enemyMaxHP = damageable.MaxHealth;
            bool isLethal = enemyCurrentHP - finalDamage <= 0f; // 判断是否致命一击


            // 应用伤害
            damageable.TakeDamage(finalDamage, playerToEnemyDir);

            // 吸血：致命一击时按敌人总生命值换算为 PB 道具
            if (isLethal && lifestealPercent > 0f && Player.player1 != null && enemyMaxHP > 0f && isEnhanced)
            {
                float totalLifesteal = enemyMaxHP * (lifestealPercent / 100f);
                Global.GeneratePureBlood(collision.gameObject, totalLifesteal);
            }

            Destroy(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
