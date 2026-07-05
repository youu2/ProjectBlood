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

		public static GameObject EnemyByScore(int score)
		{
			if (score == 1) return Instantiate(Instance.SlimeBlue).Hide();
			if (score == 2) return Instantiate(Instance.AG).Hide();
			if (score == 3) return Instantiate(Instance.Compiler).Hide();
			if (score == 4) return Instantiate(Instance.DemonI).Hide();
			return null;
		}

	}
}
