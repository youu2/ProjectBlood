using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectBlood
{
    public class FireLightController : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("开火后光线从满强度平滑淡出至 0 的总时长（秒）。高射速场景下建议不小于 0.08 以避免频闪。")]
        [SerializeField] private float fadeOutDelay = 0.09f;

        private Light2D fireLight;
        private float initialIntensity;
        private Coroutine _currentFadeRoutine;

        private void Awake()
        {
            fireLight = GetComponent<Light2D>();
            if (fireLight != null)
            {
                initialIntensity = fireLight.intensity;
                fireLight.enabled = false;
            }
        }

        private void OnEnable()
        {
            WeaponBase.OnWeaponFired += OnWeaponFired;
        }

        private void OnDisable()
        {
            WeaponBase.OnWeaponFired -= OnWeaponFired;
            if (_currentFadeRoutine != null)
            {
                StopCoroutine(_currentFadeRoutine);
                _currentFadeRoutine = null;
            }
        }

        private void OnWeaponFired(WeaponBase weapon)
        {
            if (fireLight == null) return;

            // 高射速竞态：中断上一次尚未完成的淡出，保证每次开火都从满强度重新开始
            if (_currentFadeRoutine != null)
            {
                StopCoroutine(_currentFadeRoutine);
                _currentFadeRoutine = null;
            }

            fireLight.enabled = true;
            fireLight.intensity = initialIntensity;

            _currentFadeRoutine = StartCoroutine(FadeOutLightRoutine(fadeOutDelay));
        }

        private IEnumerator FadeOutLightRoutine(float duration)
        {
            if (duration <= 0f)
            {
                if (fireLight != null)
                {
                    fireLight.intensity = 0f;
                    fireLight.enabled = false;
                }
                _currentFadeRoutine = null;
                yield break;
            }

            float startIntensity = initialIntensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // SmoothStep 缓动：两端切线为 0，过渡更自然，避免尾部突断观感
                float eased = t * t * (3f - 2f * t);

                if (fireLight != null)
                {
                    fireLight.intensity = Mathf.Lerp(startIntensity, 0f, eased);
                }

                yield return null;
            }

            if (fireLight != null)
            {
                fireLight.intensity = 0f;
                fireLight.enabled = false;
            }

            _currentFadeRoutine = null;
        }
    }
}