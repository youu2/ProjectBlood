using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ProjectBlood
{
    public class PlayerBulletPool : MonoBehaviour
    {
        public static PlayerBulletPool Instance { get; private set; }

        [Header("Global Pool Settings")]
        [SerializeField] private int defaultCapacity = 20;
        [SerializeField] private int maxSize = 100;

        // 核心：存储每种预制体对应的对象池
        private Dictionary<int, ObjectPool<GameObject>> poolDictionary = new Dictionary<int, ObjectPool<GameObject>>();

        // 当前从池中借出（活跃）的对象集合：Get 时登记，Release 时注销。
        // 用于把 Release 变成幂等操作——同一对象重复归还时直接拦截，
        // 避免 UnityEngine.Pool 抛出
        // "Trying to release an object that has already been released to the pool"
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();

        // 子弹实例的专用父容器。与弹壳容器分开：
        // 两个池组件挂在同一个 WeaponPools 物体上，各自管理各自的子物体
        private Transform bulletContainer;

        // 池跨关卡持久化（与 ShellPool 一致）：只在首次加载时预热，之后不再重建
        private bool poolsInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // 单例已存在，只移除重复组件，保留宿主 Weapon 物体
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留

            bulletContainer = new GameObject("PlayerBullets").transform;
            bulletContainer.SetParent(transform, false);
        }

        void OnEnable()
        {
            GameUI.OnLoadingComplete += OnSceneLoaded;
        }

        /// <summary>
        /// 从池中获取一个对象（如果对应预制体的池不存在，则自动创建）
        /// </summary>
        public GameObject Get(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[PlayerBulletPool] Get: prefab 为空，无法创建或获取对象。请检查调用方传入的预制体。");
                return null;
            }
            int key = prefab.GetInstanceID();
            if (!poolDictionary.ContainsKey(key))
            {
                CreatePool(prefab);
            }
            GameObject obj = poolDictionary[key].Get();
            activeObjects.Add(obj); // 登记为借出状态
            return obj;
        }

        /// <summary>
        /// 回收对象到池中（需指定对应的预制体）
        /// </summary>
        public void Release(GameObject obj, GameObject prefab)
        {
            if (obj == null) return;

            // 状态跟踪：只有处于借出状态的对象才允许归还。
            // Remove 返回 false 说明该对象已经回收过（或并非由本池借出），
            // 此时必须拦截，不能再次调用 pool.Release，否则底层 ObjectPool
            // 会抛出 InvalidOperationException
            if (!activeObjects.Remove(obj))
            {
                Debug.LogWarning($"[PlayerBulletPool] 检测到重复回收，已忽略: {obj.name} (instanceID={obj.GetInstanceID()})。请检查该对象是否在同一帧触发了多次碰撞回调。");
                return;
            }

            if (prefab == null || !poolDictionary.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                // 找不到对应池（池已随场景重建，或 prefab 引用丢失），直接销毁避免泄漏
                Debug.LogWarning($"[PlayerBulletPool] 未找到 {obj.name} 对应的对象池（prefab 为空或池已重建），直接销毁该对象。");
                Destroy(obj);
                return;
            }

            pool.Release(obj);
        }

        /// <summary>
        /// 预创建指定数量的对象（预热）
        /// </summary>
        public void Preload(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                Debug.LogError("[PlayerBulletPool] Preload: prefab 为空，跳过预热。请检查调用方传入的预制体。");
                return;
            }
            // 统一走带状态跟踪的 Get/Release，保证预热结束后对象在集合中处于"已回收"状态
            List<GameObject> tempList = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                tempList.Add(Get(prefab));
            }
            foreach (var obj in tempList)
            {
                Release(obj, prefab);
            }
        }

        private void CreatePool(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[PlayerBulletPool] CreatePool: prefab 为空，无法创建对象池。请检查武器的 BulletPrefab 是否在 Inspector 中正确赋值。");
                return;
            }
            int key = prefab.GetInstanceID();
            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    // 生成在专用子弹容器(WeaponPools/PlayerBullets,跨场景保留)下方，
                    // 便于在 Hierarchy 中统一管理，与弹壳容器隔离
                    var obj = Instantiate(prefab, bulletContainer);
                    // 预制体上 BulletPrefab 字段被序列化为指向自身根节点的自引用，
                    // Instantiate 后 Unity 会把它重映射到实例自身，
                    // 导致 Release(obj, bullet.BulletPrefab) 用实例 ID 查池找不到对应池，
                    // 最终走 Destroy 兜底分支——子弹被销毁而非回收。
                    // 这里强制把 BulletPrefab 指回真实的预制体资源，保证 Get/Release 使用相同的 key。
                    var bullet = obj.GetComponent<PlayerBullet>();
                    if (bullet != null) bullet.BulletPrefab = prefab;
                    return obj;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            poolDictionary.Add(key, pool);
        }

        // 新武器未配置预热数时的兜底默认值
        private const int FallbackPrewarmCount = 10;

        /// <summary>
        /// 读取武器预热数量：负数视为非法配置，回退默认值并警告
        /// </summary>
        private static int ResolvePrewarmCount(WeaponConfig config)
        {
            if (config.poolPrewarmCount <= 0)
            {
                Debug.LogWarning($"[PlayerBulletPool] {config.weaponName} 的预热数配置非法({config.poolPrewarmCount})，回退默认值 {FallbackPrewarmCount}。");
                return FallbackPrewarmCount;
            }
            return config.poolPrewarmCount;
        }

        private void OnSceneLoaded()
        {
            // Player 实例未就绪则跳过（下次场景加载仍会尝试首次预热）
            if (Player.player1 == null) return;

            // 关卡切换：强制回收上一关所有在飞子弹。
            // 子弹虽有寿命兜底，但加载屏只有 1~2 秒，临近切换时射出的子弹可能还没到期，
            // 不回收会携带上一关的强化/吸血/伤害状态在新场景中继续飞（最长近一个寿命周期）。
            // 池持久化存在，回收正常入栈；SetActive(false) 同时终止子弹的寿命协程
            RecycleAllActive();

            // 池与子弹实例均跨关卡保留，只在游戏首次加载时为所有武器预热一次，
            // 之后无论解锁与否各池都已就绪
            if (poolsInitialized) return;

            Debug.Log("[PlayerBulletPool] 首次加载：开始预热全部武器子弹池。");
            foreach (var config in WeaponConfig.All)
            {
                var currentWeapon = Player.player1.GetWeaponFromName(config.weaponName);
                if (currentWeapon == null)
                {
                    Debug.LogWarning($"[PlayerBulletPool] 未找到武器 {config.weaponName}，跳过预热。");
                    continue;
                }

                var prefab = currentWeapon.BulletPrefab;
                if (prefab == null)
                {
                    // 例如 Laser 武器不发射子弹，BulletPrefab 无需赋值，这里静默跳过
                    continue;
                }
                Preload(prefab, ResolvePrewarmCount(config));
            }
            poolsInitialized = true;
        }

        /// <summary>
        /// 强制回收全部借出中的子弹（关卡切换时调用）。
        /// Release 会修改 activeObjects，必须先拍快照再遍历；走子弹自身的 Recycle 以保留幂等防护
        /// </summary>
        private void RecycleAllActive()
        {
            if (activeObjects.Count == 0) return;
            var snapshot = new List<GameObject>(activeObjects);
            foreach (var obj in snapshot)
            {
                // 理论上子弹随 DDOL 容器不会被外部销毁；万一引用已死，只清理账本，不抛异常
                if (obj == null)
                {
                    activeObjects.Remove(obj);
                    continue;
                }
                obj.GetComponent<PlayerBullet>()?.Recycle();
            }
        }

        private void OnDisable()
        {
            GameUI.OnLoadingComplete -= OnSceneLoaded;
        }

        // ----- 可选：调试信息（在 Inspector 中查看） -----
        public Dictionary<int, ObjectPool<GameObject>> GetPools() => poolDictionary;
    }
}