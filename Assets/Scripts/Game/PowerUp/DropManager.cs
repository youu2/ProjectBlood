using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class DropManager : ViewController
	{
		public static DropManager Instance;
		void Awake()
		{
			Instance = this;
		}
        private void OnDestroy()
        {
            // 同 Player.player1 的修复：避免场景切换时旧实例 OnDestroy 在新实例 Awake
            // 之后执行而误清空 Instance，导致 CreateShell 等路径拿不到 DropManager。
            if (Instance == this)
            {
                Instance = null;
            }
        }
        void Start()
		{
			// Code Here
		}

	}
}
