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

        // 读档还原时为已完成房间生成装饰性战斗痕迹（血迹 + 尸体）。
        // 痕迹为纯视觉、无逻辑：位置在房间可达地面随机，尸体调暗以区别于活敌人。
        public static void SpawnBattleTraces(Room room)
        {
            if (Instance == null || room == null || room.roomConfig == null) return;

            var roomMap = room.roomConfig.roomMap;
            int h = roomMap.Count;
            int w = roomMap[0].Length;

            // 收集可达地面格（非墙/非掩体/非门，且不在门口 2 格内）
            var floorCells = new List<Vector2Int>();
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    char c = roomMap[i][j];
                    if (c != ' ') continue;             // 只取纯地面，避开墙/掩体/门/宝箱等
                    int x = j + room.LB.x;
                    int y = room.LB.y + (h - 1 - i);
                    floorCells.Add(new Vector2Int(x, y));
                }
            }
            if (floorCells.Count == 0) return;

            // 血迹：3~6 处，随机位置/旋转/透明度（模拟干涸）
            int bloodCount = Mathf.Min(Random.Range(3, 7), floorCells.Count);
            for (int i = 0; i < bloodCount; i++)
            {
                var cell = floorCells[Random.Range(0, floorCells.Count)];
                var pos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                var blood = Instance.EnemyBlood.Instantiate()
                    .Position2D(pos)
                    .EulerAnglesZ(Random.Range(0, 360f))
                    .LocalScale(Random.Range(0.5f, 1.2f))
                    .Show();
                blood.color = new Color(1f, 1f, 1f, Random.Range(0.5f, 0.8f));
                blood.sortingOrder = -2;                // 血迹在最底层
                Instance._spawnedEffects.Add(blood.gameObject);
            }

            // 尸体：1~3 具，从当前关卡实际会出现的敌人尸体中随机选。
            // 直接使用 FxManager 的 EnemyXBody 字段（这些是专用于死亡效果的尸体精灵，
            // 与敌人 prefab 上存活时的 body 是完全不同的 SpriteRenderer）。
            var availableBodies = new List<SpriteRenderer>();
            int maxIndex = Mathf.Min(Global.currentDifficulty, 3);   // 0-3 对应 Enemy1Body~Enemy4Body
            for (int i = 0; i <= maxIndex; i++)
            {
                var body = i switch
                {
                    0 => Instance.Enemy1Body,
                    1 => Instance.Enemy2Body,
                    2 => Instance.Enemy3Body,
                    3 => Instance.Enemy4Body,
                    _ => null,
                };
                if (body != null)
                    availableBodies.Add(body);
            }
            if (availableBodies.Count == 0 && Instance.Enemy1Body != null)
                availableBodies.Add(Instance.Enemy1Body);   // 兜底：至少保留一种

            int bodyCount = Mathf.Min(Random.Range(1, 4), floorCells.Count);
            for (int i = 0; i < bodyCount; i++)
            {
                var cell = floorCells[Random.Range(0, floorCells.Count)];
                var pos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                var bodyPrefab = availableBodies[Random.Range(0, availableBodies.Count)];
                var corpse = bodyPrefab.Instantiate()
                    .Position2D(pos)
                    .Show();
                // 保留原引用物体的亮度和透明度（源物体已在 Inspector 调好参数）
                corpse.sortingOrder = -1;                               // 尸体在血迹上、玩家/掉落物下
                Instance._spawnedEffects.Add(corpse.gameObject);
            }
        }

        void OnDestroy()
        {
            Instance = null;
        }
    }
}