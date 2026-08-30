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
        protected int frameCounter = 0;
        protected int framesPerSprite = 10;
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
            frameCounter++;
            if (frameCounter >= framesPerSprite)
            {
                frameCounter = 0;
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
