using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    /// <summary>
    /// 音频设置页(AudioSettingPage)：
    /// 1. 默认隐藏，暂停页点击 BtnAudioSetting 后打开（覆盖在暂停页之上）；
    /// 2. 三个滑动条实时控制 AudioKitManager 的总音量/音乐音量/音效音量；
    /// 3. 打开状态下按 ESC 关闭：按键由 PausePageController 统一路由后调用 Close，
    ///    避免与暂停页自身的 ESC 暂停/恢复逻辑在同一帧互相冲突。
    /// 页面物体在预制体中默认隐藏，可见性通过 GameObject 的激活状态控制。
    /// </summary>
    public class AudioSettingPageController : MonoBehaviour
    {
        [Header("音量滑动条（0~1）")]
        [SerializeField] private Slider masterVolumeSlider; // 总音量
        [SerializeField] private Slider musicVolumeSlider;  // 音乐音量
        [SerializeField] private Slider soundVolumeSlider;  // 音效音量

        // 页面物体未激活即为关闭状态
        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            // 滑动条事件统一用代码绑定（与 PausePageController 的按钮绑定方式一致）
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        // 打开：每次先以 AudioKitManager 当前音量同步滑块，避免显示上次的旧值，再激活页面
        public void Open()
        {
            SyncSliders();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // 不触发 onValueChanged 的同步，避免初始化滑块位置时反过来改写当前音量
        private void SyncSliders()
        {
            masterVolumeSlider.SetValueWithoutNotify(AudioKitManager.GlobalVolumeRatio.Value);
            musicVolumeSlider.SetValueWithoutNotify(AudioKitManager.MusicVolumeRatio.Value);
            soundVolumeSlider.SetValueWithoutNotify(AudioKitManager.SoundVolumeRatio.Value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioKitManager.Instance.SetGlobalVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioKitManager.Instance.SetMusicVolume(value);
        }

        private void OnSoundVolumeChanged(float value)
        {
            AudioKitManager.Instance.SetSoundVolume(value);
        }

        public void OnDestroy()
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            soundVolumeSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);
        }
    }
}
