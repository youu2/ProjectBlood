using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LaserEnemy : Enemy
    {
        private AudioKitManager AudioManager = new AudioKitManager();
        private AudioPlayer _loopPlayer;

        [Header("=== Laser Settings ===")]
        public LineRenderer laserLinePrefab;
        [Range(0.1f, 2f)]
        public float laserWidth = 0.3f;
        public Color laserColor = Color.magenta;
        public float laserDuration = 0.8f;
        public Material chargeMaterial;
        public Material fireMaterial;
        public float laserStartOffset = 0.55f;

        [Header("=== Attack Settings ===")]
        public float chargeTime = 1.5f;
        public float damageFrequency = 5f;
        public float damagePerHit = 10f;

        [Header("=== Movement Settings ===")]
        public float rotationSpeed = 180f;
        public float attackRange = 15f;
        public float chaseRange = 20f;
        public float wanderDuration = 1.0f;

        [Header("=== Multi-Laser Settings ===")]
        public int laserCount = 1;
        [Range(0f, 180f)]
        public float spreadAngle = 0f;
        public bool randomSpread = false;

        [Header("=== Audio Settings ===")]
        public AudioClip chargeSound;
        public AudioClip fireSound;
        public AudioClip laserLoopSound;
        public AudioClip attackEndSound;

        [Header("=== Fire Flash Settings ===")]
        public SpriteRenderer fireFlashRenderer;
        public Sprite[] fireFlashSprites;
        public int framesPerSprite = 3;

        public enum State
        {
            Idle,
            Chase,
            Wander,
            Aim,
            Fire
        }
        public State currentState = State.Idle;

        protected Player player;
        protected float chargeProgress = 0f;
        protected Coroutine damageCoroutine;
        protected List<List<Vector3>> laserPointsList = new List<List<Vector3>>();
        protected List<LineRenderer> laserLines = new List<LineRenderer>();
        protected float currentWanderTime = 0f;
        protected Vector3 wanderDirection;
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
                laserPointsList.Add(new List<Vector3>());
            }
        }

        void Update()
        {
            if (isDying) return;

            if (player == null)
            {
                currentState = State.Idle;
                return;
            }

            switch (currentState)
            {
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Wander:
                    UpdateWander();
                    break;
                case State.Aim:
                    UpdateAim();
                    break;
                case State.Fire:
                    UpdateFireFlash();
                    break;
            }
        }

        void UpdateChase()
        {
            if (player == null) return;
            
            Vector3 dirToPlayer = GetDirectionToPlayer();
            SmoothRotate(dirToPlayer);
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, player.transform.position) <= chaseRange)
            {
                currentState = State.Wander;
                StartWander();
            }
        }

        void StartWander()
        {
            currentWanderTime = 0f;
            Vector3 dirToPlayer = GetDirectionToPlayer();
            Vector3 perpendicular = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0);
            wanderDirection = Random.Range(0, 2) == 0 ? perpendicular : -perpendicular;
        }

        void UpdateWander()
        {
            if (player == null) return;
            
            transform.position += wanderDirection * moveSpeed * Time.deltaTime;
            currentWanderTime += Time.deltaTime;

            Vector3 dirToPlayer = GetDirectionToPlayer();
            SmoothRotate(dirToPlayer);

            if (currentWanderTime >= wanderDuration)
            {
                currentState = State.Aim;
                StartCoroutine(ChargeSequence());
            }

            if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
            {
                currentState = State.Chase;
            }
        }

        void UpdateAim()
        {
            if (player == null) return;
            
            Vector3 dirToPlayer = GetDirectionToPlayer();
            SmoothRotate(dirToPlayer);
        }

        void UpdateFireFlash()
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

        Vector3 GetDirectionToPlayer()
        {
            if (player == null)
                return transform.right;
            return (player.transform.position - transform.position).normalized;
        }

        void SmoothRotate(Vector3 direction)
        {
            if (direction.x == 0 && direction.y == 0) return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime / 180f);
            transform.eulerAngles = new Vector3(0, 0, newAngle);
        }

        IEnumerator ChargeSequence()
        {
            AudioKit.PlaySound(chargeSound);
            chargeProgress = 0f;

            while (chargeProgress < 1f)
            {
                if (player == null)
                {
                    HideLaser();
                    yield break;
                }
                
                chargeProgress += Time.deltaTime / chargeTime;
                Vector3 dirToPlayer = GetDirectionToPlayer();
                SmoothRotate(dirToPlayer);
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
            AudioManager.PlayOneShot(fireSound);
            _loopPlayer = AudioManager.PlayLoop(laserLoopSound);

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

            damageCoroutine = StartCoroutine(ApplyContinuousDamage());
            yield return StartCoroutine(UpdateLaserBeam());
            StopCoroutine(damageCoroutine);
            AudioManager.Stop(_loopPlayer);
            AudioManager.PlayOneShot(attackEndSound);
            yield return StartCoroutine(FadeOutLaser());
            HideFireFlash();
            foreach (var points in laserPointsList)
                points.Clear();
            currentState = State.Chase;
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
                
                Vector3 dirToPlayer = GetDirectionToPlayer();
                SmoothRotate(dirToPlayer);

                for (int i = 0; i < laserCount; i++)
                {
                    float angleOffset = GetLaserAngleOffset(i);
                    Vector3 laserDir = RotateVector(transform.right, angleOffset);
                    Vector3 startPos = transform.position + transform.right * laserStartOffset;
                    
                    laserPointsList[i].Clear();
                    CalculateLaserPath(i, startPos, laserDir);
                    UpdateLaserVisuals(i);
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

        void CalculateLaserPath(int laserIndex, Vector3 start, Vector3 direction)
        {
            laserPointsList[laserIndex].Add(start);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, Mathf.Infinity, LayerMask.GetMask("Wall"));

            if (hit.collider != null)
            {
                laserPointsList[laserIndex].Add(hit.point);
            }
            else
            {
                Vector3 endPoint = start + direction * attackRange;
                laserPointsList[laserIndex].Add(endPoint);
            }
        }

        void UpdateLaserVisuals(int laserIndex)
        {
            if (laserLines.Count <= laserIndex || laserPointsList.Count <= laserIndex) return;
            
            LineRenderer lr = laserLines[laserIndex];
            List<Vector3> points = laserPointsList[laserIndex];
            
            if (lr == null || points.Count < 2) return;

            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
        }

        void CheckPlayerDamage(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, LayerMask.GetMask("Player"));

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

        // protected override IEnumerator DeathSequence()
        // {
        //     if (damageCoroutine != null)
        //         StopCoroutine(damageCoroutine);

        //     AudioManager.Stop(_loopPlayer);
        //     HideLaser();
        //     HideFireFlash();

        //     yield return StartCoroutine(base.DeathSequence());
        // }

        protected override void FixedUpdate()
        {
        }

        public override void OnDestroy()
        {
            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);
            AudioManager.Stop(_loopPlayer);
            HideLaser();
            HideFireFlash();
            base.OnDestroy();
        }
    }
}