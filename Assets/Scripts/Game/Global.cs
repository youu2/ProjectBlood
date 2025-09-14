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

        private const int MAX_EXP = 5;


        // upgrade after getting 5 exp
        public static void AddExp(int amount)
        {
            Exp.Value += amount;

            if (Exp.Value >= MAX_EXP)
            {
                Level.Value++;
                Exp.Value -= MAX_EXP;

                //Debug.Log("Level Up! current LV: " + Level.Value);
            }
        }

        public static void ResetLevel()
        {
            Level.Value = 1;
            Exp.Value = 0;
            Time.timeScale = 1;
            RemainingTime.Value = 180;
        }


    }

}
