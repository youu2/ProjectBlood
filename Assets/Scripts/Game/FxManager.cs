using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;

namespace ProjectBlood
{
    public partial class FxManager : ViewController
    {
        public static FxManager Instance;
        private List<GameObject> _spawnedEffects = new List<GameObject>();
        private RoomBloodManager _roomBloodManager;

        [Header("=== 血迹系统配置 ===")]
        [Tooltip("按房间类型配置血迹上限与淡出时间；未列出的类型走 BloodTrailFallback")]
        public List<RoomTypeBloodConfig> BloodTrailEntries = new();

        [Tooltip("未在 Entries 中列出的房间类型使用此兜底配置")]
        public RoomTypeBloodConfig BloodTrailFallback = new();

        void Awake()
        {
            Instance = this;
            if (EnemyBlood != null)
            {
                _roomBloodManager = new RoomBloodManager(this, BloodTrailEntries, BloodTrailFallback, EnemyBlood, transform);
                // 池预热：按配置最大上限一次性建池，战斗期零 Instantiate
                _roomBloodManager.Prewarm();
            }
        }
        public static void PlayEnemyHurtFX(Vector2 pos)
        {
            var enemyHurt = Instance.EnemyHurt.Instantiate();
            enemyHurt
            .Position2D(pos)
            .Show()
            .Self(self =>
            {
                ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
                // StartCurrentScene用于在非MonoBehaviour类中启动协程，
                // QFramework 在场景加载时会自动创建一个隐藏的 SceneCoroutineRunner GameObject，专门用来管理这些协程。
            }).Play();
            Destroy(enemyHurt.gameObject, 1f);
        }
        public static void PlayPlayerHurtFX(Vector2 pos)
        {
            Instance.PlayerHurt.Instantiate()
            .Position2D(pos)
            .Show()
            .Self(self =>
            {
                ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
            }).Play();
        }


        public static void DrawBlood(Vector2 originPos, SpriteRenderer bloodSource)
        {
            var blood = bloodSource.Instantiate()
            .Position2D(originPos)
            .EulerAnglesZ(Random.Range(0, 360f))
            .LocalScale(0.1f)
            .Show();

            Instance._spawnedEffects.Add(blood.gameObject);

            // 血液随机向一个地方飞溅
            var angle = Random.Range(0, 360);
            var radius = Random.Range(0.2f, 1.5f);
            var movePos = angle.AngleToDirection2D() * radius;
            var scaleTo = Random.Range(0.2f, 3.0f);
            ActionKit.Lerp(0, 1, Random.Range(0.1f, 0.3f), (p) =>
            {
                p = EaseUtility.InCubic(0, 1, p);
                blood.Position2D(originPos + movePos * p);
                blood.LocalScale(scaleTo * p);
            }).StartCurrentScene();
        }

        public static void DrawPlayerBlood(Vector2 originPos)
        {
            DrawBlood(originPos, Instance.PlayerBlood);
        }

        public static void DrawEnemyBlood(Vector2 originPos, Room room = null)
        {
            if (Instance._roomBloodManager != null && room != null)
            {
                Instance._roomBloodManager.DrawEnemyBlood(originPos, room);
            }
            else
            {
                // 兜底：EnemyBlood 未配置或敌人未注册到房间时走原 Instantiate 路径
                DrawBlood(originPos, Instance.EnemyBlood);
            }
        }

        public static void PlayShieldBlockFX(Vector2 pos)
        {
            Instance.ShieldBlock.Instantiate()
            .Position2D(pos)
            .Show()
            .Self(self =>
            {
                ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
            }).Play();
        }

        public static void PlayShieldBreakFX(Vector2 pos)
        {
            Instance.ShieldBlock.Instantiate()
            .Position2D(pos)
            .Show()
            .Self(self =>
            {
                ActionKit.Delay(self.main.duration + 0.3f, () => self.gameObject.SetActive(false)).StartCurrentScene();
            }).Play();
        }

        public static void ClearAllEffects()
        {
            // 房间血迹池统一清理(淡出协程会在下一帧检测到 sprite==null 后安全退出)
            Instance._roomBloodManager?.ClearAll();
            foreach (var effect in Instance._spawnedEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            Instance._spawnedEffects.Clear();
        }

        public static void AddEffect(GameObject effect)
        {
            Instance._spawnedEffects.Add(effect);
        }

        public static void SpawnEnemyBody(SpriteRenderer body, Vector2 originPos, Vector2 hitDir)
        {
            var dieBody = body.Instantiate()
                .Self(self =>
                {
                    self.flipX = RandomUtility.Choose(true, false);
                }).Show();

            Instance._spawnedEffects.Add(dieBody.gameObject);

            var moveDistance = Random.Range(0.5f, 1.1f);
            ActionKit.Lerp(0, 1, 0.3f, (p) =>
            {
                dieBody.transform.Position2D(Vector2.Lerp(originPos, originPos + moveDistance * hitDir, p));
            }).StartCurrentScene();
        }

        void OnDestroy()
        {
            Instance = null;
        }
    }
}