using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Exp : ViewController
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
				AudioKit.PlaySound("ExpPickup");
                Global.AddExp(1);
				this.DestroyGameObjGracefully();
            }
		}
	}
}
