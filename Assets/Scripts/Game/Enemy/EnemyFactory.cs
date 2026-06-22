using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class EnemyFactory : ViewController
	{
		public static EnemyFactory Instance;
		[SerializeField] public GameObject SlimeBlue;
		[SerializeField] public GameObject AG;
		[SerializeField] public GameObject Compiler;
		[SerializeField] public GameObject DemonI;
		void Awake()
		{
			Instance = this;
		}

		void OnDestroy()
		{
			Instance = null;
		}

		// public static IDamageable GetSlimeBlue()
		// {
		// 	return Instance.SlimeBlue;
		// }
		public static GameObject EnemyByScore(int score)
		{
			if (score == 1) return Instance.SlimeBlue;
			if (score == 2) return Instance.AG;
			if (score == 3) return Instance.Compiler;
			if (score == 4) return Instance.DemonI;
			return null;
		}

	}
}
