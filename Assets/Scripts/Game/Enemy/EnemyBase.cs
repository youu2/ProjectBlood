// 敌人基类：所有敌人（含 Boss）的公共底座
// 负责血量、受击、死亡、朝向、寻路、IDamageable 接口实现、视线检测基础设施
// 子类（Enemy/ShootingEnemy/BossBase）各自实现自己的 AI 状态机
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 敌人基类：提取自原 Enemy，作为所有敌人的公共父类。
    /// 普通敌人继承 Enemy（AI 状态机），Boss 继承 BossBase（直接继承本类）。
    /// 这样 Boss 不再被迫继承 ShootingEnemy 的所有字段，Inspector 更干净。
    /// </summary>
    public class EnemyBase : ViewController, IDamageable
    {
        [Header("=== 敌人基础设置 ===")]
        [SerializeField] protected SpriteRenderer body;
        // 尸体渲染器（存档装饰痕迹等外部系统读取用）
        public SpriteRenderer DeadBody => body;
        protected SpriteRenderer spriteRenderer;          // 用于朝向控制
        [SerializeField] public float moveSpeed = 2.0f;   // 移动速度（Boss 转阶段时也会改这个值）
        public float currentHealth;
        public float maxHealth = 100.0f;                   // 敌人总生命值，记录初始血量用于吸血 PB 换算
        [SerializeField] protected float Damage = 5.0f;   // 用于直接造成伤害的敌人, 子弹碰撞在子弹脚本中处理
        protected Vector3 directionToPlayer;              // 敌人朝向玩家的方向
        [Tooltip("是否使用翻转来朝向玩家（关闭则直接旋转）")]
        public bool useFlipSprite = true;
        public List<PathSearchingHelper.NodeBase<Vector3Int>> movePath = new();

        [Header("=== 视线检测设置 ===")]
        [Tooltip("射线检测间隔时间(秒), 越小越精确但性能开销越大")]
        public float sightCheckInterval = 0.5f;
        [Tooltip("遮挡视线的Layer mask(默认0=自动使用Wall层, 只被墙体遮挡, 穿透粒子/掉落物)")]
        public LayerMask sightBlockingMask = 0;

        // 射线检测缓存(每sightCheckInterval秒刷新一次, 攻击状态期间不刷新)
        protected float sightCheckTimer = 0f;
        protected bool cachedLineOfSight = false;

        // 内部状态：玩家引用（视线检测、朝向都用得到）
        protected Player player;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            currentHealth = maxHealth;
            movePath.Clear();
        }

        // Start：初始化玩家引用 + 视线遮挡层。子类可重写并调用 base.Start()
        protected virtual void Start()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (player == null)
                player = Player.player1;
            if (sightBlockingMask == 0)
                sightBlockingMask = LayerMask.GetMask("Wall");
        }

        // 空的 Update，子类重写以驱动各自的状态机
        protected virtual void Update() { }

        /// <summary>是否对玩家有直接视线(可被墙体遮挡)。默认true, 子类(如ShootingEnemy)重写以实现射线检测</summary>
        protected virtual bool HasLineOfSightToPlayer() => true;

        /// <summary>从敌人位置到玩家位置进行射线检测, 仅被墙体遮挡</summary>
        protected virtual bool PerformLineOfSightCheck()
        {
            if (player == null) return false;
            Vector2 origin = transform.position;
            Vector2 target = player.transform.position;
            RaycastHit2D hit = Physics2D.Linecast(origin, target, sightBlockingMask);
            return hit.collider == null; // 没有命中墙体 = 视线无遮挡
        }

        /// <summary>
        /// 发射一梭子散射弹丸（纯数学工具，参数由调用方传入，不依赖任何序列化字段）。
        /// ShootingEnemy 的 FireBullet 和 TitanBoss 的 FireShotgun 共用这段散射数学，
        /// 避免重复代码，同时不引入新的 Inspector 字段。
        /// </summary>
        protected void FireScatterBullets(EnemyBullet bulletPrefab, Vector3 baseDirection, int pelletCount, float spreadAngle, bool randomScatter = false)
        {
            if (bulletPrefab == null) return;

            for (int i = 0; i < pelletCount; i++)
            {
                // 计算第 i 发的偏转角度：随机散布 or 均匀分布
                float angle;
                if (randomScatter)
                {
                    angle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                }
                else if (pelletCount == 1)
                {
                    angle = 0f;
                }
                else
                {
                    angle = (-spreadAngle / 2f) + (spreadAngle / (pelletCount - 1f)) * i;
                }

                // 把偏转角绕 Z 轴旋到基础方向上
                float radian = angle * Mathf.Deg2Rad;
                Vector3 bulletDirection = new Vector3(
                    baseDirection.x * Mathf.Cos(radian) - baseDirection.y * Mathf.Sin(radian),
                    baseDirection.x * Mathf.Sin(radian) + baseDirection.y * Mathf.Cos(radian),
                    0
                ).normalized;

                EnemyBullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                bullet.direction = bulletDirection;
                bullet.gameObject.SetActive(true);
            }
        }

        /// <summary>每帧调用：以当前敌人/玩家位置重新计算一次 A* 路径，写入 movePath（失败时为空）。
        /// 子类（Enemy 的 UpdateChase、TitanBoss 的 MoveAlongPath）复用此方法。</summary>
        protected void RecomputePath()
        {
            if (Room == null || Room.PathSearchingGrid == null) return;
            if (MapController.instance == null || MapController.instance.wallTilemap == null
                || MapController.instance.wallTilemap.layoutGrid == null) return;
            if (Player.player1 == null) return;

            var grid = MapController.instance.wallTilemap.layoutGrid;
            var selfCell = grid.WorldToCell(transform.position);
            var playerCell = grid.WorldToCell(Player.player1.transform.position);

            var startNode = Room.PathSearchingGrid[selfCell.x, selfCell.y];
            var endNode = Room.PathSearchingGrid[playerCell.x, playerCell.y];

            if (startNode == null || endNode == null)
            {
                movePath.Clear();
                return;
            }

            PathSearchingHelper.SearchPath(startNode, endNode, movePath);
        }

        protected Vector3 GetDirectionToPlayer()
        {
            if (Player.player1 == null)
                return transform.right;
            return (Player.player1.transform.position - transform.position).normalized;
        }

        protected float GetDistanceToPlayer()
        {
            return Vector3.Distance(transform.position, Player.player1.transform.position);
        }

        // 更新朝向面向玩家
        public virtual void UpdateRotate(Vector3 dirToPlayer)
        {
            if (dirToPlayer.x == 0 && dirToPlayer.y == 0) return;
            if (spriteRenderer != null)
            {
                if (useFlipSprite)
                {
                    spriteRenderer.flipX = dirToPlayer.x < 0;
                }
                else
                {
                    float targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                    float currentAngle = transform.eulerAngles.z;
                    float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, 180f * Time.deltaTime / 180f);
                    transform.eulerAngles = new Vector3(0, 0, newAngle);
                }
            }
        }

        // 敌人受伤（virtual，子类如 Boss 可重写以加入阶段切换等逻辑）
        public virtual void TakeDamage(float damage, Vector2 HitDir)
        {
            AudioKitManager.Instance.PlayOneShot("Torch Impact 2", volume: 0.5f);
            FxManager.PlayEnemyHurtFX(transform.Position2D());
            FxManager.DrawEnemyBlood(transform.Position2D(), Room);
            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                Death(HitDir);
            }
        }

        protected virtual void Death(Vector2 HitDir)
        {
            AudioKitManager.Instance.PlayOneShot("KillSFX", volume: 0.6f);
            // 血印系统：击杀单位事件（敌人伤害均来自玩家武器）
            BloodSigilState.NotifyUnitKilled();
            Global.GenerateDrops(this.gameObject);
            if (Room != null)
            {
                Room.GetEnemies().Remove(this);
            }
            moveSpeed = 0f;

            FxManager.SpawnEnemyBody(body, transform.Position2D(), HitDir);

            Global.currentNum.Value -= 1;
            this.DestroyGameObjGracefully();
        }

        // IDamageable 接口实现
        public float HitDamage { get => Damage; }
        public GameObject GameObject { get => gameObject; }
        public Room Room { get; set; }
        public float CurrentHealth { get => currentHealth; }
        public float MaxHealth { get => maxHealth; }

        public virtual void OnDestroy()
        {
            if (Room != null)
            {
                Room.GetEnemies().Remove(this);
            }
        }
    }
}
