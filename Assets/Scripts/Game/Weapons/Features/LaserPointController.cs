using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectBlood
{
    public class LaserPointController : MonoBehaviour
    {
        protected SpriteRenderer fireFlashRenderer;
        [SerializeField] protected Light2D laserLight1;
        [SerializeField] protected Light2D laserLight2;

        // 帧率无关重构：用时间累加替代帧计数
        // 原 framesPerSprite=10 在 60fps 下周期为 10/60≈0.167s，此处显式表达同一周期
        public float toggleInterval = 10f / 60f;
        protected float toggleTimer = 0f;
        protected bool bigger = false;
        public float biggerScale = 0.65f;
        public float smallerScale = 0.55f;

        // Start is called before the first frame update
        void Awake()
        {
            fireFlashRenderer = GetComponent<SpriteRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateFireFlash();
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
            if (fireFlashRenderer != null) fireFlashRenderer.enabled = false;
            if (laserLight1 != null) laserLight1.enabled = false;
            if (laserLight2 != null) laserLight2.enabled = false;
        }

        protected void UpdateFireFlash()  // 更新枪口激光点
        {
            // 帧率无关：用 Time.deltaTime 累加时间，达到 toggleInterval 后切换状态
            // 这样在 30/60/120 fps 下切换周期均为 toggleInterval 秒，视觉节奏一致
            toggleTimer += Time.deltaTime;
            if (toggleTimer >= toggleInterval)
            {
                toggleTimer -= toggleInterval;
                bigger = !bigger;
            }
            float scale = bigger ? biggerScale : smallerScale;
            transform.localScale = Vector3.one * scale;
        }

        private void OnLaserActivate()
        {
            if (fireFlashRenderer == null) return;
            fireFlashRenderer.enabled = true;
            if (laserLight1 != null) laserLight1.enabled = true;
            if (laserLight2 != null) laserLight2.enabled = true;
        }

        private void OnLaserDeactivate()
        {
            if (fireFlashRenderer == null) return;
            fireFlashRenderer.enabled = false;
            if (laserLight1 != null) laserLight1.enabled = false;
            if (laserLight2 != null) laserLight2.enabled = false;
        }
    }
}
