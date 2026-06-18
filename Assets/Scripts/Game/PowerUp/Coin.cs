using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Coin : DropItem
	{
		protected override void Collect()
		{
			AudioKitManager.Instance.PlayOneShot("CoinPickup", volume: 0.5f);
			Global.AddCoin(1);
			this.DestroyGameObjGracefully();
		}
	}
}
