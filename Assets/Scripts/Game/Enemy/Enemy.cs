using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
	public partial class Enemy : ViewController, IDamageable
	{
		protected SpriteRenderer spriteRenderer;
		public float moveSpeed = 2.0f;
		public float currentHealth = 100.0f;
		public float Damage = 5.0f;
		protected Color originalColor;  // Restore the original color after flash
		protected bool isDying = false; // Avoid repeating death process.
		protected Collider2D[] allColliders;
        private Rigidbody2D rb;

		void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			rb = GetComponent<Rigidbody2D>();

            // get all 2D colliders on itself and child objects
            allColliders = GetComponentsInChildren<Collider2D>(true);

			if (spriteRenderer == null)
				Debug.LogError("Enemy: SpriteRenderer not found!");

			originalColor = spriteRenderer.color;
		}

		void Start()
		{
			// Code Here
		}

        protected virtual void FixedUpdate()
        {
            if (Player.player1)
			{
				var direction = (Player.player1.transform.position - transform.position).normalized;
				//transform.Translate(direction * Time.deltaTime * moveSpeed);
				SelfRigidbody2D.velocity = direction * moveSpeed;
			}
        }

        void Update()
		{
			if (isDying) return;  // stop moving during death process
		}

		public void TakeDamage(float Damage)
		{
			if (isDying) return;
			AudioKit.PlaySound("Torch Impact 2", volume: 0.5f);
			this.currentHealth -= Damage;
			if (currentHealth <= 0f)
			{
				// Drop experience item, destroy enemy
				Global.GenerateDrops(this.gameObject);
                StartCoroutine(DeathSequence()); // Resulting in death: Start the death coroutine
            }
            else
            {
                StartCoroutine(FlashWhite());    // flash after hit
            }
		}

		// flash after the enemy is hit
		protected virtual IEnumerator FlashWhite()
		{
			if (spriteRenderer != null)
			{
				spriteRenderer.color = Color.white;  // flash
				yield return new WaitForSeconds(0.18f);
				spriteRenderer.color = originalColor; // restore original color
			}
		}

		// Death process (Flash first, then destroy, avoid enemy disappearing directly)
        protected virtual IEnumerator DeathSequence()
		{
			// Drop experience item, destroy enemy
			// Global.GenerateExp(this.gameObject);
            if (Room != null)
            {
                Room.GetEnemies().Remove(this);
            }
            isDying = true;
            moveSpeed = 0f;
			if (allColliders != null)
            {
                foreach (var c in allColliders) if (c) c.enabled = false;
            }

            // flash before death
			if (spriteRenderer != null)
			{
				for (int i = 0; i < 3; i++)
				{
					spriteRenderer.color = Color.white;

					yield return new WaitForSeconds(0.08f);

					spriteRenderer.color = Color.red;

					yield return new WaitForSeconds(0.08f);
				}
			}
            yield return new WaitForSeconds(0.15f);
			// enemySpawner.EnemyDestroyed();
			Global.currentNum.Value -= 1;
            this.DestroyGameObjGracefully();
        }

		public SpriteRenderer getSprite()
		{
			return spriteRenderer;
		}

		public float HitDamage { get => Damage; }
		public bool IsDying { get => isDying; }
		public GameObject GameObject { get => gameObject; }
		public Room Room { get; set; }
        public void OnDestroy()
        {
            if (Room != null)
            {
                Room.GetEnemies().Remove(this);
            }
        }
    }
}
