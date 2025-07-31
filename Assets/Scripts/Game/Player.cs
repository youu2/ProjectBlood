using UnityEngine;
using QFramework;
using UnityEditor.Rendering;

namespace ProjectBlood
{
	public partial class Player : ViewController
	{
		public float moveSpeed = 3.0f;
		void Start()
		{
			// Code Here
			"Hello world".LogInfo();
		}
		void Update()
		{
			float horizontal = Input.GetAxis("Horizontal"); // A/D
			float vertical = Input.GetAxis("Vertical");     // W/S

			// keep same speed in any direction
			var direction = new Vector2(horizontal, vertical).normalized;

			SelfRigidbody2D.velocity = direction * moveSpeed;
        }

	}
}
