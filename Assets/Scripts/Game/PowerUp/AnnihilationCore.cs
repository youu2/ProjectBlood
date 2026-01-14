using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	// 湮灭核心（好蠢的名字）
	public partial class AnnihilationCore : ViewController
	{
		void Start()
		{
			// Code Here
		}

		private void OnTriggerEnter2D(Collider2D collider)
		{
			// Check if the collider belongs to the player
            if (collider.GetComponent<CollectBox>() != null)
            {
				//AudioKit.PlaySound("AnnihilationCorePickup");
				Global.AddAnnihilationCore(1);
				this.DestroyGameObjGracefully();
            }
		}

	}
}
