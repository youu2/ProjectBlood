// using UnityEngine;
// using QFramework;

// namespace ProjectBlood
// {
// 	public partial class DirtyBlood : DropItem
// 	{
// 		public int bloodAmount = 20;

// 		protected override void Collect()
// 		{
// 			AudioKit.PlaySound("HpPickup", volume: 0.6f);
// 			Player.player1.bloodBank.AddBlood(bloodAmount);
// 			this.DestroyGameObjGracefully();
// 		}
// 	}
// }