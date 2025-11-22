using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class Global
    {
        public static BindableProperty<int> Exp = new BindableProperty<int>(0);
        public static BindableProperty<int> Level = new BindableProperty<int>(1);
        public static BindableProperty<float> BlazingCircleDamage = new BindableProperty<float>(35.0f);
        public static BindableProperty<float> RemainingTime  = new BindableProperty<float>(180);
        public static BindableProperty<int> currentNum = new BindableProperty<int>(0);    // current number of active enemies
        public static BindableProperty<int> cumulativeNum = new BindableProperty<int>(0);   // cumulative number of generated enemies so far
        public static BindableProperty<int> CurrentWaves = new BindableProperty<int>(1);
        //[SerializeField] private int maxWavesNum = 3;   // The total number of enemy waves generated
        public static BindableProperty<int> maxWavesNum = new BindableProperty<int>(10);  // The total number of enemy waves generated
        public static BindableProperty<float> BCAttackInterval = new BindableProperty<float>(1.5f); // attack interval of Blazing Circle
        public static BindableProperty<int> MAX_EXP = new BindableProperty<int>(5);

        // level up after getting 5 exp, then increase the required exp by 10%
        public static void AddExp(int amount)
        {
            Exp.Value += amount;

            if (Exp.Value >= MAX_EXP.Value)
            {
                Level.Value++;
                Exp.Value -= MAX_EXP.Value;
                MAX_EXP.Value = Mathf.CeilToInt(MAX_EXP.Value * 1.1f);
                //Debug.Log("Level Up! current LV: " + Level.Value);
            }
        }

        // restart game
        public static void ResetLevel()
        {
            Level.Value = 1;
            Exp.Value = 0;
            Time.timeScale = 1;
            RemainingTime.Value = 180;
            CurrentWaves.Value = 1;
            cumulativeNum.Value = 0;
            currentNum.Value = 0;
            BlazingCircleDamage.Value = 35.0f;
            BCAttackInterval.Value = 1.5f;
            MAX_EXP.Value = 5;
        }

        public static void ResetWave()
        {
            Time.timeScale = 1;
            // RemainingTime.Value = 180;
            cumulativeNum.Value = 0;
        }

        public static void GenerateExp(GameObject enemy)
        {
            DropManager.Instance.Exp.Instantiate()
                .Position(enemy.Position())
                .Show();
        }
    }

}
