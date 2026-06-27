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
            GUIInstance = this;
        }
        public static void UpdateClipText(GunClip gunClip)
        {
            if (GUIInstance != null && GUIInstance.ClipText != null)
            {
                GUIInstance.ClipText.text = $"Ammo: {gunClip.currentAmmo} / {gunClip.maxAmmo}\n([R] to reload)";
            }
        }
        public static void UpdateBloodText(BloodBank bloodBank)
        {
            if (GUIInstance != null && GUIInstance.BloodText != null)
            {
                GUIInstance.BloodText.text = $"Blood: {bloodBank.CurrentBloodAmount} / {bloodBank.MaxBloodAmount}";
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

        public static void ShowLoadingPage(string sceneName, System.Action onLoadingComplete = null, float minDuration = 2f)
        {
            if (GUIInstance != null)
            {
                GUIInstance.StartCoroutine(GUIInstance.LoadingPageCoroutine(sceneName, onLoadingComplete, minDuration));
            }
        }

        private IEnumerator LoadingPageCoroutine(string sceneName, System.Action onLoadingComplete, float minDuration)
        {
            // 隐藏游戏UI面板
            UIKit.HidePanel<UIGamePanel>();

            LoadingPage.gameObject.SetActive(true);
            Global.IsGamePaused = true;
            loadingDotIndex = 0;

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            float elapsed = 0f;
            // 等待加载完成(progress>=0.9)且时间够
            while (asyncOp.progress < 0.9f || elapsed < minDuration)
            {
                LoadingText.text = loadingDots[loadingDotIndex];
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

            LoadingPage.gameObject.SetActive(false);
            Global.IsGamePaused = false;
            onLoadingComplete?.Invoke();
        }
    }
}