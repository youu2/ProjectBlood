using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectBlood
{
    public partial class GameUI : ViewController
    {
        public static GameUI GUIInstance;
        public static event System.Action OnLoadingComplete;
        // 加载页显隐事件：true=加载页打开（隐藏指针），false=加载页关闭（恢复指针）
        public static event System.Action<bool> OnLoadingPageStateChanged;

        private void Awake()
        {
            // 每次进入场景都会新建一个GameUI实例，自动销毁避免重复实例
            if (GUIInstance != null && GUIInstance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                GUIInstance = this;
            }
            DontDestroyOnLoad(gameObject);
            // 相机为场景内对象，GameUI 为 DDOL。Awake 首次绑定后，
            // 后续场景重载由 Global.RebindCameraOnSceneLoad 统一重绑 Canvas 渲染相机
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = Camera.main;
            }
        }

        private void Start()
        {
            // bind to Global properties
            // update UI when properties change
            Global.currentHP.RegisterWithInitValue(currentHP =>
            {
                HPText.text = "HP: " + Mathf.FloorToInt(currentHP) + "/" + Mathf.FloorToInt(Global.INGAME_MAX_HP.Value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.INGAME_MAX_HP.RegisterWithInitValue(maxHP =>
            {
                HPText.text = "HP: " + Mathf.FloorToInt(Global.currentHP.Value) + "/" + Mathf.FloorToInt(maxHP);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Exp.RegisterWithInitValue(Exp =>
            {
                ExpText.text = "Exp: " + Exp + "/" + Global.MAX_EXP;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Level.RegisterWithInitValue(Level =>
            {
                PlayerLevelText.text = "Level: " + Level;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            Global.Coin.RegisterWithInitValue(Coin =>
            {
                CoinText.text = Coin.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        public static void UpdateClipText(GunClip gunClip)
        {
            if (GUIInstance != null && GUIInstance.ClipText != null)
            {
                GUIInstance.ClipText.text = $"Ammo: {gunClip.currentAmmo} / {gunClip.maxAmmo}";
            }
        }
        public static void UpdateBloodText()
        {
            if (GUIInstance != null && GUIInstance.BloodText != null)
            {
                GUIInstance.BloodText.text = $"Blood: {BloodBank.Instance.CurrentBloodAmount} / {BloodBank.Instance.MaxBloodAmount}";
            }
        }

        public static void ShowLevelText(string levelName, float duration = 2f, bool isDarkLevel = false)
        {
            if (GUIInstance != null && GUIInstance.LevelText != null)
            {
                GUIInstance.StartCoroutine(GUIInstance.ShowLevelTextCoroutine(levelName, duration, isDarkLevel));
            }
        }

        private IEnumerator ShowLevelTextCoroutine(string levelName, float duration, bool isDarkLevel = false)
        {
            var displayName = levelName.Replace("Level ", "");
            LevelText.text = displayName;
            // 黑暗关关卡名用红色显示，非黑暗关保持原色
            var baseColor = isDarkLevel ? Color.red : Color.white;
            var color = baseColor;
            color.a = 0f;
            LevelText.color = color;

            float fadeInTime = 0.5f;
            for (float t = 0; t < fadeInTime; t += Time.deltaTime)
            {
                color.a = t / fadeInTime;
                LevelText.color = color;
                yield return null;
            }
            color.a = 1f;
            color = baseColor;
            color.a = 1f;
            LevelText.color = color;

            yield return new WaitForSeconds(duration - fadeInTime * 2);

            float fadeOutTime = 0.5f;
            for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
            {
                color.a = 1f - t / fadeOutTime;
                LevelText.color = color;
                yield return null;
            }
            color.a = 0f;
            LevelText.color = color;
        }

        private string[] loadingDots = new string[] { "Loading", "Loading.", "Loading..", "Loading..." };
        private int loadingDotIndex = 0;

        // 显示加载页面的同时加载场景，等待加载完成或最小加载时间
        public static void ShowLoadingPage(string sceneName, System.Action onLoadingComplete = null, float minDuration = 1.5f)
        {
            if (GUIInstance != null)
            {
                GUIInstance.StartCoroutine(GUIInstance.LoadingPageCoroutine(sceneName, onLoadingComplete, minDuration));
            }
        }

        private IEnumerator LoadingPageCoroutine(string sceneName, System.Action onLoadingComplete, float minDuration)
        {
            // 隐藏游戏UI面板
            // UIKit.HidePanel<UIGamePanel>();
            HideGameUI();

            if (LoadingPage != null)
            {
                LoadingPage.gameObject.SetActive(true);
            }
            OnLoadingPageStateChanged?.Invoke(true); // 加载页打开，隐藏指针

            Global.IsGamePaused = true;
            // 冻结时间流逝：旧场景在异步加载的 1.5 秒内仍然存活，仅靠各输入点检查
            // IsGamePaused 无法冻结敌人 AI / 物理 / 子弹 / WaitForSeconds 协程
            // （曾出现加载页期间玩家仍能翻滚瞄准、敌人继续移动攻击的问题）。
            // timeScale=0 一次性停住 FixedUpdate、物理、WaitForSeconds 与所有 deltaTime 驱动逻辑；
            // 本协程用 unscaledTime/unscaledDeltaTime 计时，场景异步加载也不受 timeScale 影响。
            // 节奏由目标场景自行恢复：InGame 由 MapController.Start 设回 1，GameStart 由主菜单面板设 0.8
            Time.timeScale = 0f;
            // 加载页一出现就回正并冻结相机：旧场景在异步加载的最小等待期间仍然存活，
            // 不冻结的话相机会继续按玩家房间位置施加 Z 旋转，倾斜会残留到主菜单 UI
            CameraUtils.MainCameraController()?.SetLoadingFreeze(true);
            loadingDotIndex = 0;

            // 开始加载场景, 但不激活场景
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            float elapsed = 0f;
            // 等待加载完成(progress>=0.9)且超过最小加载时间
            while (asyncOp.progress < 0.9f || elapsed < minDuration)
            {
                // 循环显示加载点文本，每0.4秒切换一次
                if (LoadingText != null)
                {
                    LoadingText.text = loadingDots[loadingDotIndex];
                }
                loadingDotIndex = (loadingDotIndex + 1) % loadingDots.Length;
                float dotCycleTime = 0.4f;
                float startUnscaledTime = Time.unscaledTime;
                float targetUnscaledTime = startUnscaledTime + dotCycleTime;
                while (Time.unscaledTime < targetUnscaledTime && (asyncOp.progress < 0.9f || elapsed < minDuration))
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // 激活场景
            asyncOp.allowSceneActivation = true;

            // 等待场景完全激活
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            // 场景已激活（sceneLoaded 已复位相机并置 pendingSnapToPlayer），解除冻结：
            // 进入游戏场景时首帧吸附到新 Player，进入主菜单时无 Player 则持续保持回正
            CameraUtils.MainCameraController()?.SetLoadingFreeze(false);

            if (LoadingPage != null)
            {
                LoadingPage.gameObject.SetActive(false);
            }
            OnLoadingPageStateChanged?.Invoke(false); // 加载页关闭，恢复指针
            // 注意：此处不恢复 Time.timeScale —— 场景激活时目标场景的 Start 已先于本收尾执行：
            // InGame 由 MapController.Start 设回 1，GameStart 由 UIGameStartPanel.OnInit 设为 0.8
            Global.IsGamePaused = false;
            onLoadingComplete?.Invoke();
            OnLoadingComplete?.Invoke(); // 场景真正激活完成后触发外部事件（对象池预热等）
        }

        // 由于我同时使用（练习）了UnityEngine的GameUI和QF的UIGamePanel，
        // 所以这里需要同时操作GameUI和UIGamePanel的显示隐藏状态
        public static void HideGameUI()
        {
            UIKit.HidePanel<UIGamePanel>();
            GUIInstance.ClipText.Hide();
            GUIInstance.BloodText.Hide();
            GUIInstance.UIMap.Hide();
            GUIInstance.SkillIcon.Hide();
            GUIInstance.CoinIcon.Hide();
            GUIInstance.FaceFrame.Hide();
            GUIInstance.LevelText.Hide();
            GUIInstance.CoinText.Hide();
            GUIInstance.ExpText.Hide();
            GUIInstance.HPText.Hide();
            GUIInstance.SkillText.Hide();
            GUIInstance.PlayerLevelText.Hide();
            GUIInstance.HealthBar.Hide();
            if (Player.player1 != null) Player.player1.ShieldSprite.Hide();
        }

        public static void ShowGameUI()
        {
            UIKit.ShowPanel<UIGamePanel>();
            GUIInstance.ClipText.Show();
            GUIInstance.BloodText.Show();
            GUIInstance.UIMap.Show();
            GUIInstance.SkillIcon.Show();
            GUIInstance.CoinIcon.Show();
            GUIInstance.FaceFrame.Show();
            GUIInstance.LevelText.Show();
            GUIInstance.CoinText.Show();
            GUIInstance.ExpText.Show();
            GUIInstance.HPText.Show();
            GUIInstance.SkillText.Show();
            GUIInstance.PlayerLevelText.Show();
            GUIInstance.HealthBar.Show();
            if (Player.player1 != null) Player.player1.ShieldSprite.Show();
        }

        // 进入 Boss 房时调用：播放 MeetBoss 动画并暂停时间 2.5 秒
        // 动画期间 timeScale = 0，动画本身不受 timeScale 影响（Animator 默认使用 UnscaledTime）
        public static void PlayBossShow()
        {
            if (GUIInstance == null || GUIInstance.BossShowAnimator == null) return;
            GUIInstance.StartCoroutine(GUIInstance.BossShowCoroutine());
        }

        private IEnumerator BossShowCoroutine()
        {
            // 动画使用 UnscaledTime 更新，否则 timeScale=0 会把出场动画一起冻结
            BossShowAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            BossShowAnimator.SetTrigger("MeetBoss");
            Global.IsGamePaused = true;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(2.5f);
            Time.timeScale = 1f;
            Global.IsGamePaused = false;
        }
    }
}