using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 红色处决目标标记：挂在玩家下的自驱组件，每帧把标记定位到
    /// 袖剑半径内最近的一个"濒死且可处决"敌人头顶；无目标时隐藏。
    /// 与自动瞄准的白色 AimMark 相互独立。
    /// </summary>
    public class ExecutionTargetMarker : MonoBehaviour
    {
        [Tooltip("标记渲染器（留空自动取本物体上的 SpriteRenderer）")]
        [SerializeField] private SpriteRenderer markerRenderer;

        [Tooltip("标记相对敌人原点的高度偏移")]
        [SerializeField] private float heightOffset = 1.5f;

        private void Awake()
        {
            if (markerRenderer == null) markerRenderer = GetComponent<SpriteRenderer>();
            if (markerRenderer != null) markerRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (markerRenderer == null) return;

            EnemyBase dyingTarget = FindNearestDyingTarget();
            if (dyingTarget == null)
            {
                if (markerRenderer.enabled) markerRenderer.enabled = false;
                return;
            }

            markerRenderer.enabled = true;
            Vector3 targetPos = dyingTarget.transform.position;
            targetPos.y += heightOffset;
            targetPos.z = transform.position.z;
            transform.position = targetPos;
        }

        // 只在当前房间、技能半径内寻找濒死目标（含墙后），取最近一个
        private EnemyBase FindNearestDyingTarget()
        {
            if (Player.player1 == null || Global.currentRoom == null) return null;

            Vector3 origin = Player.player1.transform.position;
            float rangeSqr = HiddenBladeSettings.Range * HiddenBladeSettings.Range;
            EnemyBase nearest = null;
            float nearestDistSqr = float.MaxValue;

            foreach (IDamageable damageable in Global.currentRoom.GetEnemies())
            {
                // Unity 伪 null 判空必须先于 transform 访问（敌人帧末销毁的边界帧）
                if (!(damageable is EnemyBase enemy) || enemy == null || enemy.IsDead) continue;

                float distSqr = ((Vector2)(enemy.transform.position - origin)).sqrMagnitude;
                if (distSqr > rangeSqr) continue;
                if (!HiddenBladeSettings.IsInExecutionRange(enemy.CurrentHealth, enemy.MaxHealth, enemy.canBeExecuted)) continue;
                if (distSqr >= nearestDistSqr) continue;

                nearestDistSqr = distSqr;
                nearest = enemy;
            }
            return nearest;
        }
    }
}
