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
    /// 房间级血迹管理器：每个房间一个环形缓冲 FIFO 跟踪 + 全局共享对象池。
    /// 由 FxManager 持有，通过 FxManager.DrawEnemyBlood(pos, room) 接入。
    /// 性能设计(稳态零分配)：
    /// - 池预热：Prewarm() 按配置最大上限一次性建池，战斗期不再 Instantiate。
    /// - 环形缓冲替代 LinkedList：FIFO 内建(满时头位即淘汰者)，每次生成零分配。
    /// - 单一淡出泵：一个常驻协程推进所有淡出，替代每精灵 StartCoroutine，空闲自停。
    /// - 血迹无生命周期：未淘汰前永久保留(直到关卡切换 ClearAll)。
    /// - 无淡入：满 alpha 直接出现，与原 DrawBlood 行为一致。
    /// </summary>
    public class RoomBloodManager
    {
        /// <summary>淡出中条目(struct 值类型存 List，泵内回写，零分配)</summary>
        private struct FadingEntry
        {
            public SpriteRenderer Sprite;
            public float Elapsed;
            public float Duration;
            public Color BaseColor;
        }

        /// <summary>
        /// 环形缓冲 FIFO：数组预分配，Add 零分配；满时 TakeHead 返回最早元素并腾位，
        /// "添加即淘汰"内建于数据结构，无需独立淘汰循环。
        /// </summary>
        private class BloodRing
        {
            public readonly SpriteRenderer[] Items;
            public int Head; // 最早元素下标
            public int Count;
            public int Capacity => Items.Length;

            public BloodRing(int capacity)
            {
                Items = new SpriteRenderer[Mathf.Max(1, capacity)];
            }

            /// <summary>满时取出最早元素(调用方负责送入淡出泵)；未满返回 null</summary>
            public SpriteRenderer TakeHead()
            {
                if (Count < Capacity) return null;
                var oldest = Items[Head];
                Items[Head] = null;
                Head = (Head + 1) % Capacity;
                Count--;
                return oldest;
            }

            public void Add(SpriteRenderer sprite)
            {
                Items[(Head + Count) % Capacity] = sprite;
                Count++;
            }
        }

        private readonly MonoBehaviour _coroutineRunner;
        private readonly List<RoomTypeBloodConfig> _entries;
        private readonly RoomTypeBloodConfig _fallback;
        private readonly SpriteRenderer _template;
        private readonly Transform _container;

        // 房间 → 该房间血迹环形缓冲(FIFO 顺序)
        private readonly Dictionary<Room, BloodRing> _roomBlood = new();
        // 淡出中列表(单一泵推进)
        private readonly List<FadingEntry> _fading = new();
        // 池中空闲精灵(淡出完成，等待复用)
        private readonly Stack<SpriteRenderer> _freePool = new();
        // 池中所有精灵实例(ClearAll 时统一销毁)
        private readonly List<SpriteRenderer> _allInstances = new();
        // 单一淡出泵协程句柄
        private Coroutine _fadePump;

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

        /// <summary>池预热：按配置中最大上限一次性建池，战斗期零 Instantiate</summary>
        public void Prewarm()
        {
            if (_allInstances.Count > 0 || _template == null) return;
            int target = Mathf.Max(0, _fallback.maxBloodCount);
            foreach (var e in _entries)
                if (e != null && e.maxBloodCount > target) target = e.maxBloodCount;
            for (int i = 0; i < target; i++)
            {
                var sprite = Object.Instantiate(_template, _container);
                sprite.gameObject.SetActive(false);
                _allInstances.Add(sprite);
            }
        }

        /// <summary>产生一条血迹(EnemyBase 受击时调用)</summary>
        public void DrawEnemyBlood(Vector2 pos, Room room)
        {
            if (room == null || room.roomConfig == null) return;

            var cfg = GetConfig(room.roomConfig.roomType);
            var ring = GetOrCreateRing(room, cfg.maxBloodCount);

            // 环形已满时头位即最早血迹，取出送入淡出泵，腾出的位置正好给新血迹
            var oldest = ring.TakeHead();
            if (oldest != null) EnqueueFade(oldest, cfg.fadeOutDuration);

            var sprite = Acquire();
            if (sprite == null) return;
            ring.Add(sprite);
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
            _fading.Clear();
            if (_fadePump != null)
            {
                _coroutineRunner.StopCoroutine(_fadePump);
                _fadePump = null;
            }
        }

        private RoomTypeBloodConfig GetConfig(RoomType type)
        {
            foreach (var e in _entries)
                if (e != null && e.roomType == type) return e;
            return _fallback;
        }

        private BloodRing GetOrCreateRing(Room room, int capacity)
        {
            if (!_roomBlood.TryGetValue(room, out var ring))
            {
                ring = new BloodRing(capacity);
                _roomBlood[room] = ring;
            }
            return ring;
        }

        private SpriteRenderer Acquire()
        {
            SpriteRenderer sprite;
            if (_freePool.Count > 0)
            {
                sprite = _freePool.Pop();
            }
            else
            {
                // 预热不足的兜底：极少发生(预热取的是各房间上限最大值)
                sprite = Object.Instantiate(_template, _container);
                _allInstances.Add(sprite);
            }
            sprite.gameObject.SetActive(true);
            return sprite;
        }

        /// <summary>送入淡出泵(零 per-fade 协程分配)，泵空闲时启动</summary>
        private void EnqueueFade(SpriteRenderer sprite, float duration)
        {
            if (sprite == null) return;
            _fading.Add(new FadingEntry
            {
                Sprite = sprite,
                Elapsed = 0f,
                Duration = Mathf.Max(0.01f, duration),
                BaseColor = sprite.color,
            });
            if (_fadePump == null)
            {
                _fadePump = _coroutineRunner.StartCoroutine(FadePump());
            }
        }

        /// <summary>单一淡出泵：每帧推进所有淡出中的精灵，空闲时自停</summary>
        private IEnumerator FadePump()
        {
            while (_fading.Count > 0)
            {
                float dt = Time.deltaTime;
                for (int i = _fading.Count - 1; i >= 0; i--)
                {
                    var entry = _fading[i];
                    // 关卡切换 ClearAll 销毁精灵后，下一帧检测到 null 安全跳过
                    if (entry.Sprite == null)
                    {
                        _fading.RemoveAt(i);
                        continue;
                    }
                    entry.Elapsed += dt;
                    if (entry.Elapsed >= entry.Duration)
                    {
                        entry.Sprite.gameObject.SetActive(false);
                        // 复位 alpha 供下次复用
                        entry.Sprite.color = new Color(entry.BaseColor.r, entry.BaseColor.g, entry.BaseColor.b, 1f);
                        _freePool.Push(entry.Sprite);
                        _fading.RemoveAt(i);
                    }
                    else
                    {
                        float t = entry.Elapsed / entry.Duration;
                        entry.Sprite.color = new Color(entry.BaseColor.r, entry.BaseColor.g, entry.BaseColor.b, 1f - t);
                        _fading[i] = entry; // struct 副本需回写
                    }
                }
                yield return null;
            }
            _fadePump = null;
        }

        // 与 FxManager.DrawBlood 行为一致：满 alpha 出现 + 飞溅动画，不做淡入
        private void SetupAndPlay(SpriteRenderer blood, Vector2 originPos)
        {
            blood.Position2D(originPos)
                 .EulerAnglesZ(Random.Range(0, 360f))
                 .LocalScale(0.1f);
            var c = blood.color;
            blood.color = new Color(c.r, c.g, c.b, 1f);

            // 血液随机向一个地方飞溅(守卫：精灵被淘汰进入淡出 alpha<1 后停止写入，
            // 防止旧动画闭包污染已归池/复用的精灵)
            var angle = Random.Range(0, 360);
            var radius = Random.Range(0.2f, 1.5f);
            var movePos = angle.AngleToDirection2D() * radius;
            var scaleTo = Random.Range(0.2f, 3.0f);
            ActionKit.Lerp(0, 1, Random.Range(0.1f, 0.3f), (p) =>
            {
                if (blood.color.a < 1f) return;
                p = EaseUtility.InCubic(0, 1, p);
                blood.Position2D(originPos + movePos * p);
                blood.LocalScale(scaleTo * p);
            }).StartCurrentScene();
        }
    }
}
