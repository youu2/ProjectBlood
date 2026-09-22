// Boss 通用基类：所有 Boss 都继承它
// 负责：血条数据同步到 Global、二阶段切换、Boss 专属掉落、死亡后显示传送门
// 子类（如 TitanBoss）只需实现自己的状态机和 StartRingShot 即可
using System.Collections;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public abstract class BossBase : EnemyBase
    {
        [Header("=== Boss 子弹 ===")]
        [Tooltip("Boss 发射的子弹预制体")]
        public EnemyBullet enemyBullet;

        [Header("=== Boss 通用设置 ===")]
        [Tooltip("传送门引用：Boss 死亡后显示，供玩家进入下一关")]
        public GameObject portal;

        [Tooltip("死亡时掉落金币的数量")]
        public int coinDropCount = 20;

        [Tooltip("死亡时掉落 dirtyBlood 的数量")]
        public int dirtyBloodDropCount = 5;

        [Header("=== 阶段切换设置 ===")]
        [Tooltip("进入二阶段的血量百分比（0.5 = 半血）")]
        [Range(0f, 1f)] public float phaseTwoHpPercent = 0.5f;

        [Tooltip("转阶段演出时长（秒），期间 Boss 原地不动、颜色逐渐变红")]
        public float phaseTransitionDuration = 1f;

        // 是否已进入二阶段（防止重复触发）
        protected bool isPhaseTwo = false;
        // 是否已死亡（防止重复触发死亡逻辑）
        protected bool isDead = false;
        // 是否正在播放转阶段演出（期间 Update 不应再触发新的攻击/推进）
        protected bool isInPhaseTransition = false;

        // 记录转阶段前的移速，演出结束后恢复
        protected float speedBeforeTransition;

        /// <summary>
        /// 二阶段开始事件，UI / 动画可以订阅它做转阶段演出
        /// </summary>
        public event System.Action OnPhaseTwoStarted;

        protected override void Awake()
        {
            base.Awake();

            // 把 Boss 血量同步到 Global，让 UI 血条能拿到数据
            Global.BossMaxHp.Value = maxHealth;
            Global.BossCurrentHp.Value = currentHealth;
            Global.BossActive.Value = true;
            Global.BossPhaseTwo.Value = false;
        }

        // 受伤时更新血条，并检测是否该进入二阶段
        public override void TakeDamage(float damage, Vector2 hitDir)
        {
            if (isDead) return;

            base.TakeDamage(damage, hitDir);

            // 同步当前血量到 UI
            Global.BossCurrentHp.Value = currentHealth;

            // 血量第一次降到阈值以下 → 进入二阶段
            if (!isPhaseTwo && currentHealth <= maxHealth * phaseTwoHpPercent)
            {
                isPhaseTwo = true;
                Global.BossPhaseTwo.Value = true;
                StartPhaseTwo();
            }
        }

        /// <summary>
        /// 进入二阶段。默认实现：打断所有协程 → 原地静止变红 → 触发环形射击。
        /// 子类如果想自定义转阶段行为，可以重写这个方法。
        /// </summary>
        protected virtual void StartPhaseTwo()
        {
            OnPhaseTwoStarted?.Invoke();

            // 打断当前正在进行的所有动作（攻击 / 换弹 / 推进 / 环射）
            StopAllCoroutines();

            // 标记转阶段中，Update 里的状态切换会跳过，避免又触发新的攻击协程
            isInPhaseTransition = true;

            StartCoroutine(PhaseTwoTransition());
        }

        // 转阶段演出：原地停 1 秒，颜色从原色渐变到红色
        protected virtual IEnumerator PhaseTwoTransition()
        {
            // 记录并冻结移速
            speedBeforeTransition = moveSpeed;
            moveSpeed = 0f;

            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;

            while (elapsed < phaseTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phaseTransitionDuration);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, Color.red, t);
                }
                yield return null;
            }

            // 演出结束，恢复移速并触发环形射击
            moveSpeed = speedBeforeTransition;
            isInPhaseTransition = false;
            StartRingShot();
        }

        /// <summary>
        /// 子类实现：触发环形射击（无差别 360 度弹幕）。
        /// 转阶段演出结束后会调用它。
        /// </summary>
        protected abstract void StartRingShot();

        // Boss 死亡：专属掉落 + 显示传送门
        protected override void Death(Vector2 hitDir)
        {
            if (isDead) return;
            isDead = true;

            // 通知 UI Boss 已离场
            Global.BossActive.Value = false;

            // Boss 专属掉落：金币 + dirtyBlood
            GenerateBossDrops();

            // 显示传送门（如果在 Inspector 里绑定了的话）
            if (portal != null)
            {
                portal.SetActive(true);
            }

            // 下面复用基类的通用死亡逻辑，但跳过普通敌人的掉落（Boss 有专属掉落）
            AudioKitManager.Instance.PlayOneShot("KillSFX", volume: 0.6f);
            BloodSigilState.NotifyUnitKilled();

            // 从房间敌人列表移除，这样 Room.Update 会自动开门
            if (Room != null)
            {
                Room.GetEnemies().Remove(this);
            }

            // 生成尸体
            FxManager.SpawnEnemyBody(body, transform.Position2D(), hitDir);

            this.DestroyGameObjGracefully();
        }

        // 生成 Boss 专属掉落
        protected virtual void GenerateBossDrops()
        {
            // 掉一堆金币
            for (int i = 0; i < coinDropCount; i++)
            {
                var coin = DropManager.Instance.Coin.Instantiate()
                    .Position(transform.position + RandomOffset())
                    .Show();
            }

            // 掉几个 dirtyBlood（补血库）
            for (int i = 0; i < dirtyBloodDropCount; i++)
            {
                var db = DropManager.Instance.DirtyBlood.Instantiate()
                    .Position(transform.position + RandomOffset())
                    .Show();
            }
        }

        // 给掉落物一个小的随机偏移，避免全部叠在一起
        protected Vector3 RandomOffset()
        {
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
        }
    }
}
