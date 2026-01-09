using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class HealthPotion : ViewController
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
				if(Global.currentHP.Value < Global.MAX_HP.Value)
				{
					//AudioKit.PlaySound("HealthPickup");
					Global.AddHP(1);
					this.DestroyGameObjGracefully();
				}
            }
		}

	}
}
