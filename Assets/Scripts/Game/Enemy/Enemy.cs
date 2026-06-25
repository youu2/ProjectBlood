// 基础近战敌人
using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
	public partial class Enemy : ViewController, IDamageable
	{
		[SerializeField] protected SpriteRenderer body;
		protected SpriteRenderer spriteRenderer;
		public float moveSpeed = 2.0f;
		public float currentHealth = 100.0f;
		public float maxHealth = 100.0f; // 敌人总生命值，记录初始血量用于吸血 PB 换算
		public float Damage = 5.0f;
		protected Color originalColor;  // Restore the original color after flash
		protected bool isDying = false; // Avoid repeating death process.
		protected Collider2D[] allColliders;
		private Rigidbody2D rb;
		protected Vector3 direction;
		[Tooltip("是否使用翻转来朝向玩家（关闭则直接旋转）")]
		public bool useFlipSprite = true;

		protected virtual void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			rb = GetComponent<Rigidbody2D>();

			// get all 2D colliders on itself and child objects
			allColliders = GetComponentsInChildren<Collider2D>(true);

			if (spriteRenderer == null)
				Debug.LogError("Enemy: SpriteRenderer not found!");

			originalColor = spriteRenderer.color;

			// 记录敌人总生命值（仅在第一次初始化时记录，避免被多次实例化重置）
			if (maxHealth <= 0f)
			{
				maxHealth = currentHealth;
			}
		}

		void Start()
		{
			// Code Here
		}

		protected virtual void FixedUpdate()
		{
			if (Player.player1)
			{
				direction = (Player.player1.transform.position - transform.position).normalized;
				//transform.Translate(direction * Time.deltaTime * moveSpeed);
				SelfRigidbody2D.velocity = direction * moveSpeed;
			}
		}

		void Update()
		{
			UpdateRotate(direction);
			if (isDying) return;  // stop moving during death process
		}

		// 更新朝向面向玩家
		public virtual void UpdateRotate(Vector3 dirToPlayer)
		{
			if (spriteRenderer != null)
			{
				if (useFlipSprite)
				{
					spriteRenderer.flipX = dirToPlayer.x < 0;
				}
				else
				{
					float targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
					float currentAngle = transform.eulerAngles.z;
					float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, 180f * Time.deltaTime / 180f);
					transform.eulerAngles = new Vector3(0, 0, newAngle);
				}
			}
		}

		// 敌人受伤
		public void TakeDamage(float Damage, Vector2 HitDir)
		{
			if (isDying) return;
			AudioKitManager.Instance.PlayOneShot("Torch Impact 2", volume: 0.5f);
			FxManager.PlayEnemyHurtFX(transform.Position2D());
			FxManager.DrawEnemyBlood(transform.Position2D());
			this.currentHealth -= Damage;
			if (currentHealth <= 0f)
			{
				Death(HitDir);
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

		protected virtual void Death(Vector2 HitDir)
		{
			AudioKitManager.Instance.PlayOneShot("KillSFX", volume: 0.6f);
			Global.GenerateDrops(this.gameObject);
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

			FxManager.SpawnEnemyBody(body, transform.Position2D(), HitDir);

			Global.currentNum.Value -= 1;
			this.DestroyGameObjGracefully();
		}

		// Death process (Flash first, then destroy, avoid enemy disappearing directly)
		// protected virtual IEnumerator DeathSequence()
		// {
		// 	// Drop experience item, destroy enemy
		// 	// Global.GenerateExp(this.gameObject);
		//     if (Room != null)
		//     {
		//         Room.GetEnemies().Remove(this);
		//     }
		//     isDying = true;
		//     moveSpeed = 0f;
		// 	if (allColliders != null)
		//     {
		//         foreach (var c in allColliders) if (c) c.enabled = false;
		//     }

		//     // flash before death
		// 	if (spriteRenderer != null)
		// 	{
		// 		for (int i = 0; i < 3; i++)
		// 		{
		// 			spriteRenderer.color = Color.white;

		// 			yield return new WaitForSeconds(0.08f);

		// 			spriteRenderer.color = Color.red;

		// 			yield return new WaitForSeconds(0.08f);
		// 		}
		// 	}
		//     yield return new WaitForSeconds(0.15f);
		// 	// enemySpawner.EnemyDestroyed();
		// 	Global.currentNum.Value -= 1;
		//     this.DestroyGameObjGracefully();
		// }

		// public SpriteRenderer GetSprite()
		// {
		// 	return spriteRenderer;
		// }

		public float HitDamage { get => Damage; }
		public bool IsDying { get => isDying; }
		public GameObject GameObject { get => gameObject; }
		public Room Room { get; set; }
		public float CurrentHealth { get => currentHealth; }
		public float MaxHealth { get => maxHealth; }
		public virtual void OnDestroy()
		{
			if (Room != null)
			{
				Room.GetEnemies().Remove(this);
			}
		}
	}
}
