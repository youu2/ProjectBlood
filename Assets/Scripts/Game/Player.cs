using UnityEngine;
using QFramework;
using UnityEditor.Rendering;
using Unity.VisualScripting;

namespace ProjectBlood
{
	public partial class Player : ViewController
	{
		public float moveSpeed = 3.5f;
		public static Player player1;
		private void Awake()
		{
			player1 = this;
		}
		void Start()
		{
			HitBox.OnTriggerEnter2DEvent((Collider2D col)=>
			{
				var hitBox = col.GetComponent<HitBox>();
				// If the object colliding with the player 
				// does not have the ability to cause damage, skip the death process.
				if (hitBox == null) return;
				this.DestroyGameObjGracefully();
				UIKit.OpenPanel<UIGameOverPanel>();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);
		}
		void Update()
		{
			float horizontal = Input.GetAxis("Horizontal"); // A/D
			float vertical = Input.GetAxis("Vertical");     // W/S

			// keep same speed in any direction
			var direction = new Vector2(horizontal, vertical).normalized;

			SelfRigidbody2D.velocity = direction * moveSpeed;
		}

        private void OnDestroy()
        {
			player1 = null;
        }

    }
}
