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
            Instance = null;   
        }
        void Start()
		{
			// Code Here
		}

	}
}
