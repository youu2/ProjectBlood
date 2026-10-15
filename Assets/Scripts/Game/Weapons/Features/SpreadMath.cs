using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 散射方向数学：统一玩家与敌人两侧的散布计算。
    /// 收敛原先的三份重复实现（WeaponBase.ApplySpread、ShotGun.Attack 循环内手写、
    /// EnemyBase.FireScatterBullets 手写三角函数），支持随机散布与均匀分布两种模式。
    /// </summary>
    public static class SpreadMath
    {
        /// <summary>在 spreadAngle 总角度范围内随机偏转一次的方向。</summary>
        public static Vector2 GetRandomDirection(Vector2 baseDirection, float spreadAngle)
        {
            float angle = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
            return RotateDirection(baseDirection, angle);
        }

        /// <summary>
        /// 均匀分布：count 发弹丸在 spreadAngle 内等间隔排布（index ∈ [0, count)），
        /// 弹幕/环形射击等确定性散布用；count &lt;= 1 或 spreadAngle &lt;= 0 时返回原方向。
        /// </summary>
        public static Vector2 GetUniformDirection(Vector2 baseDirection, float spreadAngle, int index, int count)
        {
            if (count <= 1 || spreadAngle <= 0f) return baseDirection;
            float angle = (-spreadAngle * 0.5f) + (spreadAngle / (count - 1f)) * index;
            return RotateDirection(baseDirection, angle);
        }

        /// <summary>方向绕 Z 轴旋转指定角度（0 度时原样返回，避免 Quaternion 开销）。</summary>
        public static Vector2 RotateDirection(Vector2 direction, float degrees)
        {
            if (degrees == 0f) return direction;
            Quaternion rotation = Quaternion.AngleAxis(degrees, Vector3.forward);
            return rotation * direction;
        }
    }
}
