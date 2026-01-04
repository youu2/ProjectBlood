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
				AudioKit.PlaySound("Torch Attack Strike 1");
				_currentSeconds = 0;
				var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				foreach (var enemy in enemies)
				{
					//float distance = (Player.player1.transform.position - enemy.transform.position).magnitude;
					float distance = Vector2.Distance(Player.player1.transform.position, enemy.transform.position);
					if (distance <= _attackRange)
					{
						// enemy.TakeDamage(Global.BlazingCircleDamage.Value);
						// //enemy.Sprite.color = Color.white;
						//enemy.getSprite().color = Color.white;
						var enemyRefCache = enemy;
						enemyRefCache.TakeDamage(Global.BlazingCircleDamage.Value);
						// ActionKit.Delay(0.3f, () =>
						// {
						// 	enemyRefCache.getSprite().color = Color.red;
						// }).StartGlobal();
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
