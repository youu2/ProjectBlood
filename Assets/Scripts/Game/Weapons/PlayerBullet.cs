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
            float damageMultiplier = isEnhanced ? 1.0f : 0.8f; // 未强化时伤害降低到80%
            float finalDamage = damage * damageMultiplier;
            
            // 计算玩家到敌人的方向
            Vector2 playerToEnemyDir = (collision.transform.position - Player.player1.transform.position).normalized;
            
            collision.gameObject.GetComponent<IDamageable>()
                .TakeDamage(finalDamage, playerToEnemyDir);

            // 应用吸血
            if (lifestealPercent > 0 && Player.player1 != null)
            {
                float lifestealAmount = finalDamage * (lifestealPercent / 100f);
                float newHP = Global.currentHP.Value + lifestealAmount;
                Global.currentHP.Value = Mathf.Min(newHP, Global.MAX_HP.Value);
            }

            Destroy(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
