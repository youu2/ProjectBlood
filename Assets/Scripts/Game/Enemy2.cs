using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using ProjectBlood;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    public Player player;
    float speed = 2.0f;
    public enum State
    {
        Idle,
        Chase,      // 追踪玩家
        Wander,     // 沿垂直玩家连线的随机方向直线移动1秒
        Shoot       // 原地停下射击1-2秒
    }
    public State currentState = State.Idle;

    // 一些距离参数
    public float chaseRange = 8f;   // 进入这个距离就从Chase切到Wander
    public float attackRange = 10f; // 超出这个距离就回到Chase

    // 玩家方向（只在需要时更新）
    Vector3 m_DirectionToPlayer;

    // Start is called before the first frame update
    void Start()
    {
        if (player == null)
        {
            player = Player.player1;
        }
        if (player)
        {
            currentState = State.Chase;
        }
    }

    // Wander状态相关
    public float wanderDuration = 1.0f;
    float currentWanderTime = 0.0f;
    Vector3 wanderDirection;

    // Shoot状态相关
    float shootMinDuration = 1.0f;
    float shootMaxDuration = 2.0f;
    float currentShootTime = 0.0f;
    float currentShootDuration = 0.0f;

    // Update is called once per frame
    void Update()
    {
        // 如果玩家没了，就待机
        if (player == null)
        {
            currentState = State.Idle;
            return;
        }

        switch (currentState)
        {
            case State.Chase:
                // Chase状态：每帧更新玩家方向，因为要追
                m_DirectionToPlayer = (player.transform.position - transform.position).normalized;

                // 朝玩家方向移动
                transform.position += m_DirectionToPlayer * speed * Time.deltaTime;

                // 如果进入追击范围，切换到Wander
                if (Vector3.Distance(transform.position, player.transform.position) < chaseRange)
                {
                    currentState = State.Wander;
                    StartWander(); // 这里会用到一次玩家方向来生成垂直方向
                }
                break;

            case State.Wander:
                // Wander状态：不更新玩家方向，只用一开始随机的垂直方向移动
                transform.position += wanderDirection * speed * Time.deltaTime;

                currentWanderTime += Time.deltaTime;

                // 1秒后切换到Shoot
                if (currentWanderTime >= wanderDuration)
                {
                    currentState = State.Shoot;
                    StartShoot();
                }

                // 如果玩家跑出攻击范围，回到Chase（这里需要实时距离检测，但方向不更新）
                if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
                {
                    currentState = State.Chase;
                }
                break;

            case State.Shoot:
                // Shoot状态：不移动，不更新方向，只射击
                currentShootTime += Time.deltaTime;
                AttackPlayer();

                // 射击时间到了，检查是否还在攻击范围
                if (currentShootTime >= currentShootDuration)
                {
                    if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
                    {
                        currentState = State.Wander;
                        StartWander(); // 这里会用到一次玩家方向来生成垂直方向
                    }
                    else
                    {
                        currentState = State.Chase;
                    }
                }
                break;
        }
    }

    // 开始Wander状态：随机选左右垂直方向（需要一次玩家方向）
    void StartWander()
    {
        currentWanderTime = 0.0f;

        // 计算玩家方向（只在进入Wander时用一次）
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        Vector3 perpendicular = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0);
        if (Random.Range(0, 2) == 0)
        {
            wanderDirection = perpendicular;
        }
        else
        {
            wanderDirection = -perpendicular;
        }
    }

    // 开始Shoot状态：随机1-2秒射击时长
    void StartShoot()
    {
        currentShootTime = 0.0f;
        currentShootDuration = Random.Range(shootMinDuration, shootMaxDuration);
    }

    void AttackPlayer()
    {
        // 射击逻辑，后面实现
        // 比如生成子弹啥的
    }
}