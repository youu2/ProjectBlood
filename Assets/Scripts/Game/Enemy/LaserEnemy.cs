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
        public LineRenderer laserLine;
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
        public float damageAmount = 10f;

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

        [Header("=== Bounce Settings ===")]
        public bool enableBounce = false;
        public int maxBounceCount = 2;

        [Header("=== Audio Settings ===")]
        public AudioClip chargeSound;
        public AudioClip fireSound;
        public AudioClip laserLoopSound;
        public AudioClip attackEndSound;

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
        protected List<Vector3> laserPoints = new List<Vector3>();
        protected float currentWanderTime = 0f;
        protected Vector3 wanderDirection;

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

            if (laserLine == null)
                laserLine = GetComponent<LineRenderer>();

            if (laserLine != null)
            {
                laserLine.startWidth = laserWidth;
                laserLine.endWidth = laserWidth;
                laserLine.startColor = laserColor;
                laserLine.endColor = laserColor;
                laserLine.enabled = false;
            }

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
            maxBounceCount = Mathf.Max(0, maxBounceCount);
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
                    break;
            }
        }

        void UpdateChase()
        {
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
            Vector3 dirToPlayer = GetDirectionToPlayer();
            SmoothRotate(dirToPlayer);
        }

        Vector3 GetDirectionToPlayer()
        {
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
            if (laserLine == null) return;

            laserLine.enabled = true;
            if (chargeMaterial != null)
                laserLine.material = chargeMaterial;
            laserLine.startWidth = laserWidth * chargeProgress;
            laserLine.endWidth = laserWidth * chargeProgress;
            // laserLine.startColor = Color.Lerp(Color.red, laserColor, chargeProgress);
            // laserLine.endColor = Color.Lerp(Color.red, laserColor, chargeProgress);
            Vector3 startPos = transform.position + transform.right * laserStartOffset;
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, startPos + transform.right * attackRange);
        }

        IEnumerator FireSequence()
        {
            currentState = State.Fire;
            AudioManager.PlayOneShot(fireSound);
            _loopPlayer = AudioManager.PlayLoop(laserLoopSound);

            if (laserLine != null)
            {
                if (fireMaterial != null)
                    laserLine.material = fireMaterial;
                laserLine.startWidth = laserWidth;
                laserLine.endWidth = laserWidth;
                laserLine.startColor = laserColor;
                laserLine.endColor = laserColor;
            }

            damageCoroutine = StartCoroutine(ApplyContinuousDamage());
            yield return StartCoroutine(UpdateLaserBeam());
            StopCoroutine(damageCoroutine);
            AudioManager.Stop(_loopPlayer);
            AudioManager.PlayOneShot(attackEndSound);
            // AudioKit.PlaySound(attackEndSound);
            yield return StartCoroutine(FadeOutLaser());
            laserPoints.Clear();
            currentState = State.Chase;
        }

        IEnumerator UpdateLaserBeam()
        {
            float elapsed = 0f;

            while (elapsed < laserDuration)
            {
                Vector3 dirToPlayer = GetDirectionToPlayer();
                SmoothRotate(dirToPlayer);

                for (int i = 0; i < laserCount; i++)
                {
                    float angleOffset = GetLaserAngleOffset(i);
                    Vector3 laserDir = RotateVector(transform.right, angleOffset);
                    Vector3 startPos = transform.position + transform.right * laserStartOffset;
                    CalculateLaserPath(startPos, laserDir, maxBounceCount);
                    UpdateLaserVisuals();
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

        void CalculateLaserPath(Vector3 start, Vector3 direction, int bouncesLeft)
        {
            if (bouncesLeft < 0) return;

            laserPoints.Clear();
            laserPoints.Add(start);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, Mathf.Infinity, LayerMask.GetMask("Wall"));

            if (hit.collider != null)
            {
                laserPoints.Add(hit.point);
                CheckEnemyDamage(start, hit.point);

                if (enableBounce && bouncesLeft > 0 && hit.collider.CompareTag("Wall"))
                {
                    Vector3 reflectedDir = Vector3.Reflect(direction, hit.normal).normalized;
                    CalculateLaserPath((Vector3)hit.point + reflectedDir * 0.1f, reflectedDir, bouncesLeft - 1);
                }
            }
            else
            {
                Vector3 endPoint = start + direction * 50f;
                laserPoints.Add(endPoint);
                CheckEnemyDamage(start, endPoint);
            }
        }

        void UpdateLaserVisuals()
        {
            if (laserLine == null || laserPoints.Count < 2) return;

            laserLine.positionCount = laserPoints.Count;
            laserLine.SetPositions(laserPoints.ToArray());
        }

        void CheckEnemyDamage(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, LayerMask.GetMask("Enemy"));

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDying)
                {
                    damageable.TakeDamage(damageAmount);
                }
            }
        }

        IEnumerator ApplyContinuousDamage()
        {
            float interval = 1f / damageFrequency;

            while (currentState == State.Fire)
            {
                for (int i = 0; i < laserPoints.Count - 1; i++)
                {
                    CheckEnemyDamage(laserPoints[i], laserPoints[i + 1]);
                }

                yield return new WaitForSeconds(interval);
            }
        }

        IEnumerator FadeOutLaser()
        {
            if (laserLine == null) yield break;

            float duration = 0.2f;
            float elapsed = 0f;
            float startWidth = laserWidth;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float width = Mathf.Lerp(startWidth, 0f, t);

                laserLine.startWidth = width;
                laserLine.endWidth = width;

                elapsed += Time.deltaTime;
                yield return null;
            }

            HideLaser();
        }

        void HideLaser()
        {
            if (laserLine != null)
                laserLine.enabled = false;
        }

        protected override IEnumerator DeathSequence()
        {
            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);

            HideLaser();

            yield return StartCoroutine(base.DeathSequence());
        }

        protected override void FixedUpdate()
        {
        }
    }
}