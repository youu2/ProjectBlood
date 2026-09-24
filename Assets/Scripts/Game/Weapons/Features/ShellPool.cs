using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ProjectBlood
{
    public class ShellPool : MonoBehaviour
    {
        public static ShellPool instance;
        public int countAll;
        public int countActive;
        public int countInactive;

        // 弹壳实例的专用父容器，与子弹容器分开，避免被子弹池的跨关卡清理误销毁
        private Transform shellContainer;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this); // 单例已存在，只移除重复组件，保留宿主 Weapon 物体
                return;
            }
            instance = this;
            shellPool = new ObjectPool<GameObject>(CreateShell, GetShell, ReleaseShell, DestroyShell, true, 50, 200);
            DontDestroyOnLoad(gameObject); // 跨场景保留

            shellContainer = new GameObject("Shells").transform;
            shellContainer.SetParent(transform, false);
        }
        public ObjectPool<GameObject> shellPool;
        public GameObject CreateShell()
        {
            // 生成在专用弹壳容器(WeaponPools/Shells,跨场景保留)下方，
            // 与子弹容器隔离，避免被子弹池的跨关卡清理误销毁
            var shell = Instantiate(DropManager.Instance.Shell.gameObject, shellContainer);
            shell.SetActive(false);
            return shell;
        }
        public void GetShell(GameObject shell)
        {
            shell.SetActive(true);
        }
        public void ReleaseShell(GameObject shell)
        {
            shell.SetActive(false);
        }
        public void DestroyShell(GameObject shell)
        {
            Destroy(shell);
        }
        public void Update()
        {
            countAll = shellPool.CountAll;
            countActive = shellPool.CountActive;
            countInactive = shellPool.CountInactive;
        }
    }
}
