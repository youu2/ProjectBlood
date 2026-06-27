// 射击型敌人基类 - 支持散射弹丸、连射模式, 可在Unity编辑器中自定义所有参数
// 使用方法:直接挂载到敌人对象上, 配置Inspector参数即可
using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class ShootingEnemy : Enemy
    {
        [Header("=== 基础设置 ===")]
        [Tooltip("子弹预制体")]
        public EnemyBullet enemyBullet;

        [Tooltip("移动速度")]
        public float speed = 2.0f;

        [Header("=== 状态机设置 ===")]
        [Tooltip("追击范围:进入这个距离切换到Wander状态")]
        public float chaseRange = 8f;

        [Tooltip("攻击范围:超出这个距离回到Chase状态")]
        public float attackRange = 10f;

        [Tooltip("Wander状态持续时间(秒)")]
        public float wanderDuration = 1.0f;

        [Header("=== 射击模式 ===")]
        [Tooltip("两次连射之间的间隔(秒)")]
        public float shootInterval = 2.0f;

        [Tooltip("连射中每发子弹间隔(秒)")]
        public float burstInterval = 0.2f;

        [Tooltip("一次连射的子弹数量")]
        public int shotsPerBurst = 3;

        [Tooltip("总共进行几次连射")]
        public int totalBurstCount = 2;

        [Header("=== 散射弹丸设置 ===")]
        [Tooltip("每次射击同时发射的散射弹丸数量")]
        public int scatterBulletCount = 3;

        [Tooltip("散布角度(总角度范围, 单位:度)")]
        [Range(0f, 180f)]
        public float scatterAngle = 45f;

        [Tooltip("是否使用随机散布(false=均匀分布)")]
        public bool useRandomScatter = false;

        [Header("=== 音效设置 ===")]
        [Tooltip("射击音效列表(随机播放)")]
        public List<AudioClip> shootSounds = new List<AudioClip>();

        // 状态枚举
        public enum State
        {
            Idle,       // 待机
            Chase,      // 追踪玩家
            Wander,     // 沿垂直玩家连线的随机方向移动
            Shoot       // 原地射击
        }
        public State currentState = State.Idle;

        // 内部状态
        protected Player player;
        protected Vector3 m_DirectionToPlayer;
        protected float currentWanderTime = 0.0f;
        protected Vector3 wanderDirection;
        protected Coroutine shootCoroutine;

        // Start is called before the first frame update
        void Start()
        {
            // 初始化组件
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            allColliders = GetComponentsInChildren<Collider2D>(true);

            // 获取玩家引用
            if (player == null)
            {
                player = Player.player1;
            }

            // 参数校验
            ValidateParameters();

            // 开始状态
            if (player != null)
            {
                currentState = State.Chase;
            }
        }

        // 参数校验, 防止无效配置
        protected virtual void ValidateParameters()
        {
            if (chaseRange <= 0)
            {
                Debug.LogWarning($"[{gameObject.name}] chaseRange 必须大于0, 已重置为默认值8");
                chaseRange = 8f;
            }

            if (attackRange <= chaseRange)
            {
                Debug.LogWarning($"[{gameObject.name}] attackRange 必须大于 chaseRange, 已重置为 chaseRange + 2");
                attackRange = chaseRange + 2f;
            }

            if (burstInterval <= 0)
            {
                Debug.LogWarning($"[{gameObject.name}] burstInterval 必须大于0, 已重置为默认值0.2");
                burstInterval = 0.2f;
            }

            if (shotsPerBurst < 1)
            {
                Debug.LogWarning($"[{gameObject.name}] shotsPerBurst 至少为1, 已重置为1");
                shotsPerBurst = 1;
            }

            if (totalBurstCount < 1)
            {
                Debug.LogWarning($"[{gameObject.name}] totalBurstCount 至少为1, 已重置为1");
                totalBurstCount = 1;
            }

            if (scatterBulletCount < 1)
            {
                Debug.LogWarning($"[{gameObject.name}] scatterBulletCount 至少为1, 已重置为1");
                scatterBulletCount = 1;
            }

            if (scatterAngle < 0)
            {
                Debug.LogWarning($"[{gameObject.name}] scatterAngle 不能为负数, 已重置为0");
                scatterAngle = 0f;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (isDying) return;

            if (player == null)
            {
                currentState = State.Idle;
                return;
            }

            m_DirectionToPlayer = (player.transform.position - transform.position).normalized;

            switch (currentState)
            {
                case State.Chase:
                    UpdateChaseState();
                    break;

                case State.Wander:
                    UpdateWanderState();
                    break;

                case State.Shoot:
                    UpdateShootState();
                    break;
            }
        }

        // 追击状态更新
        protected virtual void UpdateChaseState()
        {
            UpdateRotate(m_DirectionToPlayer);
            transform.position += m_DirectionToPlayer * speed * Time.deltaTime;

            // 进入追击范围, 切换到Wander
            if (Vector3.Distance(transform.position, player.transform.position) < chaseRange)
            {
                currentState = State.Wander;
                StartWander();
            }
        }

        // Wander状态更新
        protected virtual void UpdateWanderState()
        {
            transform.position += wanderDirection * speed * Time.deltaTime;
            currentWanderTime += Time.deltaTime;

            // 移动时间到, 切换到Shoot
            if (currentWanderTime >= wanderDuration)
            {
                currentState = State.Shoot;
                StartShoot();
            }

            // 玩家跑出攻击范围, 回到Chase
            if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
            {
                currentState = State.Chase;
            }
        }

        // 射击状态更新(可被子类重写)
        protected virtual void UpdateShootState()
        {
            // 射击逻辑由协程处理
        }

        // 开始Wander状态
        protected virtual void StartWander()
        {
            if (player == null) return;
            currentWanderTime = 0.0f;
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            Vector3 perpendicular = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0);
            wanderDirection = Random.Range(0, 2) == 0 ? perpendicular : -perpendicular;
        }

        // 开始Shoot状态
        protected virtual void StartShoot()
        {
            if (shootCoroutine != null)
                StopCoroutine(shootCoroutine);
            shootCoroutine = StartCoroutine(ShootSequence());
        }

        // 射击序列协程
        protected virtual IEnumerator ShootSequence()
        {
            for (int burstIndex = 0; burstIndex < totalBurstCount; burstIndex++)
            {
                // 发射一轮连射(Burst)
                for (int i = 0; i < shotsPerBurst; i++)
                {
                    FireBullet();
                    yield return new WaitForSeconds(burstInterval);
                }

                // 如果不是最后一轮, 等待连射间隔
                if (burstIndex < totalBurstCount - 1)
                {
                    yield return new WaitForSeconds(shootInterval);
                }
                else
                {
                    currentState = State.Wander;
                    StartWander();
                }
            }
        }

        // 发射子弹
        protected virtual void FireBullet()
        {
            if (enemyBullet == null || player == null) return;

            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            UpdateRotate(dirToPlayer);

            // 发射散射弹丸
            for (int i = 0; i < scatterBulletCount; i++)
            {
                float angle = CalculateBulletAngle(i);
                Vector3 bulletDirection = CalculateBulletDirection(dirToPlayer, angle);

                EnemyBullet bullet = Instantiate(enemyBullet, transform.position, Quaternion.identity);
                bullet.direction = bulletDirection;
                bullet.gameObject.SetActive(true);
            }

            // 播放射击音效
            PlayShootSound();
        }

        // 计算子弹角度
        protected virtual float CalculateBulletAngle(int index)
        {
            if (useRandomScatter)   // 随机散布
            {
                return Random.Range(-scatterAngle / 2f, scatterAngle / 2f);
            }
            else   // 均匀分布
            {
                if (scatterBulletCount == 1) return 0f;
                return (-scatterAngle / 2f) + (scatterAngle / (scatterBulletCount - 1f)) * index;
            }
        }

        // 计算子弹方向
        protected virtual Vector3 CalculateBulletDirection(Vector3 baseDirection, float angleDegrees)
        {
            float radian = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                baseDirection.x * Mathf.Cos(radian) - baseDirection.y * Mathf.Sin(radian),
                baseDirection.x * Mathf.Sin(radian) + baseDirection.y * Mathf.Cos(radian),
                0
            ).normalized;
        }

        // 播放射击音效
        protected virtual void PlayShootSound()
        {
            if (shootSounds.Count > 0)
            {
                AudioKitManager.Instance.PlayOneShot(shootSounds[Random.Range(0, shootSounds.Count)], volume: 0.1f);
            }
        }

        // // 死亡序列
        // protected override IEnumerator DeathSequence()
        // {
        //     // 停止射击协程
        //     if (shootCoroutine != null)
        //         StopCoroutine(shootCoroutine);

        //     yield return StartCoroutine(base.DeathSequence());
        // }


        protected override void FixedUpdate()
        {

        }
        public override void OnDestroy()
        {
            if (shootCoroutine != null)
                StopCoroutine(shootCoroutine);
            base.OnDestroy();
        }
    }
}
