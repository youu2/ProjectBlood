using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class BlazingCircle : ViewController
	{
		private float _currentSeconds = 0;
		private float _attackRange = 5;
		private float _attackDamage = 20;
		private float _AttackInterval = 2;
		void Start()
		{
			// Code Here
		}

		void Update()
		{
			_currentSeconds += Time.deltaTime;
			if (_currentSeconds >= _AttackInterval)
			{
				_currentSeconds = 0;
				var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				foreach (var enemy in enemies)
				{
					//float distance = (Player.player1.transform.position - enemy.transform.position).magnitude;
					float distance = Vector2.Distance(Player.player1.transform.position, enemy.transform.position);
					if (distance <= _attackRange)
					{
						enemy.TakeDamage(_attackDamage);
						//enemy.Sprite.color = Color.white;
					}

				}
			}
		}
    }
}
