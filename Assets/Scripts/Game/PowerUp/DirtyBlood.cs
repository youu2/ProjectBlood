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
			if (BloodBank.Instance.CurrentBloodAmount >= BloodBank.Instance.MaxBloodAmount)
			{
				return;
			}
			AudioKitManager.Instance.PlayOneShot("DirtyBloodPickup", volume: 0.6f);
			BloodBank.Instance.AddBlood(bloodAmount);
			this.DestroyGameObjGracefully();
		}
	}
}