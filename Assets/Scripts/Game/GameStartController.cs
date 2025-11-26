using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class GameStartController : ViewController
	{
		void Start()
		{
			UIKit.OpenPanel<UIGameStartPanel>();
		}
	}
}
