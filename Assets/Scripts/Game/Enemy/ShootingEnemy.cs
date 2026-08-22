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

            // 开始状态
            if (player != null)
            {
                currentState = State.Chase;
            }
        }

        protected override void UpdateFire(float distanceToPlayer)
        {
            // 射击状态无逐帧更新逻辑，仅在StartFire()时触发攻击
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