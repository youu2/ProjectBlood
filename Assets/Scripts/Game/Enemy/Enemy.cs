// 基础近战敌人
using UnityEngine;
using QFramework;
using System.Collections;

namespace ProjectBlood
{
	public partial class Enemy : ViewController, IDamageable
	{
		[Header("=== 基础敌人设置 ===")]
		[SerializeField] protected SpriteRenderer body;
		protected SpriteRenderer spriteRenderer; // 用于朝向控制
		[SerializeField] public float moveSpeed = 2.0f;
		public float currentHealth;
		public float maxHealth = 100.0f; // 敌人总生命值，记录初始血量用于吸血 PB 换算
		[SerializeField] protected float Damage = 5.0f; // 用于直接造成伤害的敌人, 子弹碰撞在子弹脚本中处理
		protected Vector3 direction;    // 敌人朝向玩家的方向
		[Tooltip("是否使用翻转来朝向玩家（关闭则直接旋转）")]
		public bool useFlipSprite = true;
		[Tooltip("攻击距离通常要比追击距离远一点, 避免Wander期间敌人自己走出攻击范围")]
		[SerializeField] protected float attackRange = 12f;  // 攻击范围:超出这个距离回到Chase状态
		[SerializeField] protected float chaseRange = 10f;    // 追击范围:进入这个距离切换到Wander状态
		[SerializeField] protected float WanderDuration = 2.0f;
		protected float currentWanderTime = 0.0f;
		protected Vector3 wanderDirection = Vector3.right;

		public enum State
		{
			Idle,   // 空闲状态
			Chase,  // 追逐玩家
			Wander, // 游走，结束时开启充能
			Fire    // 攻击
		}
		public State currentState = State.Idle;

		protected virtual void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();  // 用于朝向控制
			currentHealth = maxHealth;
			if (Player.player1 != null) currentState = State.Chase;
		}

		protected virtual void Update()
		{

			if (Player.player1 == null)
			{
				currentState = State.Idle;
				return;
			}

			direction = GetDirectionToPlayer();
			UpdateRotate(direction);
			float distanceToPlayer = GetDistanceToPlayer();

			switch (currentState)
			{
				case State.Chase:
					UpdateChase(distanceToPlayer);
					break;
				case State.Wander:
					UpdateWander(distanceToPlayer);
					break;
				case State.Fire:
					UpdateFire(distanceToPlayer);
					break;
			}
		}

		protected float GetDistanceToPlayer()
		{
			return Vector3.Distance(transform.position, Player.player1.transform.position);
		}

		protected virtual void UpdateChase(float distanceToPlayer)
		{
			transform.position += direction * moveSpeed * Time.deltaTime;
			if (distanceToPlayer <= chaseRange)
			{
				currentState = State.Wander;
				StartWander();
			}
		}

		protected virtual void StartWander()
		{
			currentWanderTime = 0f;
			Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
			wanderDirection = Random.Range(0, 2) == 0 ? perpendicular : -perpendicular;
		}

		protected virtual void UpdateWander(float distanceToPlayer)
		{
			if (Player.player1 == null) return;
			if (currentWanderTime >= WanderDuration)
			{
				StartFire();
			}
			if (distanceToPlayer > attackRange)
			{
				currentState = State.Chase;
			}
			transform.position += wanderDirection * moveSpeed * Time.deltaTime;
			currentWanderTime += Time.deltaTime;

		}

		protected virtual void StartFire()
		{
			currentState = State.Fire;
		}

		protected virtual void MakeDamage()
		{
			Player.player1?.TakeDamage(HitDamage);
		}

		protected virtual void UpdateFire(float distanceToPlayer)
		{
			if (Player.player1 == null) return;
			if (distanceToPlayer > attackRange)
			{
				currentState = State.Chase;
			}
		}

		protected Vector3 GetDirectionToPlayer()
		{
			if (Player.player1 == null)
				return transform.right;
			return (Player.player1.transform.position - transform.position).normalized;
		}

		// 更新朝向面向玩家
		public virtual void UpdateRotate(Vector3 dirToPlayer)
		{
			if (dirToPlayer.x == 0 && dirToPlayer.y == 0) return;
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
		public void TakeDamage(float damage, Vector2 HitDir)
		{
			AudioKitManager.Instance.PlayOneShot("Torch Impact 2", volume: 0.5f);
			FxManager.PlayEnemyHurtFX(transform.Position2D());
			FxManager.DrawEnemyBlood(transform.Position2D());
			currentHealth -= damage;
			if (currentHealth <= 0f)
			{
				Death(HitDir);
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
			moveSpeed = 0f;

			FxManager.SpawnEnemyBody(body, transform.Position2D(), HitDir);

			Global.currentNum.Value -= 1;
			this.DestroyGameObjGracefully();
		}

		public float HitDamage { get => Damage; }
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