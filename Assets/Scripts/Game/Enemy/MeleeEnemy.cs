using System.Collections;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 近战敌人：进入触发距离后先播放攻击前摇（头顶"!"），前摇结束时重新检测
    // 距离与视线，玩家已脱离结算范围则空挥。袖剑慢动作会拉长前摇（缩放时间），
    // 给玩家处决/拉开距离的窗口。
    public class MeleeEnemy : Enemy
    {
        [Header("=== 近战攻击设置 ===")]
        [Tooltip("追击转攻击的触发距离")]
        [SerializeField] protected float meleeTriggerRange = 1.0f;
        [Tooltip("前摇结束时的伤害结算范围（略大于触发距离，持续移动的玩家仍可能被命中）")]
        [SerializeField] protected float meleeDamageRange = 1.5f;
        [Tooltip("攻击前摇时长（秒，缩放时间；袖剑慢动作中会被拉长）")]
        [SerializeField] protected float windupDuration = 0.3f;
        [SerializeField] protected float AttackInterval = 1.5f;

        protected override void Awake()
        {
            Damage = 2f;
            body = FxManager.Instance.Enemy1Body;
            base.Awake();

            // 复用基类状态机的距离字段：
            // chaseRange 决定 Chase→Fire，attackRange 作为语义上的伤害范围保留
            chaseRange = meleeTriggerRange;
            attackRange = meleeDamageRange;
        }

        protected override void UpdateFire(float deltaTime)
        {

        }

        protected override void StartFire()
        {
            currentState = State.Fire;
            StartCoroutine(FireSequence());
        }

        IEnumerator FireSequence()
        {
            // 1) 前摇：提示 + 等待（缩放时间，暂停/慢动作都会正确伸缩）
            ShowAttackWindup();
            yield return new WaitForSeconds(windupDuration);
            HideAttackWindup();

            // 2) 前摇结束复检：玩家仍在结算范围内且中间无墙体遮挡才造成伤害，否则空挥
            if (Player.player1 != null
                && GetDistanceToPlayer() <= meleeDamageRange
                && PerformLineOfSightCheck())
            {
                MakeDamage();
            }

            // 3) 攻击间隔后回到追击
            yield return new WaitForSeconds(AttackInterval);
            currentState = State.Chase;
        }

        // 敌人被击杀/卸载时确保"!"不会残留在头顶
        public override void OnDestroy()
        {
            HideAttackWindup();
            base.OnDestroy();
        }

#if UNITY_EDITOR
        // 调试：绘制近战触发/结算两个距离圈，便于调距离关系（仅 Editor）
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;

            UnityEditor.Handles.color = new Color(1f, 0.92f, 0.35f, 0.9f);
            UnityEditor.Handles.DrawWireDisc(origin, Vector3.back, meleeTriggerRange);

            UnityEditor.Handles.color = new Color(1f, 0.35f, 0.35f, 0.9f);
            UnityEditor.Handles.DrawWireDisc(origin, Vector3.back, meleeDamageRange);
        }
#endif
    }
}
