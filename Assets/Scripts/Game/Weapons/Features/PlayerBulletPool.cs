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
            return poolDictionary[key].Get();
        }

        /// <summary>
        /// 回收对象到池中（需指定对应的预制体）
        /// </summary>
        public void Release(GameObject obj, GameObject prefab)
        {
            if (obj == null || prefab == null) return;

            int key = prefab.GetInstanceID();
            if (poolDictionary.TryGetValue(key, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                // 如果池不存在（极少发生），直接销毁物体避免泄漏
                Destroy(obj);
            }
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
            int key = prefab.GetInstanceID();
            if (!poolDictionary.ContainsKey(key))
                CreatePool(prefab);

            var pool = poolDictionary[key];
            List<GameObject> tempList = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                tempList.Add(pool.Get());
            }
            foreach (var obj in tempList)
            {
                pool.Release(obj);
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
                createFunc: () => Instantiate(prefab),
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
            // 场景加载完成时，先清空旧池
            poolDictionary.Clear();

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