using UnityEngine;
using QFramework;
using System;

namespace ProjectBlood
{
	public partial class BlazingCircle : ViewController
	{
		private float _currentSeconds = 0;
		private float _attackRange = 5;
		//private float _attackDamage = 35;
		//private float _AttackInterval = 1.5f;
		void Start()
		{
			// Code Here
		}

		void Update()
		{
			_currentSeconds += Time.deltaTime;
			if (_currentSeconds >= Global.BCAttackInterval.Value)
			{
				AudioKitManager.Instance.PlayOneShot("Torch Attack Strike 1");
				_currentSeconds = 0;
				var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				foreach (var enemy in enemies)
				{
					//float distance = (Player.player1.transform.position - enemy.transform.position).magnitude;
					float distance = Vector2.Distance(Player.player1.transform.position, enemy.transform.position);
					if (distance <= _attackRange)
					{
						var enemyRefCache = enemy;
						// 计算击退方向：从玩家到敌人的方向
						Vector2 playerToEnemyDir = (enemy.transform.position - Player.player1.transform.position).normalized;
						enemyRefCache.TakeDamage(Global.BlazingCircleDamage.Value, playerToEnemyDir);
					}

				}
			}
		}

		// public void upgrade()
		// {
		// 	_attackDamage += 10;
		// }
    }
}
