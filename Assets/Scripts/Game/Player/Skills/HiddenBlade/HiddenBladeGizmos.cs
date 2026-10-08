using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 袖剑调试 Gizmos：挂在玩家物体上（仅 Editor 下绘制）。
    /// - 技能半径圈；
    /// - 最近一次施法：起点→目标连线、目标点、原始落点、墙体修正后落点（保留 5 真实秒）。
    /// 近战敌人的两个距离圈见 MeleeEnemy.OnDrawGizmosSelected。
    /// </summary>
    public class HiddenBladeGizmos : MonoBehaviour
    {
        [Tooltip("未施法时是否持续绘制技能半径圈")]
        [SerializeField] private bool alwaysDrawRange = true;

        [SerializeField] private Color rangeColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color lineColor = Color.yellow;
        [SerializeField] private Color rawLandingColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color correctedLandingColor = Color.green;
        [SerializeField] private Color targetColor = Color.red;

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;

            if (alwaysDrawRange)
            {
                Gizmos.color = rangeColor;
                DrawCircle(origin, HiddenBladeSettings.Range, 32);
            }

            var cast = HiddenBladeSettings.LastCast;
            if (!cast.hasCast || Time.realtimeSinceStartup > cast.expireRealtime) return;

            // 起点 → 目标
            Gizmos.color = lineColor;
            Gizmos.DrawLine(cast.origin, cast.targetPosition);
            DrawMarker(cast.targetPosition, targetColor);

            // 目标 → 原始落点
            Gizmos.color = rawLandingColor;
            Gizmos.DrawLine(cast.targetPosition, cast.rawLanding);
            DrawCross(cast.rawLanding, rawLandingColor);

            // 墙体修正后的实际落点
            DrawCross(cast.correctedLanding, correctedLandingColor);
        }

        private static void DrawCircle(Vector3 center, float radius, int segments)
        {
            const float twoPi = Mathf.PI * 2f;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = twoPi * i / segments;
                Vector3 cur = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        // 目标位置：红色实心小球
        private static void DrawMarker(Vector3 position, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(position, 0.12f);
        }

        // 落点：X 形十字
        private static void DrawCross(Vector3 position, Color color, float size = 0.2f)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(position + Vector3.left * size + Vector3.up * size,
                            position + Vector3.right * size + Vector3.down * size);
            Gizmos.DrawLine(position + Vector3.left * size + Vector3.down * size,
                            position + Vector3.right * size + Vector3.up * size);
        }
    }
}
