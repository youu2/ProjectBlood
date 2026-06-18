using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Exp : DropItem
	{
		protected override void Collect()
		{
			AudioKitManager.Instance.PlayOneShot("ExpPickup", volume: 0.7f);
			Global.AddExp(1);
			this.DestroyGameObjGracefully();
		}
	}
}