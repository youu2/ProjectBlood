using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class Global : Architecture<Global>
    {
        public static BindableProperty<float> MAX_HP = new BindableProperty<float>(3);
        public static BindableProperty<float> currentHP = new BindableProperty<float>(3);
        public static BindableProperty<int> Exp = new BindableProperty<int>(0);
        public static BindableProperty<int> Coin = new BindableProperty<int>(0);
        public static BindableProperty<int> Level = new BindableProperty<int>(1);

        // The player's level will be converted into Legacy points upon death, which can be used for Metaprogression System.
        public static BindableProperty<int> LegacyPoint = new BindableProperty<int>(0);
        public static BindableProperty<float> BlazingCircleDamage = new BindableProperty<float>(35.0f);
        public static BindableProperty<float> RemainingTime  = new BindableProperty<float>(180);
        public static BindableProperty<int> currentNum = new BindableProperty<int>(0);    // current number of active enemies
        public static BindableProperty<int> cumulativeNum = new BindableProperty<int>(0);   // cumulative number of generated enemies so far
        public static BindableProperty<int> CurrentWaves = new BindableProperty<int>(1);
        //[SerializeField] private int maxWavesNum = 3;   // The total number of enemy waves generated
        public static BindableProperty<int> maxWavesNum = new BindableProperty<int>(1);  // The total number of enemy waves generated
        public static BindableProperty<float> BCAttackInterval = new BindableProperty<float>(1.5f); // attack interval of Blazing Circle
        public static BindableProperty<int> MAX_EXP = new BindableProperty<int>(5);
        public static BindableProperty<float> CoinDropRate = new BindableProperty<float>(0.30f); // 30% chance to drop coins

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            // Initialize AudioKit, ResKit, UIKit
            // Set AudioKit to ignore same sound played in the same frame
            AudioKit.PlaySoundMode = AudioKit.PlaySoundModes.IgnoreSameSoundInGlobalFrames;
            ResKit.Init();
            UIKit.Root.SetResolution(1920, 1080,1.0f);
            // Load from PlayerPrefs
			Global.LegacyPoint.Value = PlayerPrefs.GetInt("LegacyPoint", 0);
            Global.CoinDropRate.Value = PlayerPrefs.GetFloat("CoinDropRate", 0.30f);
            Global.MAX_HP.Value = PlayerPrefs.GetFloat("MAX_HP", 3.0f);
            // Register change callbacks
			LegacyPoint.Register(legacy =>
			{
				PlayerPrefs.SetInt("LegacyPoint", legacy);
			});

			CoinDropRate.Register(coinDropRate =>
			{
				PlayerPrefs.SetFloat("CoinDropRate", coinDropRate);
			});

            MAX_HP.Register(maxHP =>
			{
				PlayerPrefs.SetFloat("MAX_HP", maxHP);
			});
        }

        // level up after getting 5 exp, then increase the required exp by 10%
        public static void AddExp(int amount)
        {
            Exp.Value += amount;

            if (Exp.Value >= MAX_EXP.Value)
            {
                AudioKit.PlaySound("LevelUp");
                Level.Value++;
                Exp.Value -= MAX_EXP.Value;
                MAX_EXP.Value = Mathf.CeilToInt(MAX_EXP.Value * 1.1f);
                //Debug.Log("Level Up! current LV: " + Level.Value);
            }
        }

        // add coins
        public static void AddCoin(int amount)
        {
            Coin.Value += amount;
        }

        public static void SettleLegacyPoints()
        {
            int legacyPointsGained = Level.Value - 1; // Gain Legacy points equal to the number of upgrades upon death
            LegacyPoint.Value += legacyPointsGained;
            Debug.Log("You have gained " + legacyPointsGained + " Legacy Points!");
            Debug.Log("Your current legacy points: " + LegacyPoint.Value);
        }
        
        // restart game
        public static void ResetLevel()
        {
            currentHP.Value = MAX_HP.Value;
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
            Coin.Value = 0;
        }

        public static void ResetWave()
        {
            Time.timeScale = 1;
            // RemainingTime.Value = 180;
            cumulativeNum.Value = 0;
        }

        // Generate drops when enemy dies
        public static void GenerateExp(GameObject enemy)
        {
            DropManager.Instance.Exp.Instantiate()
                .Position(enemy.Position())
                .Show();
        }

        public static void GenerateCoin(GameObject enemy)
        {
            DropManager.Instance.Coin.Instantiate()
                .Position(enemy.Position() + new Vector3(0.5f, 0.5f, 0))  // slight offset for better visibility
                .Show();
        }
        
        public static void GenerateHealthPotion(GameObject enemy)
        {
            DropManager.Instance.HealthPotion.Instantiate()
                .Position(enemy.Position() + new Vector3(-0.5f, -0.5f, 0))  // slight offset for better visibility
                .Show();
        }

        public static void GenerateDrops(GameObject enemy)
        {
            GenerateExp(enemy);
            var rand = Random.Range(0f, 100.0f);
            if (rand < CoinDropRate.Value * 100)
            {
                GenerateCoin(enemy);
                return;
            }
            rand = Random.Range(0f, 100.0f);
            if (rand < 0.1 * 100)
            {
                GenerateHealthPotion(enemy);
                return;
            }
        }

        protected override void Init()
        {
            throw new System.NotImplementedException();
        }

        public static void AddHP(float amount)
        {
            currentHP.Value += amount;
            if (currentHP.Value > MAX_HP.Value)
            {
                currentHP.Value = MAX_HP.Value;
            }
        }

        public static void AddAnnihilationCore(int amount)
        {
            // Implementation for adding Annihilation Cores
            // This is a placeholder; actual implementation may vary
            Debug.Log("Annihilation Cores increased by " + amount);
        }
        
    }
}
