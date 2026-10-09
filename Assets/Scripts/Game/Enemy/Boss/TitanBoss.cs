// Titan Boss：双管霰弹枪 Boss
// 阶段1：追踪 → 双发射击 → 换弹
// 阶段2（半血触发）：追踪 / 爆发推进 / 愤怒双发 / 环形射击
// 散射和环射均自己实现（BossBase 不再继承 ShootingEnemy，避免 Inspector 字段污染）
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

    public partial class TitanBoss : BossBase
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

        [Header("=== 爆发推进参数 ===")]
        [Tooltip("推进冷却时间（秒）")] public float dashCooldown = 8f;
        [Tooltip("推进速度")] public float dashSpeed = 12f;
        [Tooltip("推进持续时间（秒），距离 = 速度 × 时长")] public float dashDuration = 0.4f;

        [Header("=== 音效设置 ===")]
        [Tooltip("霰弹射击音效")] public AudioClip shootSound;
        [Tooltip("爆发推进音效")] public AudioClip dashSound;
        [Tooltip("转二阶段音效")] public AudioClip phaseTwoSound;
        [Tooltip("环形射击音效")] public AudioClip ringShotSound;

        // Animator 参数（BossAnimator / WeaponAnimator 在 Designer 文件中序列化赋值）
        private static readonly int ParamIsMoving = Animator.StringToHash("TitanIsMoving");
        private static readonly int ParamShot = Animator.StringToHash("TitanShot");

        // 当前 Boss 状态（用独立字段，避免和 Enemy 基类的 currentState 混淆）
        public BossState currentBossState = BossState.Idle;

        // 两个技能的冷却计时器
        private float ringShotCooldownTimer = 0f;
        private float dashCooldownTimer = 0f;

        // 当前平滑后的瞄准方向（用于 Arm 旋转与弹道方向，配合 rotationSpeed 实现非瞬间锁敌）
        private Vector3 currentAimDir = Vector3.right;

        // 记录换弹前的移速，换弹结束后恢复
        private float moveSpeedBeforeReload;

        protected override void Awake()
        {
            base.Awake();
            // 初始状态：待机
            currentBossState = BossState.Idle;

            // 基类 Awake 里 GetComponentInChildren<SpriteRenderer>() 按层级序会最先拿到枪的 sprite，
            // 重指到身体，让转阶段变红演出作用在最显眼的 Body 上（朝向翻转不走 flipX，见 UpdateRotate 重写）
            spriteRenderer = Face;

        }

        // Boss 是分部件帧动画，基类 flipX 单个 sprite 没意义，改为翻转整个 WholeBody 节点
        public override void UpdateRotate(Vector3 dirToPlayer)
        {
            if (dirToPlayer.x == 0) return;
            if (WholeBody == null) return;

            Vector3 bodyScale = WholeBody.localScale;
            bodyScale.x = dirToPlayer.x < 0 ? -Mathf.Abs(bodyScale.x) : Mathf.Abs(bodyScale.x);
            WholeBody.localScale = bodyScale;
        }

        protected override void Start()
        {
            // 先跑基类初始化：拿玩家引用、设视线遮挡层为 Wall
            base.Start();

            // 视线检测间隔按设计文档设为 0.3 秒
            sightCheckInterval = 0.3f;

            // 散射参数直接用本类的 shotgunPelletCount / shotgunSpreadAngle，
            // 不再复用 ShootingEnemy 的字段（BossBase 不再继承 ShootingEnemy）

            // 初始瞄准方向直接对准玩家，避免第一帧 Arm 从右方硬转过去
            if (Player.player1 != null)
            {
                currentAimDir = (Player.player1.transform.position - transform.position).normalized;
            }
        }

        protected override void Update()
        {
            // 暂停/场景加载期间冻结 Boss AI：状态机、冷却、寻路、冲锋/霰弹/环射协程一律不启动
            if (Global.IsGamePaused) return;

            // 玩家没了就待机
            if (Player.player1 == null)
            {
                currentBossState = BossState.Idle;
                SetMoving(false);
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

            // 非待机状态下每帧更新身体朝向和武器瞄准；Idle 时保持默认朝向
            if (currentBossState != BossState.Idle)
            {
                UpdateRotate(directionToPlayer);
                AimWeaponAtPlayer();
            }
        }

        // 待机：等玩家进入 Boss 房（房间状态变成 Battle）就开始追踪
        private void UpdateIdle()
        {
            if (Room != null && Room.roomState == Room.RoomState.Battle)
            {
                currentBossState = BossState.Chase;
                SetMoving(true);
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

            // 攻击时站定，身体切回 Idle
            SetMoving(false);

            // 双管：快速射两次，每次触发武器 Shot 动画
            for (int i = 0; i < 2; i++)
            {
                FireShotgun();
                PlayBossSfx(shootSound);
                TriggerShot();
                yield return new WaitForSeconds(interval);
            }

            // 打完换弹
            StartCoroutine(ReloadSequence(angry));
        }

        // 发射一次霰弹（散射数学复用 EnemyBase.FireScatterBullets）
        private void FireShotgun()
        {
            if (enemyBullet == null || player == null) return;
            UpdateRotate(directionToPlayer);

            // 子弹从枪口 ShotPoint 射出，方向使用平滑后的 currentAimDir（而非瞬间指向玩家），
            // 保证弹道与枪口 Arm 实际朝向一致，配合 rotationSpeed 让玩家可走位躲避
            Vector3 spawnPos = ShotPoint != null ? ShotPoint.position : transform.position;
            Vector3 fireDir = currentAimDir.normalized;
            FireScatterBullets(enemyBullet, fireDir, shotgunPelletCount, shotgunSpreadAngle, spawnPos: spawnPos);
        }

        // 换弹：原地停一段时间，结束后根据阶段决定下一步
        private IEnumerator ReloadSequence(bool angry)
        {
            currentBossState = angry ? BossState.AngryReload : BossState.Reload;
            float reloadTime = angry ? phase2ReloadTime : phase1ReloadTime;

            // 换弹时停下，身体播放 Idle 动画
            moveSpeedBeforeReload = moveSpeed;
            moveSpeed = 0f;
            SetMoving(false);

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
                    SetMoving(true);
                }
            }
            else
            {
                // 一阶段换弹结束：回去追
                currentBossState = BossState.Chase;
                SetMoving(true);
            }
        }

        // 爆发推进：向前冲一段，结束后立即接一次愤怒双发
        private IEnumerator DashSequence()
        {
            currentBossState = BossState.Dash;
            dashCooldownTimer = dashCooldown;
            PlayBossSfx(dashSound);
            SetMoving(true);

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

        // 转二阶段：先播放转阶段音效，再走基类的打断协程 + 变红演出
        protected override void StartPhaseTwo()
        {
            // 演出期间原地不动，身体切回 Idle
            SetMoving(false);
            PlayBossSfx(phaseTwoSound);
            base.StartPhaseTwo();
        }

        // 播放 Boss 音效（未在 Inspector 配置时静默跳过）
        private void PlayBossSfx(AudioClip clip, float volume = 1.0f)
        {
            if (clip != null)
            {
                AudioKitManager.Instance.PlayOneShot(clip, volume);
            }
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

            // 环射时停下，身体播放 Idle
            moveSpeedBeforeReload = moveSpeed;
            moveSpeed = 0f;
            SetMoving(false);

            for (int ring = 0; ring < ringCount; ring++)
            {
                FireRing(ring);
                PlayBossSfx(ringShotSound, 0.6f);
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
            // 环射同样从枪口射出
            Vector3 spawnPos = transform.position;
            FireScatterBullets(enemyBullet, directionToPlayer, ringBulletCount, 360f, spawnPos: spawnPos);
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

        // 每帧旋转 Arm 使枪指向玩家；瞄向左半边时翻转武器（scale.y 取反），避免枪身倒置
        // 加入 rotationSpeed 转向速度，Arm 不再瞬间锁敌，玩家可通过高速走位让枪口跟不上
        private void AimWeaponAtPlayer()
        {
            if (Arm == null || Player.player1 == null) return;

            Vector3 targetDir = Player.player1.transform.position - Arm.position;

            // 玩家几乎贴在 Arm 上时不更新瞄准，避免目标方向为零导致 RotateTowards 异常
            if (targetDir.sqrMagnitude > 0.0001f)
            {
                // 用 RotateTowards 按 rotationSpeed（度/秒）逐步转向玩家，
                // maxDelta 用弧度：rotationSpeed * Deg2Rad * deltaTime
                currentAimDir = Vector3.RotateTowards(
                    currentAimDir,
                    targetDir,
                    rotationSpeed * Mathf.Deg2Rad * Time.deltaTime,
                    0f);
            }

            float angle = Mathf.Atan2(currentAimDir.y, currentAimDir.x) * Mathf.Rad2Deg;
            Arm.rotation = Quaternion.Euler(0f, 0f, angle);

            if (WeaponAnimator != null)
            {
                Vector3 weaponScale = WeaponAnimator.transform.localScale;
                weaponScale.y = targetDir.x < 0f ? -Mathf.Abs(weaponScale.y) : Mathf.Abs(weaponScale.y);
                WeaponAnimator.transform.localScale = weaponScale;
            }
        }

        // 设置身体是否在移动（驱动 TitanIdle ↔ TitanMove 转换）
        private void SetMoving(bool isMoving)
        {
            if (BossAnimator != null)
            {
                BossAnimator.SetBool(ParamIsMoving, isMoving);
            }
        }

        // 触发武器射击动画（TitanWeaponIdle → TitanShot，播完由 Exit Time 自动回到 Idle）
        private void TriggerShot()
        {
            if (WeaponAnimator != null)
            {
                WeaponAnimator.SetTrigger(ParamShot);
            }
        }
    }
}
