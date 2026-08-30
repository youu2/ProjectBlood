using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectBlood
{
    public class FireLightController : MonoBehaviour
    {
        private Light2D fireLight;

        private void Awake()
        {
            fireLight = GetComponent<Light2D>();
            if (fireLight != null)
                fireLight.enabled = false;
        }

        private void OnEnable()
        {
            WeaponBase.OnWeaponFired += OnWeaponFired;
        }

        private void OnDisable()
        {
            WeaponBase.OnWeaponFired -= OnWeaponFired;
        }

        private void OnWeaponFired(WeaponBase weapon)
        {
            if (fireLight == null) return;
            fireLight.enabled = true;
            StartCoroutine(DisableLightAfterDelay(0.09f));
        }

        private IEnumerator DisableLightAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (fireLight != null)
                fireLight.enabled = false;
        }
    }
}