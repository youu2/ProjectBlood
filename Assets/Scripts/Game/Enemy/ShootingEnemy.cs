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
        public EnemyBullet enemyBullet;
        [Header("=== 连射模式设置 ===")]

        [Tooltip("总共进行几次连射")] public int totalBurstCount = 2;
        [Tooltip("两次连射之间的间隔(秒)")] public float shootInterval = 2.0f;
        [Tooltip("一次连射的子弹数量")] public int shotsPerBurst = 3;
        [Tooltip("连射中每发子弹间隔(秒)")] public float burstInterval = 0.2f;

        [Header("=== 散射模式设置 ===")]
        [Tooltip("每次射击同时发射的散射弹丸数量,1表示不散射")] public int scatterBulletCount = 3;

        [Tooltip("散布角度(总角度范围, 单位:度)")][Range(0f, 360f)] public float scatterAngle = 45f;

        [Tooltip("是否使用随机散布(false=均匀分布)")] public bool useRandomScatter = false;

        [Header("=== 音效相关设置 ===")]
        [Tooltip("射击音效列表(随机播放)")] public List<AudioClip> shootSounds = new List<AudioClip>();

        // 内部状态
        protected Player player;
        protected Coroutine shootCoroutine;

        [Header("=== 视线检测设置 ===")]
        [Tooltip("射线检测间隔时间(秒), 越小越精确但性能开销越大")] public float sightCheckInterval = 0.5f;
        [Tooltip("遮挡视线的Layer mask(默认0=自动使用Wall层, 只被墙体遮挡, 穿透粒子/掉落物)")] public LayerMask sightBlockingMask = 0;

        // 射线检测缓存(每sightCheckInterval秒刷新一次, 攻击状态期间不刷新)
        private float sightCheckTimer = 0f;
        private bool cachedLineOfSight = false;

        // Start is called before the first frame update
        void Start()
        {
            // 初始化组件
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // 获取玩家引用
            if (player == null)
            {
                player = Player.player1;
            }

            // 视线遮挡Layer未配置时自动使用Wall层
            if (sightBlockingMask == 0)
            {
                sightBlockingMask = LayerMask.GetMask("Wall");
            }

            // 开始状态
            if (player != null)
            {
                currentState = State.Chase;
            }
        }

        protected override void Update()
        {
            // 仅在追踪/游走状态启用射线检测, 攻击状态不执行(避免攻击协程期间的性能开销)
            if (currentState == State.Chase || currentState == State.Wander)
            {
                sightCheckTimer -= Time.deltaTime;
                if (sightCheckTimer <= 0f)
                {
                    sightCheckTimer = sightCheckInterval;
                    cachedLineOfSight = PerformLineOfSightCheck();
                }
            }

            base.Update();
        }

        protected override void UpdateFire(float distanceToPlayer)
        {
            // 射击状态无逐帧更新逻辑，仅在StartFire()时触发攻击
        }

        // 返回缓存的视线检测结果(由基类UpdateChase/UpdateWander的状态切换条件引用)
        protected override bool HasLineOfSightToPlayer() => cachedLineOfSight;

        /// <summary>从敌人位置到玩家位置进行射线检测, 仅被墙体遮挡</summary>
        protected virtual bool PerformLineOfSightCheck()
        {
            if (player == null) return false;
            Vector2 origin = transform.position;
            Vector2 target = player.transform.position;
            RaycastHit2D hit = Physics2D.Linecast(origin, target, sightBlockingMask);
            return hit.collider == null; // 没有命中墙体 = 视线无遮挡
        }

        // 开始Fire状态
        protected override void StartFire()
        {
            base.StartFire();
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
                    sightCheckTimer = 0f; // 强制下一帧立即检测视线, 用于决定是否切回追踪
                }
            }
        }

        // 发射子弹
        protected virtual void FireBullet()
        {
            if (enemyBullet == null || player == null) return;
            UpdateRotate(directionToPlayer);

            // 发射散射弹丸
            for (int i = 0; i < scatterBulletCount; i++)
            {
                float angle = CalculateBulletAngle(i);
                Vector3 bulletDirection = CalculateBulletDirection(directionToPlayer, angle);

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

        public override void OnDestroy()
        {
            if (shootCoroutine != null)
                StopCoroutine(shootCoroutine);
            base.OnDestroy();
        }
    }
}