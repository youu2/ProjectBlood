// LevelTimer.cs
using System;
using System.Collections;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LevelTimer : MonoBehaviour
    {
        [SerializeField] private int startSeconds = 180;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool useUnscaledTime = false; // true => not affected by Time.timeScale
        [SerializeField] private WavesSystem waves;
        public static event Action OnTimerFinished;
        public static event Action<int> OnTimerTick; // show the remaining seconds

        public int Remaining { get; private set; }

        Coroutine _co;
        bool _paused;
        float secondAccumulator = 0f;

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

        public void Pause() { _paused = true; }
        public void Resume() { _paused = false; }
        public void StopTimer()
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
        }

        IEnumerator TimerCo()
        {
            secondAccumulator = 0f;
            while (true)
            {
                // Countdown timer
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
                // yield return null;
                // clear enemy => next wave
                if (Global.currentNum.Value <= 0 && Global.cumulativeNum.Value >= waves.getWave1TotalNum())
                {
                    if (Global.CurrentWaves.Value >= Global.maxWavesNum.Value && Global.currentNum.Value <= 0)
                    {
                        UIKit.OpenPanel<UIGamePassPanel>(); //Survive until the last wave => pass the level
                        OnTimerFinished?.Invoke();
                        yield break;
                    }
                    Global.CurrentWaves.Value += 1;
                    waves.FinishWave();
                    resetWave();
                    yield return null;
                    continue;
                }

                //UIKit.OpenPanel<UIGamePassPanel>(); //Survive until the countdown ends => pass the level
                // countdown ends => next wave
                if (Remaining <= 0 && Global.CurrentWaves.Value < Global.maxWavesNum.Value)
                {
                    Global.CurrentWaves.Value += 1;
                    waves.FinishWave();
                    resetWave();
                    yield return null;
                    continue;
                }
                // clear the enemies of the last wave => pass the level 
                if (Global.CurrentWaves.Value >= Global.maxWavesNum.Value && Global.currentNum.Value <= 0
                && Global.cumulativeNum.Value >= waves.getWave1TotalNum())
                {
                    UIKit.OpenPanel<UIGamePassPanel>(); //Survive until the last wave => pass the level
                    OnTimerFinished?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        void UpdateGlobal(int value)
        {
            Global.RemainingTime.Value = value;
        }

        void resetWave()
        {
            // if(Global.CurrentWaves.Value < waves.maxWavesNum) 
            Remaining = startSeconds; // reset time limit
            secondAccumulator = 0f;             // Clear the accumulated dt
            UpdateGlobal(Remaining);            // Push to UI
        }
    }
}
