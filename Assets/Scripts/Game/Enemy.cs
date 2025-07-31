using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class Enemy : ViewController
	{
		public float moveSpeed = 2.0f;
		void Start()
		{
			// Code Here
		}
		void Update()
		{
			var player = FindObjectOfType<Player>();
			var direction = (player.transform.position - transform.position).normalized;
			transform.Translate(direction*Time.deltaTime*moveSpeed);
        }
    }
}
