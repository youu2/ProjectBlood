using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ProjectBlood
{
    public partial class GameUI : ViewController
    {
        public static GameUI GUIInstance;
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

        public static void ShowLevelText(string levelName, float duration = 2f)
        {
            if (GUIInstance != null && GUIInstance.LevelText != null)
            {
                GUIInstance.StartCoroutine(GUIInstance.ShowLevelTextCoroutine(levelName, duration));
            }
        }

        private IEnumerator ShowLevelTextCoroutine(string levelName, float duration)
        {
            var displayName = levelName.Replace("Level ", "");
            LevelText.text = displayName;
            var color = LevelText.color;
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

            Global.IsGamePaused = true;
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


            if (LoadingPage != null)
            {
                LoadingPage.gameObject.SetActive(false);
            }
            Global.IsGamePaused = false;
            onLoadingComplete?.Invoke();
        }

        // 由于我同时使用（练习）了UnityEngine的GameUI和QF的UIGamePanel，
        // 所以这里需要同时操作GameUI和UIGamePanel的显示隐藏状态
        public static void HideGameUI()
        {
            UIKit.HidePanel<UIGamePanel>();
            GUIInstance.ClipText.Hide();
            GUIInstance.BloodText.Hide();
            GUIInstance.UIMap.Hide();
        }

        public static void ShowGameUI()
        {
            UIKit.ShowPanel<UIGamePanel>();
            GUIInstance.ClipText.Show();
            GUIInstance.BloodText.Show();
            GUIInstance.UIMap.Show();
        }
    }
}