// Titan Boss：双管霰弹枪 Boss
// 阶段1：追踪 → 双发射击 → 换弹
// 阶段2（半血触发）：追踪 / 爆发推进 / 愤怒双发 / 环形射击
// 复用 ShootingEnemy 的散射参数（霰弹），环射自己写协程（螺旋弹幕）
using System.Collections;
using UnityEngine;

namespace ProjectBlood
{
    // Titan 的所有行为状态
    public enum BossState
    {
        Idle,            // 待机，等玩家进房
        Chase,           // 一阶段追踪
        Attack,          // 一阶段双发霰弹
        Reload,          // 一阶段换弹
        AngryChase,      // 二阶段追踪（更快）
        AngryAttack,     // 二阶段双发霰弹（更快）
        AngryReload,     // 二阶段换弹（更短）
        Dash,            // 爆发推进（冲锋接近玩家）
        RingShot,        // 无差别环形射击
        PhaseTransition  // 转阶段演出（由基类控制）
    }

    public class TitanBoss : BossBase
    {
        [Header("=== 一阶段参数 ===")]
        [Tooltip("一阶段移动速度")] public float phase1MoveSpeed = 2.0f;
        [Tooltip("一阶段两次射击间隔（秒）")] public float phase1ShootInterval = 0.8f;
        [Tooltip("一阶段换弹时间（秒）")] public float phase1ReloadTime = 2.0f;

        [Header("=== 二阶段参数 ===")]
        [Tooltip("二阶段移动速度")] public float phase2MoveSpeed = 3.5f;
        [Tooltip("二阶段两次射击间隔（秒）")] public float phase2ShootInterval = 0.3f;
        [Tooltip("二阶段换弹时间（秒）")] public float phase2ReloadTime = 1.5f;

        [Header("=== 攻击范围 ===")]
        // 注意：不能用 attackRange 这个名字，Enemy 基类已经有同名字段，
        // 父子类同名序列化字段会报 "serialized multiple times" 错误
        [Tooltip("玩家进入这个距离后 Boss 会开火")] public float bossAttackRange = 10f;

        [Header("=== 双管霰弹散射参数 ===")]
        [Tooltip("每次射击喷出的弹丸数量")] public int shotgunPelletCount = 5;
        [Tooltip("霰弹散射总角度（度）")] public float shotgunSpreadAngle = 30f;

        [Header("=== 环形射击参数 ===")]
        [Tooltip("环形射击冷却时间（秒）")] public float ringShotCooldown = 10f;
        [Tooltip("一共射几圈")] public int ringCount = 3;
        [Tooltip("每圈有多少发子弹")] public int ringBulletCount = 16;
        [Tooltip("两圈之间的间隔（秒）")] public float ringInterval = 0.8f;
        [Tooltip("每圈的旋转偏移角度（形成螺旋感）")] public float ringSpiralOffset = 15f;

        [Header("=== 爆发推进参数 ===")]
        [Tooltip("推进冷却时间（秒）")] public float dashCooldown = 8f;
        [Tooltip("推进速度")] public float dashSpeed = 12f;
        [Tooltip("推进持续时间（秒），距离 = 速度 × 时长")] public float dashDuration = 0.4f;

        // 当前 Boss 状态（用独立字段，避免和 Enemy 基类的 currentState 混淆）
        public BossState currentBossState = BossState.Idle;

        // 两个技能的冷却计时器
        private float ringShotCooldownTimer = 0f;
        private float dashCooldownTimer = 0f;

        // 记录换弹前的移速，换弹结束后恢复
        private float moveSpeedBeforeReload;

        protected override void Awake()
        {
            base.Awake();
            // 初始状态：待机
            currentBossState = BossState.Idle;
        }

        protected override void Start()
        {
            // 先跑基类初始化：拿玩家引用、设视线遮挡层为 Wall
            base.Start();

            // 视线检测间隔按设计文档设为 0.3 秒
            sightCheckInterval = 0.3f;

            // 霰弹散射参数复用 ShootingEnemy 的字段
            scatterBulletCount = shotgunPelletCount;
            scatterAngle = shotgunSpreadAngle;
        }

        protected override void Update()
        {
            // 玩家没了就待机
            if (Player.player1 == null)
            {
                currentBossState = BossState.Idle;
                return;
            }
            if (isDead) return;

            // 视线检测只在追踪状态做，攻击时不刷（省性能）
            if (currentBossState == BossState.Chase || currentBossState == BossState.AngryChase)
            {
                sightCheckTimer -= Time.deltaTime;
                if (sightCheckTimer <= 0f)
                {
                    sightCheckTimer = sightCheckInterval;
                    cachedLineOfSight = PerformLineOfSightCheck();
                }
            }

            directionToPlayer = GetDirectionToPlayer();
            UpdateRotate(directionToPlayer);
            float distanceToPlayer = GetDistanceToPlayer();

            // 冷却倒计时
            if (ringShotCooldownTimer > 0f) ringShotCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

            // 根据当前状态执行对应逻辑
            switch (currentBossState)
            {
                case BossState.Idle:
                    UpdateIdle();
                    break;
                case BossState.Chase:
                    UpdateChase(distanceToPlayer, phase1MoveSpeed);
                    break;
                case BossState.AngryChase:
                    UpdateAngryChase(distanceToPlayer, phase2MoveSpeed);
                    break;
                // 攻击 / 换弹 / 推进 / 环射 都由协程驱动，Update 里不做事
                case BossState.Attack:
                case BossState.Reload:
                case BossState.AngryAttack:
                case BossState.AngryReload:
                case BossState.Dash:
                case BossState.RingShot:
                case BossState.PhaseTransition:
                    break;
            }
        }

        // 待机：等玩家进入 Boss 房（房间状态变成 Battle）就开始追踪
        private void UpdateIdle()
        {
            if (Room != null && Room.roomState == Room.RoomState.Battle)
            {
                currentBossState = BossState.Chase;
            }
        }

        // 一阶段追踪：朝玩家走，进攻击范围 + 能看到就开火
        private void UpdateChase(float distanceToPlayer, float speed)
        {
            MoveAlongPath(speed);

            // 转阶段演出期间不触发新动作
            if (isInPhaseTransition) return;

            if (distanceToPlayer <= bossAttackRange && cachedLineOfSight)
            {
                StartCoroutine(AttackSequence(angry: false));
            }
        }

        // 二阶段追踪：更快；攻击范围内开火，范围外且 CD 好了就冲锋
        private void UpdateAngryChase(float distanceToPlayer, float speed)
        {
            MoveAlongPath(speed);

            // 转阶段演出期间不触发新动作
            if (isInPhaseTransition) return;

            if (distanceToPlayer <= bossAttackRange && cachedLineOfSight)
            {
                StartCoroutine(AttackSequence(angry: true));
            }
            else if (distanceToPlayer > bossAttackRange && cachedLineOfSight && dashCooldownTimer <= 0f)
            {
                StartCoroutine(DashSequence());
            }
        }

        // 双发霰弹：打两枪，间隔由阶段决定，打完进入换弹
        private IEnumerator AttackSequence(bool angry)
        {
            currentBossState = angry ? BossState.AngryAttack : BossState.Attack;
            float interval = angry ? phase2ShootInterval : phase1ShootInterval;

            // 双管：快速射两次
            for (int i = 0; i < 2; i++)
            {
                FireShotgun();
                yield return new WaitForSeconds(interval);
            }

            // 打完换弹
            StartCoroutine(ReloadSequence(angry));
        }

        // 发射一次霰弹（复用 ShootingEnemy 的散射逻辑）
        private void FireShotgun()
        {
            // 确保散射参数是最新的
            scatterBulletCount = shotgunPelletCount;
            scatterAngle = shotgunSpreadAngle;
            FireBullet();
        }

        // 换弹：原地停一段时间，结束后根据阶段决定下一步
        private IEnumerator ReloadSequence(bool angry)
        {
            currentBossState = angry ? BossState.AngryReload : BossState.Reload;
            float reloadTime = angry ? phase2ReloadTime : phase1ReloadTime;

            // 换弹时停下
            moveSpeedBeforeReload = moveSpeed;
            moveSpeed = 0f;

            yield return new WaitForSeconds(reloadTime);

            moveSpeed = moveSpeedBeforeReload;

            if (angry)
            {
                // 二阶段换弹结束：玩家在攻击范围内 + 环射没在 CD → 环射，否则继续追
                if (GetDistanceToPlayer() <= bossAttackRange && ringShotCooldownTimer <= 0f)
                {
                    StartRingShot();
                }
                else
                {
                    currentBossState = BossState.AngryChase;
                }
            }
            else
            {
                // 一阶段换弹结束：回去追
                currentBossState = BossState.Chase;
            }
        }

        // 爆发推进：向前冲一段，结束后立即接一次愤怒双发
        private IEnumerator DashSequence()
        {
            currentBossState = BossState.Dash;
            dashCooldownTimer = dashCooldown;

            // 朝玩家方向冲
            Vector3 dashDir = directionToPlayer.normalized;
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                transform.position += dashDir * dashSpeed * Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 冲完立刻愤怒双发，打完自动进换弹
            yield return StartCoroutine(AttackSequence(angry: true));
        }

        // 环形射击：360 度射 3 圈，每圈带旋转偏移形成螺旋
        protected override void StartRingShot()
        {
            StartCoroutine(RingShotSequence());
        }

        private IEnumerator RingShotSequence()
        {
            currentBossState = BossState.RingShot;
            ringShotCooldownTimer = ringShotCooldown;

            // 环射时停下
            moveSpeedBeforeReload = moveSpeed;
            moveSpeed = 0f;

            for (int ring = 0; ring < ringCount; ring++)
            {
                FireRing(ring);
                yield return new WaitForSeconds(ringInterval);
            }

            moveSpeed = moveSpeedBeforeReload;

            // 环射结束 → 愤怒换弹
            StartCoroutine(ReloadSequence(angry: true));
        }

        // 发射一圈子弹，ringIndex 用于算螺旋偏移
        private void FireRing(int ringIndex)
        {
            if (enemyBullet == null) return;

            float angleStep = 360f / ringBulletCount;
            // 每圈转一点角度，三圈错开就有螺旋感
            float baseOffset = ringIndex * ringSpiralOffset;

            for (int i = 0; i < ringBulletCount; i++)
            {
                float angle = i * angleStep + baseOffset;
                // 把角度转成方向向量
                Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;

                EnemyBullet bullet = Instantiate(enemyBullet, transform.position, Quaternion.identity);
                bullet.direction = dir;
                bullet.gameObject.SetActive(true);
            }
        }

        // 沿寻路路径移动（复用 Enemy 基类的 A* 寻路结果）
        private void MoveAlongPath(float speed)
        {
            RecomputePath();

            Vector3 moveDir;
            if (movePath.Count > 0)
            {
                var next = movePath[^1];
                if (next != null && next.Coords != null)
                {
                    var pos = next.Coords.Position;
                    var target = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);
                    var toTarget = target - transform.position;
                    // 走到当前格子就直接朝玩家，下帧路径会更新
                    if (new Vector2(toTarget.x, toTarget.y).sqrMagnitude <= 0.4f * 0.4f)
                    {
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
                // 没寻到路就直线朝玩家
                moveDir = directionToPlayer;
            }

            transform.position += moveDir * speed * Time.deltaTime;
        }

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }
    }
}
