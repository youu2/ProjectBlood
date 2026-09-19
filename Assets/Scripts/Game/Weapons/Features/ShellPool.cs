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
        }
        public ObjectPool<GameObject> shellPool;
        public GameObject CreateShell()
        {
            var shell = Instantiate(DropManager.Instance.Shell.gameObject);
            // shell.transform.SetParent(transform);
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
