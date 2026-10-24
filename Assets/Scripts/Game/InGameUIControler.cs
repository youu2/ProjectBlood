using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class InGameUIControler : ViewController
	{
		void Start()
		{
			UIKit.OpenPanel<UIGamePanel>();
		}

		private void OnDestroy()
		{
			UIKit.ClosePanel<UIGamePanel>();
		}
	}
}
