using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
	public partial class Enemy : ViewController
	{
		private SpriteRenderer spriteRenderer;
		public float moveSpeed = 2.0f;
		public float currentHealth = 100.0f;
		private Color originalColor;

        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

			if (spriteRenderer == null)
            Debug.LogError("Enemy: SpriteRenderer not found!");

        	originalColor = spriteRenderer.color;
        }

        void Start()
		{
			// Code Here
		}
		void Update()
		{
			if (Player.player1)
			{
				var direction = (Player.player1.transform.position - transform.position).normalized;
				transform.Translate(direction * Time.deltaTime * moveSpeed);
			}

			// death of enemy
			if (currentHealth <= 0)
			{
				this.DestroyGameObjGracefully();
				Global.AddExp(1);
				// UIKit.OpenPanel<UIGamePassPanel>();
			}
		}

		public void TakeDamage(float Damage)
		{
			this.currentHealth -= Damage;
			StartCoroutine(FlashWhite());
		}

		// flash after the enemy is hit
		private IEnumerator FlashWhite()
		{
			spriteRenderer.color = Color.white;  // flash
			//Debug.Log("Flash start");
			yield return new WaitForSeconds(0.15f);
			spriteRenderer.color = originalColor; // restore original color
			//Debug.Log("Flash end");
		}
    }
}
