using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectBlood
{
    public class LaserLightController : MonoBehaviour
    {
        private Light2D laserLight;

        private void Awake()
        {
            laserLight = GetComponent<Light2D>();
            if (laserLight != null)
                laserLight.enabled = false; // 初始关闭
        }

        private void OnEnable()
        {
            Laser.OnLaserActivate += OnLaserActivate;
            Laser.OnLaserDeactivate += OnLaserDeactivate;
        }

        private void OnDisable()
        {
            Laser.OnLaserActivate -= OnLaserActivate;
            Laser.OnLaserDeactivate -= OnLaserDeactivate;

            // 确保关灯
            if (laserLight != null)
                laserLight.enabled = false;
        }

        private void OnLaserActivate()
        {
            if (laserLight == null) return;
            laserLight.enabled = true;
        }

        private void OnLaserDeactivate()
        {
            if (laserLight == null) return;
            laserLight.enabled = false;
        }

    }
}