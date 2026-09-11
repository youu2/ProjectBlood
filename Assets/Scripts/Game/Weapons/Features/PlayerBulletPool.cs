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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留
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
                    var obj = Instantiate(prefab);
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

        private void OnSceneLoaded()
        {
            Debug.Log("[PlayerBulletPool] OnSceneLoaded: 场景加载完成，开始预热子弹池。");
            // 场景加载完成时，先清空旧池。
            // 池实例挂在 DontDestroyOnLoad 下，但池中的子弹实例生成在旧场景中，
            // 会随场景卸载被销毁，活跃跟踪集合也必须同步清空，避免残留已销毁对象的引用
            poolDictionary.Clear();
            activeObjects.Clear();

            // Player 实例未就绪则跳过预热
            if (Player.player1 == null) return;

            // 然后根据已解锁的武器预热子弹池
            for (int i = 0; i < WeaponDataSystem.weaponDataList.Count; i++)
            {
                var weaponName = WeaponDataSystem.weaponDataList[i].weaponName;
                var currentWeapon = Player.player1.GetWeaponFromName(weaponName);
                if (currentWeapon == null)
                {
                    Debug.LogWarning($"[PlayerBulletPool] 未找到武器 {weaponName}，跳过预热。");
                    continue;
                }

                var prefab = currentWeapon.BulletPrefab;
                if (prefab == null)
                {
                    // 例如 Laser 武器不发射子弹，BulletPrefab 无需赋值，这里静默跳过
                    continue;
                }
                Preload(prefab, 50);
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