using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class Global : Architecture<Global>
    {
        // 局外养成运行时属性：由 LegacyUpgradeState.ApplyEffect 按养成等级写入，勿在此直接持久化
        public static BindableProperty<float> INIT_MAX_HP = new BindableProperty<float>(30.0f);
        public static BindableProperty<float> INGAME_MAX_HP = new BindableProperty<float>(INIT_MAX_HP.Value);
        public static BindableProperty<float> currentHP = new BindableProperty<float>(INGAME_MAX_HP.Value);
        public static BindableProperty<int> Exp = new BindableProperty<int>(0);
        public static BindableProperty<int> Coin = new BindableProperty<int>(0);
        public static BindableProperty<int> Level = new BindableProperty<int>(1);

        public static BindableProperty<float> BlazingCircleDamage = new BindableProperty<float>(35.0f);
        public static BindableProperty<float> RemainingTime = new BindableProperty<float>(180);
        public static BindableProperty<int> currentNum = new BindableProperty<int>(0);    // current number of active enemies
        public static BindableProperty<int> cumulativeNum = new BindableProperty<int>(0);   // cumulative number of generated enemies so far
        public static BindableProperty<int> CurrentWaves = new BindableProperty<int>(1);
        //[SerializeField] private int maxWavesNum = 3;   // The total number of enemy waves generated
        public static BindableProperty<int> maxWavesNum = new BindableProperty<int>(5);  // The total number of enemy waves generated
        public static BindableProperty<float> BCAttackInterval = new BindableProperty<float>(1.5f); // attack interval of Blazing Circle
        public static BindableProperty<int> MAX_EXP = new BindableProperty<int>(5);
        public static BindableProperty<float> CoinDropRate = new BindableProperty<float>(0.30f); // 30% chance to drop coins（由 LegacyUpgradeState.ApplyEffect 写入）
        public static Room currentRoom;
        public static BindableProperty<bool> FireEnabled = new BindableProperty<bool>(true);
        public static int currentDifficulty;    // 0 - 9 共10个难度等级
        public static List<LevelsConfig> LevelConfigs = new List<LevelsConfig>();
        public static bool IsGamePaused = false;
        public static float WeaponAdditionalCameraSize = 0.5f;

        // ===== Boss 战相关状态（供 UI 血条 / 阶段演出订阅）=====
        // 当前是否有 Boss 在场（Boss 生成时置 true，死亡时置 false）
        public static BindableProperty<bool> BossActive = new BindableProperty<bool>(false);
        // Boss 最大生命值
        public static BindableProperty<float> BossMaxHp = new BindableProperty<float>(0f);
        // Boss 当前生命值
        public static BindableProperty<float> BossCurrentHp = new BindableProperty<float>(0f);
        // Boss 是否进入二阶段（供 UI 变色 / 演出订阅）
        public static BindableProperty<bool> BossPhaseTwo = new BindableProperty<bool>(false);

        // 通关耗时（秒）：仅游戏未暂停时累加，重开新局时在 ResetLevel 清零
        public static float RunElapsedSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            // Initialize AudioKit, ResKit, UIKit
            // Set AudioKit to ignore same sound played in the same frame
            AudioKit.PlaySoundMode = AudioKit.PlaySoundModes.IgnoreSameSoundInGlobalFrames;
            ResKit.Init();
            UIKit.Root.SetResolution(1920, 1080, 1.0f);
            // 相机为跨场景常驻预制体(Assets/Resources/MainCamera.prefab)，场景中不再自带相机
            EnsurePersistentCamera();
            // UIRoot 的 Canvas 为 Screen Space - Camera 模式；主相机是运行时实例化的常驻对象，
            // 预制体中无法序列化对它的引用，因此在两者都就绪后统一绑定 UICamera 字段与渲染相机
            BindUIRootCamera();
            // 初始化强化系统（订阅武器开火事件，用于单武器持续输出叠加被动）
            PlayerUpgradeState.Initialize();
            // 初始化血印系统（订阅武器开火事件等游戏事件）
            BloodSigilState.Initialize();
            // 初始化局外养成系统（加载配置与存档，并按等级写入养成属性）
            LegacyUpgradeState.Initialize();

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
        }

        // 实例化跨场景常驻主相机：首个场景加载前创建一次并 DontDestroyOnLoad，
        // 使 Screen Space - Camera 的 Canvas（GameUI）在场景重载后引用不再失效
        private static void EnsurePersistentCamera()
        {
            if (Camera.main != null)
            {
                return;
            }
            var cameraPrefab = Resources.Load<GameObject>("MainCamera");
            if (cameraPrefab == null)
            {
                Debug.LogError("Resources/MainCamera 预制体缺失，常驻主相机初始化失败");
                return;
            }
            var cameraObject = UnityEngine.Object.Instantiate(cameraPrefab);
            cameraObject.name = "Main Camera";
            UnityEngine.Object.DontDestroyOnLoad(cameraObject);
        }

        // 把 QFramework UIRoot 的 UICamera 字段和 Canvas(Screen Space - Camera) 的
        // Render Camera 都绑定到常驻主相机，保证 UIKit 面板由主相机渲染
        private static void BindUIRootCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("常驻主相机缺失，UIRoot Canvas 渲染相机绑定失败");
                return;
            }
            var uiRoot = UIKit.Root;
            uiRoot.UICamera = mainCamera;
            uiRoot.Canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
            uiRoot.Canvas.worldCamera = mainCamera;
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
                MAX_EXP.Value = 5 + (Level.Value - 1) / 3;
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

        // restart game
        public static void ResetLevel()
        {
            PlayerUpgradeState.Reset();
            BloodSigilState.Reset();
            INGAME_MAX_HP.Value = INIT_MAX_HP.Value; // 最大生命值随强化系统重置（旧 PlayerUpgrade.ResetUpgrade 的职责）
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
            RunElapsedSeconds = 0f;
            // 重置 Boss 战状态，避免上一关的 Boss 血条残留到下一关
            BossActive.Value = false;
            BossMaxHp.Value = 0f;
            BossCurrentHp.Value = 0f;
            BossPhaseTwo.Value = false;
            WeaponDataSystem.weaponDataList.Clear();
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.DE.NewWeapon()); // 默认武器只有DE
            PlayerUpgradeState.ApplyGlobalWeaponUnlocks(); // 局外养成额外解锁武器（按宝箱掉落顺序）
            if (Player.player1 != null)
            {
                Player.player1.UpdateSpecialReloadCost();   // 更新玩家的特殊装弹成本
            }
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
            var rand = Random.Range(0f, 100f);
            if (rand < CoinDropRate.Value * 100)
            {
                GenerateCoin(enemy);
                return;
            }
            // 掉落Shield, 5%概率
            rand = Random.Range(0f, 100f);
            if (rand < 5f) // 测试 ///////////////////////////////////////   
            {
                GenerateShield(enemy);
                return;
            }
            // 只有当血库血量低于30%时才有可能掉落dirtyBlood
            if (BloodBank.Instance != null)
            {
                // float bloodPercent = (float)BloodBank.Instance.CurrentBloodAmount / BloodBank.Instance.MaxBloodAmount;
                // if (bloodPercent < 0.3f)
                // {
                rand = Random.Range(0f, 100f);
                if (rand < 5f)
                {
                    GenerateDirtyBlood(enemy);
                    return;
                }
                // }
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
            // 血印：治疗后血量变化事件（驱动"血量大于/等于阈值"类触发）
            BloodSigilState.NotifyHealthChanged();
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
