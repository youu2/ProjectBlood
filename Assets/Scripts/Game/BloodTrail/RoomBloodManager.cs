using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 单个房间类型的血迹配置(FxManager Inspector 直接编辑)。
    /// </summary>
    [System.Serializable]
    public class RoomTypeBloodConfig
    {
        [Tooltip("房间类型")] public RoomType roomType;
        [Tooltip("本房间内血迹精灵上限")] public int maxBloodCount = 30;
        [Tooltip("被淘汰时血迹的淡出时间(秒)")] public float fadeOutDuration = 0.5f;
    }

    /// <summary>
    /// 房间级血迹管理器：维护每个房间的血迹 FIFO 跟踪 + 全局共享对象池。
    /// 由 FxManager 持有，通过 FxManager.DrawEnemyBlood(pos, room) 接入。
    /// 设计要点：
    /// - 池动态扩容：跨房间累计时若 freePool 空，直接 Instantiate 新精灵。
    /// - 每个 Room 一个 LinkedList<SpriteRenderer> 跟踪 FIFO 顺序。
    /// - 满额时淘汰最早精灵：启动淡出协程 → 完成后归池。
    /// - 淡出期间精灵计入 _releasing 集合，不重复淘汰。
    /// - 血迹无生命周期：未淘汰前永久保留(直到关卡切换 ClearAll)。
    /// 配置直接以 List+fallback 形式挂在 FxManager Inspector,无需 SO 资产。
    /// </summary>
    public class RoomBloodManager
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly List<RoomTypeBloodConfig> _entries;
        private readonly RoomTypeBloodConfig _fallback;
        private readonly SpriteRenderer _template;
        private readonly Transform _container;

        // 房间 → 该房间内当前所有血迹(FIFO 顺序：First = 最早生成)
        private readonly Dictionary<Room, LinkedList<SpriteRenderer>> _roomBlood = new();
        // 正在淡出中的精灵集合，防止同一精灵被重复淘汰
        private readonly HashSet<SpriteRenderer> _releasing = new();
        // 池中空闲精灵(已淡出完成或被回收，等待下次复用)
        private readonly Stack<SpriteRenderer> _freePool = new();
        // 池中所有曾经创建的精灵实例(ClearAll 时统一销毁)
        private readonly List<SpriteRenderer> _allInstances = new();

        public RoomBloodManager(MonoBehaviour coroutineRunner,
                                 List<RoomTypeBloodConfig> entries,
                                 RoomTypeBloodConfig fallback,
                                 SpriteRenderer template,
                                 Transform container)
        {
            _coroutineRunner = coroutineRunner;
            _entries = entries ?? new List<RoomTypeBloodConfig>();
            _fallback = fallback ?? new RoomTypeBloodConfig();
            _template = template;
            _container = container;
        }

        /// <summary>产生一条血迹(EnemyBase 受击时调用)</summary>
        public void DrawEnemyBlood(Vector2 pos, Room room)
        {
            if (room == null || room.roomConfig == null) return;

            var cfg = GetConfig(room.roomConfig.roomType);
            var tracker = GetOrCreateTracker(room);

            // 满额 → 淘汰最早(非释放中)，直到腾出空位
            while (tracker.Count >= cfg.maxBloodCount)
            {
                var oldest = tracker.First;
                if (oldest == null) break;
                tracker.RemoveFirst();
                var oldestSprite = oldest.Value;

                if (!_releasing.Contains(oldestSprite))
                {
                    _releasing.Add(oldestSprite);
                    _coroutineRunner.StartCoroutine(FadeOutAndRelease(oldestSprite, cfg.fadeOutDuration));
                }
            }

            var sprite = Acquire();
            if (sprite == null) return;

            tracker.AddLast(sprite);
            SetupAndPlay(sprite, pos);
        }

        /// <summary>清空所有血迹(关卡切换时调用)</summary>
        public void ClearAll()
        {
            foreach (var sprite in _allInstances)
            {
                if (sprite != null) Object.Destroy(sprite.gameObject);
            }
            _allInstances.Clear();
            _freePool.Clear();
            _roomBlood.Clear();
            _releasing.Clear();
        }

        private RoomTypeBloodConfig GetConfig(RoomType type)
        {
            foreach (var e in _entries)
                if (e != null && e.roomType == type) return e;
            return _fallback;
        }

        private LinkedList<SpriteRenderer> GetOrCreateTracker(Room room)
        {
            if (!_roomBlood.TryGetValue(room, out var list))
            {
                list = new LinkedList<SpriteRenderer>();
                _roomBlood[room] = list;
            }
            return list;
        }

        // 从池中获取一个精灵实例(或创建新实例)
        // 池耗尽时动态扩容(理论上发生在跨房间累计血迹超过单房上限时)
        private SpriteRenderer Acquire()
        {
            SpriteRenderer sprite;
            if (_freePool.Count > 0)
            {
                sprite = _freePool.Pop();
            }
            else
            {
                // 池耗尽：动态扩容(理论上发生在跨房间累计血迹超过单房上限时)
                sprite = Object.Instantiate(_template, _container);
                _allInstances.Add(sprite);
            }
            sprite.gameObject.SetActive(true);
            return sprite;
        }

        // 与 FxManager.DrawBlood 行为一致：满 alpha 出现 + 飞溅动画，不做淡入
        private void SetupAndPlay(SpriteRenderer blood, Vector2 originPos)
        {
            blood.Position2D(originPos)
                 .EulerAnglesZ(Random.Range(0, 360f))
                 .LocalScale(0.1f);
            var c = blood.color;
            blood.color = new Color(c.r, c.g, c.b, 1f);

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

        private IEnumerator FadeOutAndRelease(SpriteRenderer sprite, float duration)
        {
            if (sprite == null)
            {
                _releasing.Remove(sprite);
                yield break;
            }

            var startColor = sprite.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // 关卡切换时 ClearAll 会销毁精灵，协程下一帧检测到 null 后安全退出
                if (sprite == null) { _releasing.Remove(sprite); yield break; }
                float t = Mathf.Clamp01(elapsed / duration);
                sprite.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            if (sprite != null)
            {
                sprite.gameObject.SetActive(false);
                // 复位 alpha 供下次复用
                sprite.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
                _freePool.Push(sprite);
            }
            _releasing.Remove(sprite);
        }
    }
}
