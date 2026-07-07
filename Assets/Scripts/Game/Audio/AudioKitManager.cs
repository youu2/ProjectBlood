using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    // 简单封装QF AudioKit
    public class AudioKitManager : IAudioManager
    {
        private static readonly AudioKitManager instance = new AudioKitManager();

        public static AudioKitManager Instance => instance;
        public static BindableProperty<float> SoundVolumeRatio = new BindableProperty<float>(1f);
        public static BindableProperty<float> MusicVolumeRatio = new BindableProperty<float>(1f);
        public static BindableProperty<float> GlobalVolumeRatio = new BindableProperty<float>(1f);

        private AudioKitManager() { }  // 私有构造函数，防止外部 new

        public void Init()
        {
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
            return clip != null ? AudioKit.PlaySound(clip, loop: true, volume: volume) : null;
        }

        public AudioPlayer PlayLoop(string clipName, float volume = 1f)
        {
            return !string.IsNullOrEmpty(clipName) ? AudioKit.PlaySound(clipName, loop: true, volume: volume) : null;
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
        }

        // 停止指定的音频播放器
        public void Stop(AudioPlayer player)
        {
            player?.Stop();
        }
        public void StopMusic()
        {
            AudioKit.StopMusic();
        }

    }
}