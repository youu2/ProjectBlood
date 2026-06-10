using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Exp : DropItem
	{
		protected override void Collect()
		{
			AudioKit.PlaySound("ExpPickup");
			Global.AddExp(1);
			this.DestroyGameObjGracefully();
		}
	}
}