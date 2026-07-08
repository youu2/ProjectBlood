using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class BloodBank
    {
        private static BloodBank instance;
        public static BloodBank Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BloodBank();
                }
                return instance;
            }
        }

        public int MaxBloodAmount = 100;
        public int CurrentBloodAmount = 100;
        public void AddBlood(int amount)
        {
            CurrentBloodAmount += amount;
            CurrentBloodAmount = Mathf.Clamp(CurrentBloodAmount, 0, MaxBloodAmount);
        }
        public void RemoveBlood(int amount)
        {
            CurrentBloodAmount -= amount;
            CurrentBloodAmount = Mathf.Clamp(CurrentBloodAmount, 0, MaxBloodAmount);
        }
    }
}