using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    public interface IAudioManager
    {
        void PlayOneShot(string clip, float volume = 1f);
        void PlayOneShot(AudioClip clip, float volume = 1f);
        void PlayLoop(string clip, float volume = 1f);
        void PlayLoop(AudioClip clip, float volume = 1f);
        void StopLoop();
    }
}