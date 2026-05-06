using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using ProjectBlood;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class Enemy2 : MonoBehaviour, IDamageable
    {
        public Player player;
        public EnemyBullet enemyBullet;
        public float speed = 2.0f;
        public float currentHealth = 100.0f;
        public float Damage = 2.0f;
        public SpriteRenderer spriteRenderer;
        
        // 血量、受击、死亡机制相关
        private bool isDying = false; // 避免重复死亡
        private Color originalColor;  // 恢复受击后颜色
        private Collider2D[] allColliders;
        
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
        public AudioSource EnemyShotAudioSource;

        // Start is called before the first frame update
        void Start()
        {
            // 初始化血量、受击、死亡相关
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

        // 射击模式参数 - 修改为固定两次连射
        public float shootInterval = 2.0f;      // 两次连射之间的间隔
        public float burstInterval = 0.2f;      // 连射中每发子弹间隔
        public int bulletsPerBurst = 3;         // 一次连射n发
        public int totalBurstCount = 2;         // 新增：总共进行几次连射
        
        // 射击状态变量
        private int currentBurstCount = 0;      // 当前已完成的连射次数
        private float shootIntervalTimer = 0f;  // 连射间隔计时器
        private float burstIntervalTimer = 0f;  // 连射内子弹间隔计时器
        private int shotsFired = 0;             // 当前连射已发射子弹数
        private bool isInBurst = false;         // 是否正在连射阶段

        

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
                        StartWander(); // 这里会用到一次玩家方向来生成垂直方向
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
                    // Shoot状态：不移动，不更新方向，只射击
                    currentShootTime += Time.deltaTime;
                    
                    // 先执行攻击逻辑
                    AttackPlayer();

                    // 射击时间到了，检查是否还在攻击范围
                    if (currentShootTime >= currentShootDuration)
                    {
                        if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
                        {
                            currentState = State.Wander;
                            StartWander();
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

        // 开始Shoot状态
        void StartShoot()
        {
            currentShootTime = 0.0f;
            currentShootDuration = Random.Range(shootMinDuration, shootMaxDuration);

            // 重置射击状态变量 - 修改为固定两次连射
            currentBurstCount = 0;
            shootIntervalTimer = 0f;
            burstIntervalTimer = 0f;
            shotsFired = 0;
            isInBurst = false;
        }

        void AttackPlayer()
        {
            // 如果已经超出射击时间，直接返回
            if (currentShootTime >= currentShootDuration)
            {
                return;
            }

            // 如果还没开始第一次连射，立即开始
            if (currentBurstCount == 0 && !isInBurst)
            {
                isInBurst = true;
                FireBullet();
                shotsFired++;
                return;
            }

            if (isInBurst)
            {
                // 正在连射阶段
                burstIntervalTimer += Time.deltaTime;
                if (burstIntervalTimer >= burstInterval)
                {
                    // 检查是否还在射击时间内
                    if (currentShootTime >= currentShootDuration)
                    {
                        return;
                    }
                    
                    if (shotsFired < bulletsPerBurst)
                    {
                        // 继续发射当前连射的子弹
                        FireBullet();
                        shotsFired++;
                        burstIntervalTimer = 0f;
                    }
                    else
                    {
                        // 当前连射完成
                        isInBurst = false;
                        currentBurstCount++;
                        shotsFired = 0;
                        burstIntervalTimer = 0f;
                        
                        // 如果还没完成所有连射，开始间隔计时
                        if (currentBurstCount < totalBurstCount)
                        {
                            shootIntervalTimer = 0f;
                        }
                    }
                }
            }
            else
            {
                // 不在连射阶段，等待连射间隔
                if (currentBurstCount < totalBurstCount)
                {
                    shootIntervalTimer += Time.deltaTime;
                    if (shootIntervalTimer >= shootInterval)
                    {
                        // 开始新一轮连射
                        isInBurst = true;
                        FireBullet();
                        shotsFired++;
                        shootIntervalTimer = 0f;
                    }
                }
            }
        }

        void FireBullet()
        {
            if (enemyBullet == null || player == null) return;

            // 计算朝玩家的方向
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            UpdateRotate(dirToPlayer); // 更新朝向
            
            // 生成子弹
            EnemyBullet bullet = Instantiate(enemyBullet, transform.position, Quaternion.identity);
            bullet.direction = dirToPlayer;  // 假设EnemyBullet里用这个方向
            bullet.gameObject.SetActive(true);

            int randomIndex = Random.Range(0, EnemyShootSounds.Count);
            // EnemyShotAudioSource.clip = EnemyShootSounds[randomIndex];
            // EnemyShotAudioSource.Play();
            AudioKit.PlaySound("EnemyShoot1", volume: 0.2f);
        }

        public float HitDamage { get => Damage; }
        public bool IsDying { get => isDying; }
        
        public void TakeDamage(float damage)
        {
            if (isDying) return;
            
            AudioKit.PlaySound("Torch Impact 2", volume: 0.5f);
            this.currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                // 生成掉落物
                Global.GenerateDrops(this.gameObject);
                StartCoroutine(DeathSequence());
            }
            else
            {
                StartCoroutine(FlashWhite());
            }
        }
        
        // 受击闪白效果
        private IEnumerator FlashWhite()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.18f);
                spriteRenderer.color = originalColor;
            }
        }
        
        // 死亡序列（闪红后销毁）
        private IEnumerator DeathSequence()
        {
            Room.GetEnemies().Remove(this);
            isDying = true;
            speed = 0f;
            
            // 禁用所有碰撞体
            if (allColliders != null)
            {
                foreach (var c in allColliders)
                {
                    if (c != null) c.enabled = false;
                }
            }
            
            // 死亡前闪烁
            if (spriteRenderer != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    spriteRenderer.color = Color.white;
                    yield return new WaitForSeconds(0.08f);
                    spriteRenderer.color = Color.red;
                    yield return new WaitForSeconds(0.08f);
                }
            }
            
            yield return new WaitForSeconds(0.15f);
            Global.currentNum.Value -= 1;
            this.DestroyGameObjGracefully();
        }

        public void UpdateRotate(Vector3 dirToPlayer)
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
        
        public GameObject GameObject { get => gameObject; }
		public Room Room { get; set; }
        public void OnDestroy()
        {
            Room.GetEnemies().Remove(this);
        }
    }
}