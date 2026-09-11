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
    public WeaponType weaponType = WeaponType.DE; // 发射该子弹的武器类型，由武器开火时设置，用于强化伤害计算
    public GameObject BulletPrefab;

    // 回收状态跟踪：同一物理帧内子弹可能同时接触多个碰撞体，
    // Unity 会把多个 OnCollisionEnter2D 回调排在同一帧派发，
    // 该标记保证一次借出生命周期内只归还对象池一次
    protected bool isRecycled;

    /// <summary>
    /// 每次从池中取出（actionOnGet -> SetActive(true)）时重置运行时状态，
    /// 子类可重写以追加自己的状态重置
    /// </summary>
    protected virtual void ResetPooledState()
    {
        isRecycled = false;
    }

    protected virtual void OnEnable()
    {
        ResetPooledState();
    }

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
        // 同一物理帧的多个碰撞回调可能已在之前的回调中回收本子弹
        if (isRecycled) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 根据子弹是否被强化以及强化系统加成计算伤害（未强化时伤害降低到70%）
            float damageMultiplier = (isEnhanced ? 1.0f : 0.7f) * PlayerUpgradeState.GetFinalDamageRatio(weaponType);
            float finalDamage = damage * damageMultiplier;

            // 计算玩家到敌人的方向
            Vector2 playerToEnemyDir = (collision.transform.position - Player.player1.transform.position).normalized;

            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable == null)
            {
                // 无可伤害组件也必须走池回收，直接 Destroy 会破坏池的状态跟踪
                Recycle();
                return;
            }

            // 防止多弹丸同时命中时重复触发吸血）
            if (damageable.CurrentHealth <= 0f)
            {
                Recycle();
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

            Recycle();
        }
        else
        {
            Recycle();
        }
    }

    /// <summary>
    /// 幂等回收：同一子弹在同一物理帧内产生多个碰撞回调时，只有第一次会真正归还对象池。
    /// 所有销毁路径（命中敌人/撞墙/无可伤害组件）都应经过此方法，禁止再直接 Destroy 池对象。
    /// </summary>
    public void Recycle()
    {
        if (isRecycled) return;
        isRecycled = true;

        if (PlayerBulletPool.Instance != null)
        {
            PlayerBulletPool.Instance.Release(gameObject, BulletPrefab);
        }
        else
        {
            // 池不存在（如应用退出阶段单例已被销毁）时兜底销毁，避免悬空对象
            Destroy(gameObject);
        }
    }
}
