using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Exp : DropItem
	{
		protected override void Collect()
		{
			AudioKitManager.Instance.PlayOneShot("ExpPickup");
			Global.AddExp(1);
			this.DestroyGameObjGracefully();
		}
	}
}