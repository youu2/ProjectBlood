using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class Global : Architecture<Global>
    {
        public static BindableProperty<float> INIT_MAX_HP = new BindableProperty<float>(30.0f);
        public static BindableProperty<float> INGAME_MAX_HP = new BindableProperty<float>(INIT_MAX_HP.Value);
        public static BindableProperty<float> currentHP = new BindableProperty<float>(INGAME_MAX_HP.Value);
        public static BindableProperty<int> Exp = new BindableProperty<int>(0);
        public static BindableProperty<int> Coin = new BindableProperty<int>(0);
        public static BindableProperty<int> Level = new BindableProperty<int>(1);

        // The player's level will be converted into Legacy points upon death, which can be used for Metaprogression System.
        public static BindableProperty<int> LegacyPoint = new BindableProperty<int>(0);
        public static BindableProperty<float> BlazingCircleDamage = new BindableProperty<float>(35.0f);
        public static BindableProperty<float> RemainingTime = new BindableProperty<float>(180);
        public static BindableProperty<int> currentNum = new BindableProperty<int>(0);    // current number of active enemies
        public static BindableProperty<int> cumulativeNum = new BindableProperty<int>(0);   // cumulative number of generated enemies so far
        public static BindableProperty<int> CurrentWaves = new BindableProperty<int>(1);
        //[SerializeField] private int maxWavesNum = 3;   // The total number of enemy waves generated
        public static BindableProperty<int> maxWavesNum = new BindableProperty<int>(5);  // The total number of enemy waves generated
        public static BindableProperty<float> BCAttackInterval = new BindableProperty<float>(1.5f); // attack interval of Blazing Circle
        public static BindableProperty<int> MAX_EXP = new BindableProperty<int>(5);
        public static BindableProperty<float> CoinDropRate = new BindableProperty<float>(0.30f); // 30% chance to drop coins
        public static Room currentRoom;
        public static BindableProperty<bool> FireEnabled = new BindableProperty<bool>(true);
        public static int currentDifficulty;    // 0 - 9 共10个难度等级
        public static List<LevelsConfig> LevelConfigs = new List<LevelsConfig>();
        public static bool IsGamePaused = false;
        public static float WeaponAdditionalCameraSize = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            // Initialize AudioKit, ResKit, UIKit
            // Set AudioKit to ignore same sound played in the same frame
            AudioKit.PlaySoundMode = AudioKit.PlaySoundModes.IgnoreSameSoundInSoundFrames;
            ResKit.Init();
            UIKit.Root.SetResolution(1920, 1080, 1.0f);
            // Load from PlayerPrefs
            Global.LegacyPoint.Value = PlayerPrefs.GetInt("LegacyPoint", 0);
            Global.CoinDropRate.Value = PlayerPrefs.GetFloat("CoinDropRate", 0.30f);
            Global.INIT_MAX_HP.Value = PlayerPrefs.GetFloat("INIT_MAX_HP", 30.0f);

            currentDifficulty = 0;

            LevelConfigs.Clear();
            LevelConfigs.Add(Level1_1.Config);
            LevelConfigs.Add(Level1_2.Config);
            LevelConfigs.Add(Level1_3.Config);
            LevelConfigs.Add(Level2_1.Config);
            LevelConfigs.Add(Level2_2.Config);
            LevelConfigs.Add(Level2_3.Config);
            LevelConfigs.Add(Level3_1.Config);
            LevelConfigs.Add(Level3_2.Config);
            LevelConfigs.Add(Level3_3.Config);

            // Register change callbacks
            LegacyPoint.Register(legacy =>
            {
                PlayerPrefs.SetInt("LegacyPoint", legacy);
            });

            CoinDropRate.Register(coinDropRate =>
            {
                PlayerPrefs.SetFloat("CoinDropRate", coinDropRate);
            });

            INIT_MAX_HP.Register(maxHP =>
            {
                PlayerPrefs.SetFloat("INIT_MAX_HP", maxHP);
            });
        }

        // level up after getting 5 exp, then increase the required exp by 10%
        public static void AddExp(int amount)
        {
            Exp.Value += amount;

            if (Exp.Value >= MAX_EXP.Value)
            {
                AudioKitManager.Instance.PlayOneShot("LevelUp", volume: 0.5f);
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

        public static void SpendCoin(int amount)
        {
            Coin.Value -= amount;
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
            PlayerUpgrade.ResetUpgrade();
            currentHP.Value = INGAME_MAX_HP.Value;
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
            currentDifficulty = 0;
            WeaponDataSystem.weaponDataList.Clear();
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.DE.NewWeapon()); // 默认武器只有DE
            Player.player1.UpdateSpecialReloadCost();   // 更新玩家的特殊装弹成本
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

        public static void GenerateDirtyBlood(GameObject enemy)
        {
            DropManager.Instance.DirtyBlood.Instantiate()
                .Position(enemy.Position() + new Vector3(-0.5f, -0.5f, 0))  // slight offset for better visibility
                .Show();
        }

        // 武器吸血击杀时，从敌人死亡位置生成多个 PB 道具
        // 总治疗量 = 敌人总生命值 × 武器吸血百分比
        // 数量 = 1 + floor(总治疗量 / 5)
        // 单个 PB 治疗量 = 总治疗量 / 数量（保证 PB 治疗总和等于原吸血治疗量）
        public static void GeneratePureBlood(GameObject enemy, float totalLifestealAmount)
        {
            if (DropManager.Instance == null || DropManager.Instance.PureBlood == null) return;
            if (enemy == null) return;
            if (totalLifestealAmount <= 0f) return;

            int count = 1 + Mathf.FloorToInt(totalLifestealAmount / 5f);
            if (count <= 0) return;
            float healPerPB = totalLifestealAmount / count;

            Vector3 origin = enemy.Position();
            for (int i = 0; i < count; i++)
            {
                var pb = DropManager.Instance.PureBlood.Instantiate()
                    .Position(origin);

                pb.Initialize(healPerPB);

                pb.Show();
            }
        }

        public static void GenerateShield(GameObject enemy)
        {
            DropManager.Instance.Shield.Instantiate()
                .Position(enemy.Position() + new Vector3(0, 0.5f, 0))  // slight offset for better visibility
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
            // 掉落Shield, 5%概率
            rand = Random.Range(0f, 100.0f);
            if (rand < 70f) // 测试 ///////////////////////////////////////   
            {
                GenerateShield(enemy);
                return;
            }
            // 只有当血库血量低于30%时才有可能掉落dirtyBlood
            if (BloodBank.Instance != null)
            {
                float bloodPercent = (float)BloodBank.Instance.CurrentBloodAmount / BloodBank.Instance.MaxBloodAmount;
                if (bloodPercent < 0.3f)
                {
                    rand = Random.Range(0f, 100.0f);
                    if (rand < 0.5 * 100)
                    {
                        GenerateDirtyBlood(enemy);
                        return;
                    }
                }
            }
        }

        protected override void Init()
        {
            throw new System.NotImplementedException();
        }

        public static void AddHP(float amount)
        {
            currentHP.Value += amount;
            if (currentHP.Value > INGAME_MAX_HP.Value)
            {
                currentHP.Value = INGAME_MAX_HP.Value;
            }
        }

        public static void AddAnnihilationCore(int amount)
        {
            // Implementation for adding Annihilation Cores
            // This is a placeholder; actual implementation may vary
            Debug.Log("Annihilation Cores increased by " + amount);
        }

        public static void UpdateCameraSize(float size)
        {
            Camera.main.orthographicSize = size;
        }
    }
}
