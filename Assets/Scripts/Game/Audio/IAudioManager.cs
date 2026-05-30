using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    public interface IAudioManager
    {
        AudioPlayer PlayOneShot(AudioClip clip, float volume = 1f);
        AudioPlayer PlayOneShot(string clipName, float volume = 1f);
        AudioPlayer PlayLoop(AudioClip clip, float volume = 1f);
        AudioPlayer PlayLoop(string clipName, float volume = 1f);
        void Stop(AudioPlayer player);
    }
}