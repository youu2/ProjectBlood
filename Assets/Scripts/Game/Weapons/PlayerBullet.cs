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
    [SerializeField] protected float lifetime = 3f; // 子弹寿命(秒)，到期未命中则强制回收，射程 = speed × lifetime，可按武器在预制体调整
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
        StartCoroutine(LifetimeRoutine());
    }

    // 寿命兜底：子弹只靠碰撞回收时，射不进墙的开阔弹道会永久挂在池的借出账上。
    // 协程挂在子弹自身上，SetActive(false)(回池/关卡切换强制回收)会自动终止，
    // 下次借出 OnEnable 重新计时；到期与碰撞竞态由 isRecycled 幂等拦截
    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Recycle();
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
            // 命中后的伤害/吸血统一走 DamageResolver 结算（未强化系数 0.7，与历史口径一致）；
            // 无论是否命中（无接口/已死亡）都按池规则回收
            var ctx = new InstantHitContext(0.7f, isEnhanced, weaponType, lifestealPercent);
            DamageResolver.Apply(collision.collider, damage, ctx);
        }

        Recycle();
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
