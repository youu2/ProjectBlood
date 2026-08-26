using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class EnemyFactory : ViewController
    {
        public static EnemyFactory Instance;
        public List<GameObject> enemyList = new();
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
            if (score == 1) return Instantiate(Instance.enemyList[0]).Hide();
            if (score == 2) return Instantiate(Instance.enemyList[1]).Hide();
            if (score == 3) return Instantiate(Instance.enemyList[2]).Hide();
            if (score == 4) return Instantiate(Instance.enemyList[3]).Hide();
            return null;
        }

    }
}
