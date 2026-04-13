using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Chest : ViewController
	{
		bool isCollected;
		void Start()
		{
			isCollected = false;
		}

		private void OnTriggerEnter2D(Collider2D collider)
		{
			// Check if the collider belongs to the player
            if (collider.GetComponent<CollectBox>() != null && !isCollected)
            {
				AudioKit.PlaySound("RareLootSFX", volume: 1.0f);
				
				isCollected = true;
				// 延迟 45 帧后生成战利品
				ActionKit.DelayFrame(45, () => {
					DropManager.Instance.HealthPotion.Instantiate()
					.Position(this.transform.position + new Vector3(0, 1.3f, 0))  // slight offset for better visibility
					.Show();
				// 	this.DestroyGameObjGracefully();
				}).Start(this);
            }
		}
	}
}
