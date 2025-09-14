// LevelTimer.cs
using System;
using System.Collections;
using UnityEngine;

namespace ProjectBlood
{
    public class LevelTimer : MonoBehaviour
    {
        public int startSeconds = 180;
        public bool autoStart = true;
        public bool useUnscaledTime = false; // true => not affected by Time.timeScale
        public static event Action OnTimerFinished;
        public static event Action<int> OnTimerTick; // show the remaining seconds

        public int Remaining { get; private set; }

        Coroutine _co;
        bool _paused;

        void OnEnable()
        {
            if (autoStart) StartTimer(startSeconds);
        }

        public void StartTimer(int seconds)
        {
            StopTimer();
            Remaining = Mathf.Max(0, seconds);
            UpdateGlobal(Remaining);
            _paused = false;
            _co = StartCoroutine(TimerCo());
        }

        public void Pause()  { _paused = true; }
        public void Resume() { _paused = false; }
        public void StopTimer()
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
        }

        IEnumerator TimerCo()
        {
            float secondAccumulator = 0f;
            while (Remaining > 0)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (!_paused)
                {
                    secondAccumulator += dt;
                    while (secondAccumulator >= 1f && Remaining > 0)
                    {
                        secondAccumulator -= 1f;
                        Remaining--;
                        UpdateGlobal(Remaining);
                        OnTimerTick?.Invoke(Remaining);
                    }
                }
                yield return null;
            }

            OnTimerFinished?.Invoke();
        }

        void UpdateGlobal(int value)
        {
            Global.RemainingTime.Value = value;
        }
    }
}
