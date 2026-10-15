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
            // 伤害/吸血统一走即时命中结算（未强化系数 0.8，与历史口径一致；
            // 强化乘区与 Boss 吸血条件由 Resolver 补齐，修复旧副本的漂移）。
            // 返回 false（无接口/已死亡）时不消耗穿透次数，尸体不再触发吸血
            var ctx = new InstantHitContext(0.8f, isEnhanced, weaponType, lifestealPercent);
            if (InstantHitResolver.Apply(collision.collider, damage, ctx))
            {
                currentPenetrationCount++;
                if (currentPenetrationCount >= maxPenetrationCount)
                {
                    Recycle();
                }
            }
        }
        else
        {
            Recycle();
        }
    }
}
