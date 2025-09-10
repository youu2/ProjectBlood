using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class InGameUIControler : ViewController
	{
		void Start()
		{
			// Code Here
			UIKit.OpenPanel<UIGamePanel>();
		}

		private void OnDestroy()
		{
			UIKit.ClosePanel<UIGamePanel>();
		}
	}
}
