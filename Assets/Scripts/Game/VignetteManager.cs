using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectBlood
{
    // 屏幕暗角管理器：
    // 1) 常驻红屏强度 BaseIntensity 随玩家剩余血量百分比变化（血量越低越红）
    // 2) BaseIntensity 带正弦浮动，模拟"濒死呼吸"感
    // 3) 受伤时在 BaseIntensity 之上短暂叠加 BonusIntensity（淡入淡出）
    // 4) 治疗时屏幕短暂闪绿（颜色过渡 + 独立强度，淡入淡出）
    //
    // 依赖 URP Volume + Vignette。若未在 Inspector 指定 Volume，会在运行时自动创建一个 Global Volume。
    // 所有效果参数均可在 Inspector 调节。
    [DefaultExecutionOrder(100)]
    public class VignetteManager : MonoBehaviour
    {
        public static VignetteManager Instance { get; private set; }

        [Header("Volume 引用")]
        [Tooltip("包含 Vignette 的 URP Volume。为空时运行时自动创建 Global Volume。")]
        [SerializeField] private Volume vignetteVolume;
        private Vignette vignette;

        [Header("颜色")]
        [SerializeField] private Color hurtColor = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("BaseIntensity（血量映射）")]
        [Tooltip("血量 100% 时的基础暗角强度")]
        [Range(0f, 1f)][SerializeField] private float baseIntensityAtFullHp = 0f;
        [Tooltip("血量 0% 时的基础暗角强度")]
        [Range(0f, 1f)][SerializeField] private float baseIntensityAtZeroHp = 0.55f;

        [Header("BaseIntensity 浮动")]
        [Tooltip("浮动振幅（在 BaseIntensity 上 ± 该值）")]
        [Range(0f, 1f)][SerializeField] private float floatAmplitude = 0.05f;
        [Tooltip("每轮浮动所需时间（秒），即一个完整正弦周期")]
        [Min(0.01f)][SerializeField] private float floatPeriod = 2.5f;

        [Header("BonusIntensity（受伤叠加）")]
        [Tooltip("受伤时在 BaseIntensity 之上叠加的额外强度")]
        [Range(0f, 1f)][SerializeField] private float bonusIntensity = 0.3f;
        [Tooltip("BonusIntensity 淡入时间（秒）")]
        [Min(0f)][SerializeField] private float bonusFadeInTime = 0.08f;
        [Tooltip("BonusIntensity 淡出时间（秒）")]
        [Min(0f)][SerializeField] private float bonusFadeOutTime = 0.6f;

        [Header("治疗闪绿")]
        [Tooltip("治疗闪光峰值强度")]
        [Range(0f, 1f)][SerializeField] private float healFlashIntensity = 0.45f;
        [Tooltip("治疗闪光淡入时间（秒）")]
        [Min(0f)][SerializeField] private float healFadeInTime = 0.12f;
        [Tooltip("治疗闪光淡出时间（秒）")]
        [Min(0f)][SerializeField] private float healFadeOutTime = 0.7f;

        [Header("最终强度平滑")]
        [Tooltip("最终强度跟随目标值的平滑速度，越大越跟手；0 表示不做平滑（即时）")]
        [Min(0f)][SerializeField] private float intensitySmoothSpeed = 10f;

        [Header("暗角形状")]
        [Range(0.01f, 1f)][SerializeField] private float vignetteSmoothness = 0.4f;
        [SerializeField] private bool vignetteRounded = true;

        // 运行时状态
        private float bonusTimer;    // 受伤叠加剩余时间
        private float bonusValue;    // 当前 BonusIntensity

        private float healTimer;     // 治疗闪光剩余时间
        private float healValue;     // 当前治疗闪光强度系数 0..1

        private float smoothedIntensity;

        // 主菜单场景判定（与 Player.IsMainMenuScene 一致）：GameStart 场景完全禁用暗角
        private static bool IsMainMenuScene => SceneManager.GetActiveScene().name == "GameStart";

        // 加载页/主菜单是否应强制隐藏暗角
        private static bool ShouldForceHide => GameUI.IsLoadingPageActive || IsMainMenuScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 场景切换时旧场景 Manager 的 OnDestroy 可能晚于新 Manager 的 Awake 执行
                // （Player.player1 已验证的同款时序），不能让新实例自毁，否则切关后暗角缺失；
                // 改为销毁旧实例、由新实例接管（旧实例的 OnDestroy 不会误清新引用）
                Destroy(Instance.gameObject);
            }
            Instance = this;
            EnsureVignette();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureVignette()
        {
            if (vignetteVolume == null)
            {
                var go = new GameObject("VignetteVolume_Auto");
                go.transform.SetParent(transform);
                vignetteVolume = go.AddComponent<Volume>();
                vignetteVolume.isGlobal = true;
                vignetteVolume.priority = 100f;
                vignetteVolume.weight = 1f;
                vignetteVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }
            else if (vignetteVolume.profile == null)
            {
                vignetteVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            if (!vignetteVolume.profile.TryGet(out vignette))
            {
                vignette = vignetteVolume.profile.Add<Vignette>();
            }

            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.rounded.overrideState = true;
            vignette.smoothness.value = vignetteSmoothness;
            vignette.rounded.value = vignetteRounded;
            vignette.color.value = hurtColor;
            vignette.intensity.value = 0f;
        }

        // 玩家受伤时调用：在 BaseIntensity 之上叠加一段 BonusIntensity
        public void NotifyHurt()
        {
            bonusTimer = bonusFadeInTime + bonusFadeOutTime;
        }

        // 玩家治疗时调用：屏幕短暂闪绿
        public void NotifyHeal()
        {
            healTimer = healFadeInTime + healFadeOutTime;
        }

        private void Update()
        {
            if (vignette == null) return;

            // ---- 加载页/主菜单：完全禁用 Vignette 渲染 ----
            // 关闭 Vignette 的 VolumeComponent 开关（URP 直接跳过该效果，无任何渲染开销），
            // 并清零强度兜底、清空受伤/治疗叠加计时，确保恢复战斗时不残留旧红闪/绿闪
            if (ShouldForceHide)
            {
                if (vignette.active)
                {
                    vignette.active = false;
                    vignette.intensity.value = 0f;
                }
                bonusTimer = 0f; bonusValue = 0f;
                healTimer = 0f; healValue = 0f;
                smoothedIntensity = 0f;
                return;
            }

            // 战斗状态：确保 Vignette 处于启用（从加载页/主菜单返回时恢复渲染）
            if (!vignette.active)
            {
                vignette.active = true;
            }

            // ---- BaseIntensity：按血量百分比映射 ----
            float hpRatio = GetHpRatio();
            float baseIntensity = Mathf.Lerp(baseIntensityAtZeroHp, baseIntensityAtFullHp, hpRatio);

            // ---- BaseIntensity 浮动（正弦呼吸） ----
            float phase = (Time.time / Mathf.Max(floatPeriod, 0.01f)) * Mathf.PI * 2f;
            float floatOffset = Mathf.Sin(phase) * floatAmplitude;
            float baseWithFloat = Mathf.Clamp01(baseIntensity + floatOffset);

            // ---- BonusIntensity 淡入淡出 ----
            UpdateFade(ref bonusTimer, ref bonusValue, bonusFadeInTime, bonusFadeOutTime, bonusIntensity);

            // ---- 治疗闪绿 淡入淡出 ----
            UpdateFade(ref healTimer, ref healValue, healFadeInTime, healFadeOutTime, 1f);

            float targetIntensity = Mathf.Clamp01(baseWithFloat + bonusValue);

            // 治疗闪光期间：颜色从红过渡到绿，强度过渡到治疗峰值
            if (healValue > 0.001f)
            {
                vignette.color.value = Color.Lerp(hurtColor, healColor, healValue);
                targetIntensity = Mathf.Lerp(targetIntensity, healFlashIntensity, healValue);
            }
            else
            {
                vignette.color.value = hurtColor;
            }

            targetIntensity = Mathf.Clamp01(targetIntensity);

            // ---- 最终强度平滑 ----
            if (intensitySmoothSpeed > 0f)
            {
                // 指数平滑：帧率无关，速度越大越接近目标
                smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity,
                    1f - Mathf.Exp(-intensitySmoothSpeed * Time.deltaTime));
            }
            else
            {
                smoothedIntensity = targetIntensity;
            }

            vignette.intensity.value = smoothedIntensity;
            vignette.smoothness.value = vignetteSmoothness;
            vignette.rounded.value = vignetteRounded;
        }

        // 统一的淡入淡出计算：elapsed 先在 fadeIn 内从 0→maxValue，再在 fadeOut 内从 maxValue→0
        private void UpdateFade(ref float timer, ref float value, float fadeIn, float fadeOut, float maxValue)
        {
            if (timer <= 0f)
            {
                value = 0f;
                return;
            }

            float total = fadeIn + fadeOut;
            timer -= Time.deltaTime;
            float elapsed = total - timer;

            if (elapsed < fadeIn)
            {
                float t = fadeIn > 0f ? elapsed / fadeIn : 1f;
                value = Mathf.Clamp01(t) * maxValue;
            }
            else
            {
                float outT = fadeOut > 0f ? 1f - (elapsed - fadeIn) / fadeOut : 0f;
                value = Mathf.Clamp01(outT) * maxValue;
            }

            if (timer <= 0f) value = 0f;
        }

        private float GetHpRatio()
        {
            float maxHp = Global.INGAME_MAX_HP.Value;
            if (maxHp <= 0f) return 1f;
            return Mathf.Clamp01(Global.currentHP.Value / maxHp);
        }

        // 运行时获取单例：若场景中未放置 VignetteManager，则懒创建一个（使用默认参数）
        public static VignetteManager GetOrCreateInstance()
        {
            if (Instance != null) return Instance;
            Instance = FindObjectOfType<VignetteManager>();
            if (Instance == null)
            {
                var go = new GameObject("[VignetteManager]");
                Instance = go.AddComponent<VignetteManager>();
            }
            return Instance;
        }
    }
}
