using UnityEngine;
using QFramework;

namespace ProjectBlood{
    public class AudioKitManager : IAudioManager
    {
        private AudioPlayer _currentLoopPlayer;
        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if(clip != null)
            {
                AudioKit.PlaySound(clip, volume: volume);
            }
        }
        public void PlayOneShot(string clipName, float volume = 1f){
            if(!string.IsNullOrEmpty(clipName))
            {
                AudioKit.PlaySound(clipName, volume: volume);
            }
        }
        public void PlayLoop(string clipName, float volume = 1f)
        {
            StopLoop();
            if(!string.IsNullOrEmpty(clipName))
            {
                _currentLoopPlayer = AudioKit.PlaySound(clipName, loop: true, volume: volume);
            }
        }
        public void PlayLoop(AudioClip clip, float volume = 1f)
        {
            StopLoop();
            if(clip != null)
            {
                _currentLoopPlayer = AudioKit.PlaySound(clip, loop: true, volume: volume);
            }
        }
        public void StopLoop()
        {
            if (_currentLoopPlayer != null)
                {
                    _currentLoopPlayer.Stop();
                    _currentLoopPlayer = null;
                }
        }
    }
}