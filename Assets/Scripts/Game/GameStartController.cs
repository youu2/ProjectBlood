using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class GameStartController : ViewController
	{
		public void awake()
		{
			ResKit.Init();	
		}
		void Start()
		{
			UIKit.OpenPanel<UIGameStartPanel>();
		}
	}
}
