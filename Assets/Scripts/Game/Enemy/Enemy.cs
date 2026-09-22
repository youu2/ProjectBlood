// 基础近战敌人：继承 EnemyBase，实现 AI 状态机（追踪/游走/攻击）
// 血量、受击、死亡、朝向、寻路等通用逻辑在 EnemyBase 中
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Enemy : EnemyBase
    {
        [Header("=== 基础敌人 AI 设置 ===")]
        [Tooltip("攻击距离通常要比追击距离远一点, 避免Wander期间敌人自己走出攻击范围")]
        [SerializeField] protected float attackRange = 12f;  // 攻击范围:超出这个距离回到Chase状态
        [SerializeField] protected float chaseRange = 10f;    // 追击范围:进入这个距离切换到Wander状态
        [SerializeField] protected float WanderDuration = 2.0f;
        protected float currentWanderTime = 0.0f;
        protected Vector3 wanderDirection = Vector3.right;

        public enum State
        {
            Idle,   // 空闲状态
            Chase,  // 追逐玩家
            Wander, // 游走，结束时开启充能
            Fire    // 攻击
        }
        public State currentState = State.Idle;

        // 目标点"到达阈值"：单位格子中心到边缘距离约0.5，取0.4确保进入格子即可通过
        private const float PathNodeArrivalThreshold = 0.4f;

        protected override void Awake()
        {
            base.Awake();
            if (Player.player1 != null) currentState = State.Chase;
        }

        protected override void Update()
        {
            if (Player.player1 == null)
            {
                currentState = State.Idle;
                return;
            }

            directionToPlayer = GetDirectionToPlayer();
            UpdateRotate(directionToPlayer);
            float distanceToPlayer = GetDistanceToPlayer();

            switch (currentState)
            {
                case State.Chase:
                    UpdateChase(distanceToPlayer);
                    break;
                case State.Wander:
                    UpdateWander(distanceToPlayer);
                    break;
                case State.Fire:
                    UpdateFire(distanceToPlayer);
                    break;
            }
        }

        protected virtual void UpdateChase(float distanceToPlayer)
        {
            // ===== 每帧动态刷新寻路路径 =====
            RecomputePath();

            // ===== 沿路径（或 fallback 直线朝玩家）移动 =====
            Vector3 moveDir;
            if (movePath.Count > 0)
            {
                var next = movePath[^1];
                if (next != null && next.Coords != null)
                {
                    var pos = next.Coords.Position;
                    var target = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);
                    var toTarget = target - transform.position;
                    if (new Vector2(toTarget.x, toTarget.y).sqrMagnitude <= PathNodeArrivalThreshold * PathNodeArrivalThreshold)
                    {
                        // 抵达当前下一跳：已经很近，直接朝玩家（下帧路径会自动更新到下一格）
                        moveDir = directionToPlayer;
                    }
                    else
                    {
                        moveDir = toTarget.normalized;
                    }
                }
                else
                {
                    moveDir = directionToPlayer;
                }
            }
            else
            {
                moveDir = directionToPlayer;
            }

            transform.position += moveDir * moveSpeed * Time.deltaTime;

            if (distanceToPlayer <= chaseRange && HasLineOfSightToPlayer())
            {
                currentState = State.Fire;
                StartFire();
            }
        }

        protected virtual void StartWander()
        {
            currentWanderTime = 0f;
            Vector3 perpendicular = new Vector3(-directionToPlayer.y, directionToPlayer.x, 0);
            wanderDirection = Random.Range(0, 2) == 0 ? perpendicular : -perpendicular;
        }

        protected virtual void UpdateWander(float distanceToPlayer)
        {
            if (Player.player1 == null) return;
            if (currentWanderTime >= WanderDuration)
            {
                StartFire();
            }
            if (distanceToPlayer > attackRange || !HasLineOfSightToPlayer())
            {
                currentState = State.Chase;
            }
            transform.position += wanderDirection * moveSpeed * Time.deltaTime;
            currentWanderTime += Time.deltaTime;

        }

        protected virtual void StartFire()
        {
            currentState = State.Fire;
        }

        protected virtual void MakeDamage()
        {
            // 层级检查：玩家处于"Invincible"层级（翻滚无敌期间）时跳过伤害施加
            if (Player.player1 != null
                && Player.player1.gameObject.layer == LayerMask.NameToLayer("Invincible"))
            {
                return;
            }
            Player.player1?.TakeDamage(HitDamage);
        }

        protected virtual void UpdateFire(float distanceToPlayer)
        {
            if (Player.player1 == null) return;
            if (distanceToPlayer > attackRange)
            {
                currentState = State.Chase;
            }
        }
    }
}
