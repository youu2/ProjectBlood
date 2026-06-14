using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Coin : DropItem
	{
		protected override void Collect()
		{
			AudioKitManager.Instance.PlayOneShot("CoinPickup");
			Global.AddCoin(1);
			this.DestroyGameObjGracefully();
		}
	}
}
