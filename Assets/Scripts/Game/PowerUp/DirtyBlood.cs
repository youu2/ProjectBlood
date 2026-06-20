using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class DirtyBlood : DropItem
	{
		public int bloodAmount = 30;
		public void Awake()
		{
			price = 5;
		}
		protected override void Collect()
		{
			if (Player.player1.bloodBank.CurrentBloodAmount >= Player.player1.bloodBank.MaxBloodAmount)
			{
				return;
			}
			AudioKitManager.Instance.PlayOneShot("DirtyBloodPickup", volume: 0.6f);
			Player.player1.bloodBank.AddBlood(bloodAmount);
			this.DestroyGameObjGracefully();
		}
	}
}