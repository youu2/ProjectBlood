using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Coin : ViewController
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
                Global.AddCoin(1);
				this.DestroyGameObjGracefully();
            }
		}
	}
}
