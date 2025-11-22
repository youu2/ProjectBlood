using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Exp : ViewController
	{
		void Start()
		{
			// Code Here
		}
		private void OnTriggerEnter2D(Collider2D collider)
		{
			// Check if the collider belongs to the player
            if (collider.GetComponent<CollectBox>() != null)
            {
                Global.AddExp(1);
				this.DestroyGameObjGracefully();
            }
			// if (collider.gameObject.CompareTag("Player"))
			// {
			// 	Global.AddExp(1);
			// 	this.DestroyGameObjGracefully();
			// }
		}
	}
}
