using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class HealthPotion : DropItem
	{
		protected override void Collect()
		{
			if (Global.currentHP.Value < Global.MAX_HP.Value)
			{
				AudioKit.PlaySound("HpPickup", volume: 0.6f);
				Global.AddHP(1);
				this.DestroyGameObjGracefully();
			}
		}
	}
}