using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 简单封装QF AudioKit
    public class AudioKitManager : IAudioManager
    {
        private static readonly AudioKitManager instance = new AudioKitManager();

        public static AudioKitManager Instance => instance;

        public static BindableProperty<float> GlobalVolumeRatio = new BindableProperty<float>(1f);
        public static BindableProperty<float> MusicVolumeRatio = new BindableProperty<float>(1f);

        public static BindableProperty<float> SoundVolumeRatio = new BindableProperty<float>(1f);

        private AudioKitManager() { }  // 私有构造函数，防止外部 new

        private bool initialized;

        // ===== 循环音效暂停/恢复 =====
        // timeScale=0 不影响 AudioSource（音频按实时时钟播放），暂停时循环音会继续响。
        // 所有循环音统一经 PlayLoop 登记：进入暂停时快照并 Pause，恢复时仅续播仍有效的快照项。
        private readonly HashSet<AudioPlayer> activeLoops = new HashSet<AudioPlayer>();
        private readonly List<AudioPlayer> pausedLoops = new List<AudioPlayer>();

        private void HandleGamePausedChanged(bool paused)
        {
            if (paused)
            {
                // 只暂停"暂停前就在响"的循环音，快照供恢复时使用
                pausedLoops.Clear();
                foreach (var player in activeLoops)
                {
                    if (player == null || player.IsRecycled) continue;
                    player.Pause();
                    pausedLoops.Add(player);
                }
            }
            else
            {
                // 暂停期间被停止（松键停火/实体销毁/场景加载）的循环音已移出 activeLoops，不再恢复
                foreach (var player in pausedLoops)
                {
                    if (player != null && !player.IsRecycled && activeLoops.Contains(player))
                    {
                        player.Resume();
                    }
                }
                pausedLoops.Clear();
            }
        }

        // 登记一个新播放的循环音；暂停期间播放（正常不会发生，开火输入已被拦截）时立即暂停并入快照
        private void RegisterLoop(AudioPlayer player)
        {
            if (player == null) return;
            activeLoops.Add(player);
            if (Global.IsGamePaused)
            {
                player.Pause();
                if (!pausedLoops.Contains(player)) pausedLoops.Add(player);
            }
        }

        public void Init()
        {
            // 幂等保护：音量监听注册在常驻的静态 BindableProperty 上，重复 Init 会导致重复注册
            if (initialized) return;

            // AudioKit.Settings 的音量属性由 AudioKit 自身 BeforeSceneLoad AutoInit 构造。
            // 若被过早调用（早于该 AutoInit），此处为 null：打印明确日志并放弃本次初始化，
            // 不置 initialized，保证稍后（AfterSceneLoad）可重试，且绝不抛异常中断调用方启动流程。
            if (AudioKit.Settings.SoundVolume == null || AudioKit.Settings.MusicVolume == null)
            {
                Debug.LogError("[AudioKitManager] Init 过早：AudioKit.Settings 尚未完成初始化，本次跳过，等待稍后重试。");
                return;
            }
            initialized = true;

            // 订阅游戏暂停边沿事件：Esc 暂停页/关卡加载/过场/死亡与结算面板/主菜单养成等
            // 全部经 Global.IsGamePaused 收口，一处订阅覆盖所有时间停止场景
            Global.OnGamePausedChanged += HandleGamePausedChanged;

            // 从 PlayerPrefs 加载音量比例
            if (PlayerPrefs.HasKey("GlobalVolumeRatio"))
            {
                GlobalVolumeRatio.Value = PlayerPrefs.GetFloat("GlobalVolumeRatio", 1f);
            }
            if (PlayerPrefs.HasKey("MusicVolumeRatio"))
            {
                MusicVolumeRatio.Value = PlayerPrefs.GetFloat("MusicVolumeRatio", 1f);
            }
            if (PlayerPrefs.HasKey("SoundVolumeRatio"))
            {
                SoundVolumeRatio.Value = PlayerPrefs.GetFloat("SoundVolumeRatio", 1f);
            }

            SetSoundVolume(SoundVolumeRatio.Value);
            SetMusicVolume(MusicVolumeRatio.Value);

            SoundVolumeRatio.Register(volume =>
            {
                PlayerPrefs.SetFloat("SoundVolumeRatio", volume);
            });
            MusicVolumeRatio.Register(volume =>
            {
                PlayerPrefs.SetFloat("MusicVolumeRatio", volume);
            });
            GlobalVolumeRatio.Register(volume =>
            {
                PlayerPrefs.SetFloat("GlobalVolumeRatio", volume);
            });
        }
        public AudioPlayer PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                return AudioKit.PlaySound(clip, volume: volume);
            }
            return null;
        }

        public AudioPlayer PlayOneShot(string clipName, float volume = 1f)
        {
            if (!string.IsNullOrEmpty(clipName))
            {
                return AudioKit.PlaySound(clipName, volume: volume);
            }
            return null;
        }

        public AudioPlayer PlayLoop(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return null;
            var player = AudioKit.PlaySound(clip, loop: true, volume: volume);
            RegisterLoop(player);
            return player;
        }

        public AudioPlayer PlayLoop(string clipName, float volume = 1f)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            var player = AudioKit.PlaySound(clipName, loop: true, volume: volume);
            RegisterLoop(player);
            return player;
        }

        public void PlayMusic(AudioClip music, float volume = 1f)
        {
            AudioKit.PlayMusic(music, volume: volume);
        }

        public void PlayMusic(string musicName, float volume = 1f)
        {
            AudioKit.PlayMusic(musicName, volume: volume);
        }

        // 设置音效音量(在现有音量基础上设置一个独立系数)
        public void SetSoundVolume(float volume)
        {
            AudioKit.Settings.SoundVolume.Value = volume * GlobalVolumeRatio.Value;
            SoundVolumeRatio.Value = volume;
        }

        // 设置背景音乐音量(在现有音量基础上设置一个独立系数)
        public void SetMusicVolume(float volume)
        {
            AudioKit.Settings.MusicVolume.Value = volume * GlobalVolumeRatio.Value;
            MusicVolumeRatio.Value = volume;
        }

        public void SetGlobalVolume(float volume)
        {
            GlobalVolumeRatio.Value = volume;

            // 总音量是音效/音乐通道的公共系数，变化后重算 AudioKit 各通道最终音量，
            // 使拖动总音量滑条时正在播放的音效也实时变化（FMOD 音乐由 FmodMusicManager 自行监听 GlobalVolumeRatio）
            AudioKit.Settings.SoundVolume.Value = SoundVolumeRatio.Value * volume;
            AudioKit.Settings.MusicVolume.Value = MusicVolumeRatio.Value * volume;
        }

        // 停止指定的音频播放器
        public void Stop(AudioPlayer player)
        {
            if (player == null) return;
            // 同步移出循环音登记与暂停快照，保证恢复时不会复活已停的循环音
            activeLoops.Remove(player);
            pausedLoops.Remove(player);
            player.Stop();
        }
        public void StopMusic()
        {
            AudioKit.StopMusic();
        }

    }
}