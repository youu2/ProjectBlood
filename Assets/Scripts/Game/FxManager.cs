using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class FxManager : ViewController
	{
		public static FxManager Instance;
		void Awake()
		{
			Instance = this;
		}
		void OnDestroy()
		{
			Instance = null;
		}
	}
}
