using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LaserEnemy : Enemy
    {
        private AudioPlayer _loopPlayer;

        [Header("=== 激光设置 ===")]
        public LineRenderer laserLinePrefab;
        [Range(0.1f, 2f)] public float laserWidth = 0.3f;
        public Color laserColor = Color.magenta;
        public float laserDuration = 0.8f;
        public Material chargeMaterial;
        public Material fireMaterial;
        public float laserStartOffset = 0.55f;
        public float chargeTime = 1.5f;

        [Header("=== 攻击设置 ===")]
        public float damageFrequency = 5f;
        public float damagePerHit = 10f;
        public float rotationSpeed = 180f;

        [Header("=== 多激光设置 ===")]
        public int laserCount = 1;
        [Range(0f, 360f)]
        public float spreadAngle = 0f;
        public bool randomSpread = false;

        [Header("=== 音效设置 ===")]
        public AudioClip chargeSound;
        public AudioClip fireSound;
        public AudioClip laserLoopSound;
        public AudioClip attackEndSound;

        [Header("=== 激光点设置 ===")]
        public SpriteRenderer fireFlashRenderer;
        public Sprite[] fireFlashSprites;
        public int framesPerSprite = 3;
        protected Player player;
        protected float chargeProgress = 0f;
        private Coroutine _damageCoroutine;
        private Coroutine _chargeCoroutine;
        protected List<List<Vector3>> laserPointsList = new List<List<Vector3>>();
        protected List<LineRenderer> laserLines = new List<LineRenderer>();
        protected List<Vector3[]> laserPointArrays = new List<Vector3[]>();
        private int _wallLayer;
        private int _playerLayer;
        // protected float currentWanderTime = 0f;
        // protected Vector3 wanderDirection;
        protected int currentSpriteIndex = 0;
        protected int frameCounter = 0;

        protected override void Awake()
        {
            base.Awake();
            useFlipSprite = false;
        }

        void Start()
        {
            InitializeComponents();
            ValidateParameters();

            if (player != null)
                currentState = State.Chase;
        }

        void InitializeComponents()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _wallLayer = LayerMask.GetMask("Wall");
            _playerLayer = LayerMask.GetMask("Player");

            CreateLaserLines();

            if (fireFlashRenderer != null)
                fireFlashRenderer.enabled = false;

            if (player == null)
                player = Player.player1;
        }

        void ValidateParameters()
        {
            laserCount = Mathf.Max(1, laserCount);
            spreadAngle = Mathf.Max(0, spreadAngle);
            chargeTime = Mathf.Max(0, chargeTime);
            laserDuration = Mathf.Max(0.1f, laserDuration);
            damageFrequency = Mathf.Max(1f, damageFrequency);
            framesPerSprite = Mathf.Max(1, framesPerSprite);
        }

        void CreateLaserLines()
        {
            for (int i = 0; i < laserCount; i++)
            {
                LineRenderer lr;
                if (laserLinePrefab != null)
                {
                    lr = Instantiate(laserLinePrefab, transform);
                    lr.name = "LaserLine_" + i;
                }
                else
                {
                    GameObject laserObj = new GameObject("LaserLine_" + i);
                    laserObj.transform.parent = transform;
                    lr = laserObj.AddComponent<LineRenderer>();
                }

                lr.startWidth = laserWidth;
                lr.endWidth = laserWidth;
                lr.startColor = laserColor;
                lr.endColor = laserColor;
                lr.enabled = false;

                laserLines.Add(lr);
                laserPointsList.Add(new List<Vector3>(2));
                laserPointArrays.Add(new Vector3[2]);
            }
        }

        protected override void StartFire()
        {
            currentState = State.Fire;
            if (_chargeCoroutine != null)
                StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = StartCoroutine(ChargeSequence());
        }

        protected override void UpdateFire(float distanceToPlaye)  // 更新枪口激光点
        {
            if (fireFlashRenderer == null || fireFlashSprites == null || fireFlashSprites.Length == 0)
                return;

            frameCounter++;
            if (frameCounter >= framesPerSprite)
            {
                frameCounter = 0;
                currentSpriteIndex = (currentSpriteIndex + 1) % fireFlashSprites.Length;
                fireFlashRenderer.sprite = fireFlashSprites[currentSpriteIndex];
            }
        }

        protected void UpdateFireFlash()  // 更新枪口激光点
        {
            if (fireFlashRenderer == null || fireFlashSprites == null || fireFlashSprites.Length == 0)
                return;

            frameCounter++;
            if (frameCounter >= framesPerSprite)
            {
                frameCounter = 0;
                currentSpriteIndex = (currentSpriteIndex + 1) % fireFlashSprites.Length;
                fireFlashRenderer.sprite = fireFlashSprites[currentSpriteIndex];
            }
        }

        IEnumerator ChargeSequence()
        {
            AudioKitManager.Instance.PlayOneShot(chargeSound);
            chargeProgress = 0f;

            while (chargeProgress < 1f)
            {
                if (player == null)
                {
                    HideLaser();
                    yield break;
                }

                chargeProgress += Time.deltaTime / chargeTime;
                ShowChargeIndicator();

                if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
                {
                    HideLaser();
                    currentState = State.Chase;
                    yield break;
                }

                yield return null;
            }

            StartCoroutine(FireSequence());
        }

        void ShowChargeIndicator()
        {
            if (laserLines.Count == 0) return;

            Vector3 startPos = transform.position + transform.right * laserStartOffset;
            float width = laserWidth * chargeProgress;

            for (int i = 0; i < laserLines.Count; i++)
            {
                LineRenderer lr = laserLines[i];
                if (lr == null) continue;

                lr.enabled = true;
                if (chargeMaterial != null)
                    lr.material = chargeMaterial;
                lr.startWidth = width;
                lr.endWidth = width;

                float angleOffset = GetLaserAngleOffset(i);
                Vector3 laserDir = RotateVector(transform.right, angleOffset);
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, startPos + laserDir * attackRange);
            }
        }

        IEnumerator FireSequence()
        {
            currentState = State.Fire;
            AudioKitManager.Instance.PlayOneShot(fireSound);
            _loopPlayer = AudioKitManager.Instance.PlayLoop(laserLoopSound);

            foreach (LineRenderer lr in laserLines)
            {
                if (lr != null)
                {
                    if (fireMaterial != null)
                        lr.material = fireMaterial;
                    lr.startWidth = laserWidth;
                    lr.endWidth = laserWidth;
                    lr.startColor = laserColor;
                    lr.endColor = laserColor;
                }
            }

            ShowFireFlash();
            _damageCoroutine = StartCoroutine(ApplyContinuousDamage()); // 持续造成伤害直到退出开火状态
            yield return StartCoroutine(UpdateLaserBeam());
            StopCoroutine(_damageCoroutine);
            AudioKitManager.Instance.Stop(_loopPlayer);
            AudioKitManager.Instance.PlayOneShot(attackEndSound);
            yield return StartCoroutine(FadeOutLaser());
            HideFireFlash();

            foreach (var points in laserPointsList)
                points.Clear();
            currentState = State.Chase; // 激光敌人攻击距离很远，所以攻击完之后直接进入追击状态，这样就能用追击距离控制交火距离
        }

        void ShowFireFlash()
        {
            if (fireFlashRenderer == null || fireFlashSprites == null || fireFlashSprites.Length == 0)
                return;

            currentSpriteIndex = 0;
            frameCounter = 0;
            fireFlashRenderer.sprite = fireFlashSprites[0];
            fireFlashRenderer.enabled = true;
        }

        void HideFireFlash()
        {
            if (fireFlashRenderer != null)
                fireFlashRenderer.enabled = false;
        }

        IEnumerator UpdateLaserBeam()
        {
            float elapsed = 0f;

            while (elapsed < laserDuration)
            {
                if (player == null)
                {
                    HideLaser();
                    yield break;
                }

                UpdateRotate(direction);

                for (int i = 0; i < laserCount; i++)
                {
                    float angleOffset = GetLaserAngleOffset(i);
                    Vector3 laserDir = RotateVector(transform.right, angleOffset);
                    Vector3 startPos = transform.position + transform.right * laserStartOffset;

                    laserPointsList[i].Clear();
                    CalculateLaserPath(i, startPos, laserDir); // 计算激光的起点终点
                    UpdateLaserVisuals(i);  // 绘制
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        float GetLaserAngleOffset(int index)
        {
            if (laserCount <= 1 || spreadAngle <= 0)
                return 0f;

            if (randomSpread)
                return Random.Range(-spreadAngle / 2f, spreadAngle / 2f);

            return (-spreadAngle / 2f) + (spreadAngle / (laserCount - 1f)) * index;
        }

        Vector3 RotateVector(Vector3 vector, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                vector.x * Mathf.Cos(rad) - vector.y * Mathf.Sin(rad),
                vector.x * Mathf.Sin(rad) + vector.y * Mathf.Cos(rad),
                0
            ).normalized;
        }

        // 计算激光路径
        // 1. 检查是否碰撞到墙
        // 2. 如果没有碰撞到墙，计算激光路径到玩家
        void CalculateLaserPath(int laserIndex, Vector3 start, Vector3 direction)
        {
            var points = laserPointsList[laserIndex];
            points.Clear();
            points.Add(start);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, attackRange, _wallLayer);

            if (hit.collider != null)
            {
                points.Add(hit.point);
            }
            else
            {
                points.Add(start + direction * attackRange);
            }
        }

        void UpdateLaserVisuals(int laserIndex)
        {
            if (laserLines.Count <= laserIndex || laserPointsList.Count <= laserIndex) return;

            // 获取当前激光对应的LineRenderer和路径点列表
            LineRenderer lr = laserLines[laserIndex];
            List<Vector3> points = laserPointsList[laserIndex];

            // 路径点不足2个就无法画线
            // 原本有计划实现激光反射，所以可能有两个以上的路径点
            if (lr == null || points.Count < 2) return;

            lr.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                laserPointArrays[laserIndex][i] = points[i];
            }
            lr.SetPositions(laserPointArrays[laserIndex]);
        }

        void CheckPlayerDamage(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, _playerLayer);

            if (hit.collider != null)
            {
                Player.player1.TakeDamage(damagePerHit);
            }
        }

        IEnumerator ApplyContinuousDamage()
        {
            float interval = 1f / damageFrequency;

            while (currentState == State.Fire)
            {
                for (int laserIndex = 0; laserIndex < laserPointsList.Count; laserIndex++)
                {
                    List<Vector3> points = laserPointsList[laserIndex];
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        CheckPlayerDamage(points[i], points[i + 1]);
                    }
                }

                yield return new WaitForSeconds(interval);
            }
        }

        IEnumerator FadeOutLaser()
        {
            if (laserLines.Count == 0) yield break;

            float duration = 0.2f;
            float elapsed = 0f;
            float startWidth = laserWidth;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float width = Mathf.Lerp(startWidth, 0f, t);

                foreach (LineRenderer lr in laserLines)
                {
                    if (lr != null)
                    {
                        lr.startWidth = width;
                        lr.endWidth = width;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            HideLaser();
        }

        void HideLaser()
        {
            foreach (LineRenderer lr in laserLines)
            {
                if (lr != null)
                    lr.enabled = false;
            }
        }

        public override void OnDestroy()
        {
            if (_damageCoroutine != null)
                StopCoroutine(_damageCoroutine);
            if (_chargeCoroutine != null)
                StopCoroutine(_chargeCoroutine);
            AudioKitManager.Instance.Stop(_loopPlayer);
            HideLaser();
            HideFireFlash();
            base.OnDestroy();
        }
    }
}