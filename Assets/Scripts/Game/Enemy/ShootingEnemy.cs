// 射击型敌人基类 - 支持散射弹丸、连射模式, 可在Unity编辑器中自定义所有参数
// 使用方法:直接挂载到敌人对象上, 配置Inspector参数即可
// 血量、受击、死亡、朝向、寻路、视线检测基础设施在 EnemyBase 中
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

        [Header("=== 转向限速 ===")]
        [Tooltip("瞄准方向旋转速度(度/秒)。玩家绕到身后时子弹不会立刻转过来，防止贴脸瞬时连射")]
        public float turnSpeed = 360f;
        // 实际开火方向：以限速方式从当前方向平滑转向玩家，子弹沿此方向发射
        protected Vector2 aimDirection;
        private bool aimInitialized = false;

        [Header("=== 音效相关设置 ===")]
        [Tooltip("射击音效列表(随机播放)")] public List<AudioClip> shootSounds = new List<AudioClip>();

        // 内部状态
        protected Coroutine shootCoroutine;

        // Start：基类已初始化 spriteRenderer/player/sightBlockingMask，这里只设初始状态
        protected override void Start()
        {
            base.Start();
            if (player != null)
            {
                currentState = State.Chase;
                aimDirection = GetDirectionToPlayer();
                aimInitialized = true;
            }
        }

        // 转向限速：开火方向以 turnSpeed 度/秒平滑转向玩家（而非瞬间指向），
        // 翻转只负责表现朝向；FireBullet 沿平滑后的 aimDirection 发射
        public override void UpdateRotate(Vector3 dirToPlayer)
        {
            if (dirToPlayer.sqrMagnitude < 0.0001f) return;
            if (!aimInitialized)
            {
                aimDirection = dirToPlayer;
                aimInitialized = true;
            }
            float maxStepRadians = turnSpeed * Time.deltaTime * Mathf.Deg2Rad;
            aimDirection = Vector3.RotateTowards(aimDirection, dirToPlayer, maxStepRadians, 0f);

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = aimDirection.x < 0f;
            }
        }

        protected override void Update()
        {
            // 暂停/场景加载期间冻结 AI：视线检测与状态机都不推进，不再启动射击协程
            if (Global.IsGamePaused) return;

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

        // 发射子弹（散射数学复用 EnemyBase.FireScatterBullets）
        protected virtual void FireBullet()
        {
            if (enemyBullet == null || player == null) return;
            UpdateRotate(directionToPlayer);

            // 沿限速平滑后的瞄准方向开火（而不是瞬时指向玩家的方向）
            FireScatterBullets(enemyBullet, aimDirection, scatterBulletCount, scatterAngle, useRandomScatter);

            // 播放射击音效
            PlayShootSound();
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
