using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class HitBox : ViewController
	{
		public GameObject owner;
		void Start()
		{
			if (owner == null)
			{
				owner = this.transform.parent.gameObject;
			}
		}
	}
}
