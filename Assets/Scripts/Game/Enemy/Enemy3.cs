// Enemy2的改进版，更可控，自定义程度更高，支持散射弹丸的射击行为
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using ProjectBlood;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Enemy3 : Enemy
    {
        public Player player;
        public EnemyBullet enemyBullet;
        public float speed = 2.0f;
        
        public enum State
        {
            Idle,       // 待机(暂未使用)
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

        public List<AudioClip> EnemyShootSounds = new List<AudioClip>();

        // Start is called before the first frame update
        void Start()
        {
            // 调用基类的初始化
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            allColliders = GetComponentsInChildren<Collider2D>(true);
            
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
        float shootMinDuration = 3.0f;
        float shootMaxDuration = 5.0f;
        float currentShootTime = 0.0f;
        float currentShootDuration = 0.0f;

        // 射击模式参数 - 两次连射
        public float shootInterval = 2.0f;      // 两次连射之间的间隔
        public float burstInterval = 0.2f;      // 连射中每发子弹间隔
        public int bulletsPerBurst = 3;         // 一次连射n发
        public int totalBurstCount = 2;         // 总共进行几次连射
        
        // 散射弹丸参数
        public int scatterBulletCount = 3;      // 每次射击同时发射的散射弹丸数量
        public float scatterAngle = 45f;        // 散布角度（总角度范围，单位：度）
        public bool useRandomScatter = false;   // true=随机散布，false=均匀分布角度
        
        private Coroutine shootCoroutine;       // 射击协程引用

        

        // Update is called once per frame
        void Update()
        {
            // 如果正在死亡，不执行任何逻辑
            if (isDying) return;
            
            // 如果玩家没了，就待机
            if (player == null)
            {
                currentState = State.Idle;
                return;
            }
            m_DirectionToPlayer = (player.transform.position - transform.position).normalized;
            switch (currentState)
            {
                case State.Chase:

                    UpdateRotate(m_DirectionToPlayer); // 更新朝向
                    // 朝玩家方向移动
                    transform.position += m_DirectionToPlayer * speed * Time.deltaTime;

                    // 如果进入追击范围，切换到Wander
                    if (Vector3.Distance(transform.position, player.transform.position) < chaseRange)
                    {
                        currentState = State.Wander;
                        StartWander();
                    }
                    break;

                case State.Wander:
                    // Wander状态：不更新玩家方向，只用一开始随机的垂直方向移动
                    transform.position += wanderDirection * speed * Time.deltaTime;

                    currentWanderTime += Time.deltaTime;

                    // 移动1秒后切换到Shoot
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

        // 开始Shoot状态
        void StartShoot()
        {
            currentShootTime = 0.0f;
            currentShootDuration = Random.Range(shootMinDuration, shootMaxDuration);
            
            // 启动射击协程
            if (shootCoroutine != null)
                StopCoroutine(shootCoroutine);
            shootCoroutine = StartCoroutine(ShootSequence());
        }

        // 射击序列协程 - 简洁版本
        IEnumerator ShootSequence()
        {
            for (int burstIndex = 0; burstIndex < totalBurstCount; burstIndex++)
            {
                // 发射一轮连射
                for (int i = 0; i < bulletsPerBurst; i++)
                {
                    FireBullet();
                    yield return new WaitForSeconds(burstInterval);
                }
                
                // 如果不是最后一轮，等待连射间隔
                if (burstIndex < totalBurstCount - 1)
                {
                    yield return new WaitForSeconds(shootInterval);
                }else
                {
                    currentState = State.Wander;
                    StartWander();
                }
            }
        }

        void FireBullet()
        {
            if (enemyBullet == null || player == null) return;

            // 计算朝玩家的方向
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            UpdateRotate(dirToPlayer); // 更新朝向
            
            // 发射散射弹丸
            for (int i = 0; i < scatterBulletCount; i++)
            {
                float angle = 0f;
                
                if (useRandomScatter)
                {
                    // 随机散布：在[-scatterAngle/2, scatterAngle/2]范围内随机
                    angle = Random.Range(-scatterAngle / 2f, scatterAngle / 2f);
                }
                else
                {
                    // 均匀分布：平均分配角度
                    if (scatterBulletCount == 1)
                    {
                        angle = 0f; // 只有一颗子弹时不偏移
                    }
                    else
                    {
                        // 从 -scatterAngle/2 到 +scatterAngle/2 均匀分布
                        angle = (-scatterAngle / 2f) + (scatterAngle / (scatterBulletCount - 1f)) * i;
                    }
                }
                
                // 将角度转换为弧度并计算偏移后的方向
                float radian = angle * Mathf.Deg2Rad;
                Vector3 bulletDirection = new Vector3(
                    dirToPlayer.x * Mathf.Cos(radian) - dirToPlayer.y * Mathf.Sin(radian),
                    dirToPlayer.x * Mathf.Sin(radian) + dirToPlayer.y * Mathf.Cos(radian),
                    0
                ).normalized;
                
                // 生成子弹
                EnemyBullet bullet = Instantiate(enemyBullet, transform.position, Quaternion.identity);
                bullet.direction = bulletDirection;
                bullet.gameObject.SetActive(true);
            }

            if (EnemyShootSounds.Count > 0)
            {
                AudioKitManager.Instance.PlayOneShot(EnemyShootSounds[Random.Range(0, EnemyShootSounds.Count)], volume: 0.2f);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDying) return;
            
            AudioKitManager.Instance.PlayOneShot("Torch Impact 2", volume: 0.5f);
            this.currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                // 生成掉落物
                Global.GenerateDrops(this.gameObject);
                // StartCoroutine(DeathSequence());
            }
            else
            {
                StartCoroutine(FlashWhite());
            }
        }
        
        // 死亡序列（闪红后销毁）
        // protected override IEnumerator DeathSequence()
        // {
        //     // 停止射击协程
        //     if (shootCoroutine != null)
        //         StopCoroutine(shootCoroutine);
            
        //     // 调用基类的死亡序列
        //     yield return StartCoroutine(base.DeathSequence());
        // }

        public override void UpdateRotate(Vector3 dirToPlayer)
        {
            if(dirToPlayer.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
        }
        
        protected override void FixedUpdate()
        {
            // 空实现，使用 Enemy3 自己的状态机控制移动，不使用基类的移动逻辑
        }
    }
}