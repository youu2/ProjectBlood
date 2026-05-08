// 激光敌人基类 - 支持多种激光效果配置
using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LaserEnemy : Enemy
    {
        [Header("=== 激光设置 ===")]
        [Tooltip("激光线条组件")]
        public LineRenderer laserLine;
        
        [Tooltip("激光宽度")]
        [Range(0.1f, 2f)]
        public float laserWidth = 0.3f;
        
        [Tooltip("激光颜色")]
        public Color laserColor = Color.magenta;
        
        [Tooltip("激光持续时间（秒）")]
        public float laserDuration = 0.8f;

        [Header("=== 攻击设置 ===")]
        [Tooltip("蓄力时间（秒）")]
        public float chargeTime = 1.5f;
        
        [Tooltip("伤害频率（每秒造成伤害次数）")]
        public float damageFrequency = 5f;
        
        [Tooltip("单次伤害值")]
        public float damageAmount = 10f;

        [Header("=== 瞄准设置 ===")]
        [Tooltip("转向速度（度数/秒）")]
        public float rotationSpeed = 180f;
        
        [Tooltip("攻击范围")]
        public float attackRange = 15f;
        
        [Tooltip("追击范围")]
        public float chaseRange = 20f;

        [Header("=== 多束激光设置 ===")]
        [Tooltip("激光束数量")]
        public int laserCount = 1;
        
        [Tooltip("激光扩散角度（总角度范围）")]
        [Range(0f, 180f)]
        public float spreadAngle = 0f;
        
        [Tooltip("是否随机扩散")]
        public bool randomSpread = false;

        [Header("=== 反弹设置 ===")]
        [Tooltip("是否允许反弹")]
        public bool enableBounce = false;
        
        [Tooltip("最大反弹次数")]
        public int maxBounceCount = 2;

        [Header("=== 音效设置 ===")]
        [Tooltip("蓄力音效")]
        public AudioClip chargeSound;
        
        [Tooltip("发射音效")]
        public AudioClip fireSound;
        
        [Tooltip("激光持续音效")]
        public AudioClip laserLoopSound;

        // 状态枚举
        public enum State
        {
            Idle,
            Chase,
            Aim,      // 瞄准蓄力
            Fire      // 发射激光
        }
        public State currentState = State.Idle;

        // 内部状态
        protected Player player;
        protected float chargeProgress = 0f;
        protected Coroutine laserCoroutine;
        protected Coroutine damageCoroutine;
        protected List<Vector3> laserPoints = new List<Vector3>();

        void Start()
        {
            // 初始化组件
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (laserLine == null)
                laserLine = GetComponent<LineRenderer>();
            
            if (laserLine != null)
            {
                laserLine.startWidth = laserWidth;
                laserLine.endWidth = laserWidth;
                laserLine.startColor = laserColor;
                laserLine.endColor = laserColor;
                laserLine.enabled = false;
            }
            
            // 获取玩家引用
            if (player == null)
                player = Player.player1;
            
            // 参数校验
            ValidateParameters();
            
            // LaserEnemy默认不使用翻转，而是直接旋转
            useFlipSprite = false;
            
            // 开始状态
            if (player != null)
                currentState = State.Chase;
        }

        void ValidateParameters()
        {
            if (laserCount < 1) laserCount = 1;
            if (spreadAngle < 0) spreadAngle = 0;
            if (chargeTime < 0) chargeTime = 0;
            if (laserDuration < 0.1f) laserDuration = 0.1f;
            if (damageFrequency < 1f) damageFrequency = 1f;
            if (maxBounceCount < 0) maxBounceCount = 0;
            // if (attackRange > chaseRange) attackRange = chaseRange;
        }

        void Update()
        {
            if (isDying) return;
            
            if (player == null)
            {
                currentState = State.Idle;
                return;
            }
            
            
            switch (currentState)
            {
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Aim:
                    UpdateAim();
                    break;
                case State.Fire:
                    UpdateFire();
                    break;
            }
        }

        void UpdateChase()
        {
            // 追击玩家
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            
            // 平滑转向
            UpdateRotation(dirToPlayer);
            
            // 移动
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;
            
            // 进入攻击范围，开始瞄准
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange)
            {
                currentState = State.Aim;
                StartCoroutine(ChargeCoroutine());
            }
        }

        void UpdateAim()
        {
            // 保持面向玩家
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            UpdateRotation(dirToPlayer);
        }

        void UpdateFire()
        {
            // 发射状态不需要额外处理，FireMultipleLasers中已经在处理转向
        }

        void UpdateRotation(Vector3 direction)
        {
            if (direction.x != 0 || direction.y != 0)
            {
                if (base.useFlipSprite)
                {
                    // 使用翻转方式朝向玩家
                    spriteRenderer.flipX = direction.x < 0;
                }
                else
                {
                    // 直接旋转朝向玩家
                    float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    float currentAngle = transform.eulerAngles.z;
                    
                    float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime / 180f);
                    transform.eulerAngles = new Vector3(0, 0, newAngle);
                }
            }
        }

        IEnumerator ChargeCoroutine()
        {
            // 播放蓄力音效
            if (chargeSound != null)
                AudioKit.PlaySound(chargeSound);
            
            chargeProgress = 0f;
            
            while (chargeProgress < 1f)
            {
                chargeProgress += Time.deltaTime / chargeTime;
                
                // 转向玩家
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                UpdateRotation(dirToPlayer);
                
                // 蓄力期间可以显示预警线
                if (laserLine != null)
                {
                    laserLine.enabled = true;
                    laserLine.startWidth = laserWidth * chargeProgress;
                    laserLine.endWidth = laserWidth * chargeProgress;
                    
                    laserLine.SetPosition(0, transform.position);
                    laserLine.SetPosition(1, transform.position + transform.right * attackRange * chargeProgress);
                    laserLine.startColor = Color.Lerp(Color.red, laserColor, chargeProgress);
                    laserLine.endColor = Color.Lerp(Color.red, laserColor, chargeProgress);
                }
                
                // 检查是否超出攻击范围
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance > attackRange)
                {
                    // 玩家逃出范围，取消蓄力
                    if (laserLine != null)
                        laserLine.enabled = false;
                    currentState = State.Chase;
                    yield break;
                }
                
                yield return null;
            }
            
            // 蓄力完成，开始发射
            StartCoroutine(FireCoroutine());
        }

        IEnumerator FireCoroutine()
        {
            currentState = State.Fire;
            
            // 播放发射音效
            if (fireSound != null)
                AudioKit.PlaySound(fireSound);
            
            // 播放激光持续音效
            if (laserLoopSound != null)
                AudioKit.PlaySound(laserLoopSound);
            
            // 重置激光宽度
            if (laserLine != null)
            {
                laserLine.startWidth = laserWidth;
                laserLine.endWidth = laserWidth;
                laserLine.startColor = laserColor;
                laserLine.endColor = laserColor;
            }
            
            // 开始伤害检测
            damageCoroutine = StartCoroutine(DamageCoroutine());
            
            // 发射多束激光
            yield return StartCoroutine(FireMultipleLasers());
            
            // 停止伤害检测
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
            
            // 淡出激光
            yield return StartCoroutine(FadeOutLaser());
            
            // 重置状态
            laserPoints.Clear();
            currentState = State.Chase;
        }

        IEnumerator FireMultipleLasers()
        {
            float duration = laserDuration;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // 转向玩家
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                UpdateRotation(dirToPlayer);
                
                // 每帧更新激光位置
                for (int i = 0; i < laserCount; i++)
                {
                    float angleOffset = CalculateLaserAngle(i);
                    Vector3 laserDir = RotateVector(transform.right, angleOffset);
                    
                    // 计算激光路径（带反弹）
                    laserPoints.Clear();
                    laserPoints.Add(transform.position);
                    CalculateLaserPath(transform.position, laserDir, maxBounceCount);
                    
                    // 更新激光显示
                    if (laserLine != null && laserPoints.Count >= 2)
                    {
                        laserLine.positionCount = laserPoints.Count;
                        laserLine.SetPositions(laserPoints.ToArray());
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        float CalculateLaserAngle(int index)
        {
            if (laserCount == 1 || spreadAngle <= 0)
                return 0f;
            
            if (randomSpread)
            {
                return Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
            }
            else
            {
                return (-spreadAngle / 2f) + (spreadAngle / (laserCount - 1f)) * index;
            }
        }

        Vector3 RotateVector(Vector3 vector, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                vector.x * Mathf.Cos(rad) - vector.y * Mathf.Sin(rad),
                vector.x * Mathf.Sin(rad) + vector.y * Mathf.Cos(rad),
                0
            ).normalized;
        }

        void CalculateLaserPath(Vector3 start, Vector3 direction, int bouncesLeft)
        {
            if (bouncesLeft < 0) return;
            
            // 只检测墙，敌人伤害单独检测
            RaycastHit2D hit = Physics2D.Raycast(start, direction, Mathf.Infinity, 
                LayerMask.GetMask("Wall"));
            
            if (hit.collider != null)
            {
                laserPoints.Add(hit.point);
                
                // 检测敌人伤害（在激光路径上）
                CheckEnemyHit(start, hit.point);
                
                // 如果可以反弹且撞到墙
                if (enableBounce && bouncesLeft > 0 && hit.collider.CompareTag("Wall"))
                {
                    Vector3 normal = hit.normal;
                    Vector3 reflectedDir = Vector3.Reflect(direction, normal).normalized;
                    CalculateLaserPath((Vector3)hit.point + reflectedDir * 0.1f, reflectedDir, bouncesLeft - 1);
                }
            }
            else
            {
                // 没有碰到墙，延伸到最大距离
                Vector3 endPoint = start + direction * 50f;
                laserPoints.Add(endPoint);
                
                // 检测敌人伤害
                CheckEnemyHit(start, endPoint);
            }
        }

        void CheckEnemyHit(Vector3 start, Vector3 end)
        {
            RaycastHit2D hit = Physics2D.Raycast(start, (end - start).normalized, 
                Vector3.Distance(start, end), LayerMask.GetMask("Enemy"));
            
            if (hit.collider != null)
            {
                // 检查是否是自己
                if (hit.collider.gameObject == gameObject)
                    return;
                
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDying)
                {
                    damageable.TakeDamage(damageAmount);
                }
            }
        }

        IEnumerator DamageCoroutine()
        {
            float interval = 1f / damageFrequency;
            
            while (currentState == State.Fire)
            {
                // 对激光路径上的敌人造成伤害
                for (int i = 0; i < laserPoints.Count - 1; i++)
                {
                    RaycastHit2D hit = Physics2D.Raycast(laserPoints[i], 
                        (laserPoints[i + 1] - laserPoints[i]).normalized,
                        Vector3.Distance(laserPoints[i], laserPoints[i + 1]),
                        LayerMask.GetMask("Enemy"));
                    
                    if (hit.collider != null)
                    {
                        // 检查是否是自己
                        if (hit.collider.gameObject == gameObject)
                            continue;
                        
                        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                        if (damageable != null && !damageable.IsDying)
                        {
                            damageable.TakeDamage(damageAmount);
                        }
                    }
                }
                
                yield return new WaitForSeconds(interval);
            }
        }

        IEnumerator FadeOutLaser()
        {
            if (laserLine == null) yield break;
            
            float duration = 0.2f;
            float elapsed = 0f;
            float startWidth = laserWidth;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float width = Mathf.Lerp(startWidth, 0f, t);
                
                laserLine.startWidth = width;
                laserLine.endWidth = width;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            laserLine.enabled = false;
        }

        protected override IEnumerator DeathSequence()
        {
            // 停止所有协程
            if (laserCoroutine != null)
                StopCoroutine(laserCoroutine);
            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);
            
            // 隐藏激光
            if (laserLine != null)
                laserLine.enabled = false;
            
            yield return StartCoroutine(base.DeathSequence());
        }

        protected override void FixedUpdate()
        {
            // 空实现，使用自己的状态机
        }
    }
}
