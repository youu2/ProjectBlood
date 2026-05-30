using UnityEngine;
using QFramework;

namespace ProjectBlood{
    // 简单封装QF AudioKit
    public class AudioKitManager : IAudioManager
    {
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
        
        public void Stop(AudioPlayer player)
        {
            player?.Stop();
            player = null;
        }

    }
}