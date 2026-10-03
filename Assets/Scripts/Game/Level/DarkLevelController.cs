using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectBlood
{
    // 关卡类型标识：后续黑暗关专属玩法（敌人强化/专属掉落等）可直接基于此枚举扩展
    public enum LevelType
    {
        Normal,
        Dark,
    }

    // 黑暗关卡控制器：负责关卡类型概率判定与环境光/玩家自发光的切换。
    // 独立静态类，不侵入 LevelsConfig / MapController，避免关卡生成逻辑膨胀。
    // 光源随场景重建，不跨场景缓存引用，每次关卡加载后重新查找。
    // 枪口火光/激光等战斗光源由各自控制器（FireLightController 等）管理，此处不做干预。
    public static class DarkLevelController
    {
        // ===== 概率规则配置（difficultyIndex：0=1-1，3=2-1，7=3-2，8=3-3）=====
        // 2-1 至 3-2：每关 30% 概率黑暗
        public const int DarkRollStartIndex = 3;
        public const int DarkRollEndIndex = 7;
        // 3-3：强制黑暗（100%）
        public const int ForcedDarkIndex = 8;
        // 黑暗关触发概率（百分比）。Random.Range(0,100) 均匀分布取整，<30 命中即严格 30%
        public const float DarkRollChancePercent = 30f;

        // 光源过渡时长：黑暗关开场环境光淡出、玩家自发光淡入，SmoothStep 缓动无闪烁
        private const float LightFadeDuration = 0.6f;

        public static LevelType CurrentLevelType { get; private set; } = LevelType.Normal;
        public static bool IsDarkLevel => CurrentLevelType == LevelType.Dark;

        // 场景初始强度基准：进关首次应用时灯光处于序列化初值，记录后作为恢复/淡入目标
        private static float _globalLightOriginalIntensity = 0.5f;
        private static float _playerLightTargetIntensity = 0.5f;
        private static Coroutine _fadeRoutine;

        // 协程宿主：静态类无法启动协程，懒创建隐藏对象承载灯光过渡动画
        private class LightFadeHost : MonoBehaviour { }
        private static LightFadeHost _host;

        // ============================== 关卡类型判定 ==============================

        // 新关卡生成时调用：按概率规则决定本关类型并应用光照（每关只调用一次，进关时）
        public static void SetupForLevel(int difficultyIndex)
        {
            CurrentLevelType = DecideLevelType(difficultyIndex);
            ApplyLighting();
        }

        // 读档还原时调用：不重新掷骰，直接恢复存档时的黑暗状态，保证进度一致
        public static void RestoreFromSave(bool isDarkLevel)
        {
            CurrentLevelType = isDarkLevel ? LevelType.Dark : LevelType.Normal;
            ApplyLighting();
        }

        // 黑暗规则分类（纯函数）
        public static DarkRule ClassifyLevel(int difficultyIndex)
        {
            if (difficultyIndex == ForcedDarkIndex) return DarkRule.ForcedDark;
            if (difficultyIndex >= DarkRollStartIndex && difficultyIndex <= DarkRollEndIndex) return DarkRule.Roll;
            return DarkRule.Never;
        }

        // 概率判定（纯函数）：roll 为 Random.Range(0,100) 的均匀结果，<30 即黑暗
        public static bool IsDarkRoll(float rollPercent)
        {
            return rollPercent < DarkRollChancePercent;
        }

        public enum DarkRule
        {
            Never,      // 1-1 ~ 1-3：永不黑暗
            Roll,       // 2-1 ~ 3-2：30% 概率
            ForcedDark, // 3-3：强制黑暗
        }

        private static LevelType DecideLevelType(int difficultyIndex)
        {
            switch (ClassifyLevel(difficultyIndex))
            {
                case DarkRule.ForcedDark:
                    return LevelType.Dark;
                case DarkRule.Roll:
                    return IsDarkRoll(Random.Range(0, 100)) ? LevelType.Dark : LevelType.Normal;
                default:
                    return LevelType.Normal;
            }
        }

        // ============================== 光源控制 ==============================

        // 应用当前关卡类型对应的光照状态：
        // 黑暗关：环境光淡出至 0 后禁用（完全关闭），玩家自发光从 0 淡入（有限可见范围）
        // 非黑暗关：环境光立即恢复启用，玩家自发光保持隐藏
        public static void ApplyLighting()
        {
            var globalLight = FindGlobalLight();
            var playerLight = FindPlayerSelfLight();

            if (globalLight == null || playerLight == null)
            {
                Debug.LogWarning($"[DarkLevelController] 光源查找失败 global={globalLight != null} player={playerLight != null}，跳过光照切换");
                return;
            }

            // 首次应用时（灯光为场景序列化初值）记录基准，重复调用不会覆盖已被修改的值
            if (globalLight.enabled && globalLight.intensity > 0f)
            {
                _globalLightOriginalIntensity = globalLight.intensity;
            }
            if (playerLight.intensity > 0f)
            {
                _playerLightTargetIntensity = playerLight.intensity;
            }

            // 中断上一次未完成的过渡，避免同场景重复应用时叠加
            if (_fadeRoutine != null && _host != null)
            {
                _host.StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (IsDarkLevel)
            {
                playerLight.gameObject.SetActive(true);
                playerLight.intensity = 0f;
                if (_host == null)
                {
                    _host = new GameObject("DarkLevelLightFadeHost").AddComponent<LightFadeHost>();
                }
                _fadeRoutine = _host.StartCoroutine(FadeLightsRoutine(
                    globalLight, 0f,
                    playerLight, _playerLightTargetIntensity));
            }
            else
            {
                // 非黑暗关：场景初值即目标状态，直接确保启用即可，无需动画
                playerLight.gameObject.SetActive(false);
                globalLight.enabled = true;
                globalLight.intensity = _globalLightOriginalIntensity;
            }
        }

        // 环境光与玩家自发光同步过渡：SmoothStep 缓动与 FireLightController 保持一致观感
        private static IEnumerator FadeLightsRoutine(Light2D globalLight, float globalTarget,
            Light2D playerLight, float playerTarget)
        {
            float globalStart = globalLight.intensity;
            float playerStart = playerLight.intensity;
            float elapsed = 0f;

            while (elapsed < LightFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / LightFadeDuration);
                // SmoothStep：两端切线为 0，过渡自然无突断
                float eased = t * t * (3f - 2f * t);

                globalLight.intensity = Mathf.Lerp(globalStart, globalTarget, eased);
                playerLight.intensity = Mathf.Lerp(playerStart, playerTarget, eased);

                yield return null;
            }

            // 到达终态后彻底关闭：环境光组件禁用（完全禁用），玩家自发光物体隐藏（不可见）
            globalLight.intensity = globalTarget;
            playerLight.intensity = playerTarget;
            globalLight.enabled = globalTarget > 0f;
            playerLight.gameObject.SetActive(playerTarget > 0f);
            _fadeRoutine = null;
        }

        // 按灯光类型识别场景内全局环境光（不依赖物体命名，抗改名）
        private static Light2D FindGlobalLight()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    return light;
                }
            }
            return null;
        }

        // 玩家自发光：取 Player 上已绑定的 PlayerSelfLight2D（场景内 "Player Light 2D"）
        private static Light2D FindPlayerSelfLight()
        {
            var player = Player.player1;
            if (player == null || player.PlayerSelfLight2D == null)
            {
                return null;
            }
            return player.PlayerSelfLight2D.GetComponent<Light2D>();
        }
    }
}
