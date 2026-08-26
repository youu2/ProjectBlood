using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public abstract class WeaponBase : MonoBehaviour
    {
        private Coroutine _reloadCoroutine;   // 原先的设计放在GunClip,这会依赖MonoBehaviour,违反单一职责原则
        [SerializeField] protected int MaxAmmo = 10;
        protected GunClip gunClip;
        public PlayerBullet Bullet = new PlayerBullet();
        protected AttackIntervalFeature attackInterval;
        [SerializeField] protected float AttackInterval = 1.0f;
        private AudioPlayer _shootClipPlayer;
        [SerializeField] protected float ReloadTime = 1.5f;
        [SerializeField] protected float DryFireClickVolume = 0.7f;
        [SerializeField] protected float FireVolume = 0.3f;
        [SerializeField] protected float ReloadVolume = 0.7f;
        [SerializeField] protected float ShellVolume = 0.5f;
        protected FireFlash fireFlash = new(); // 枪口火焰特效组件
        [SerializeField] protected float CameraShakeIntensity = 0.15f; // 开火镜头震动强度
        [SerializeField] protected float CameraShakeDuration = 3f; // 开火镜头震动持续时间(帧)
        [SerializeField] public Animator WeaponAnimator;
        [SerializeField] protected string ShootAnimatioTrigger = "SingleShoot";
        [Range(0, 360)][SerializeField] protected float spreadAngle = 2f; // 随机散布角度
        [SerializeField] protected List<AudioClip> ShootSounds = new List<AudioClip>();
        public AudioClip reloadSound;
        [SerializeField] protected int BloodRequired = 5; // 每次换弹需要的血量
        protected bool reloadTextShown = false; // 标记是否已经显示过 reload 提示文本
        protected bool newClip = true;  // 标记是否是新换弹,用于处理按住开火键时的换弹逻辑
        public LifestealFeature Lifesteal { get; set; } = new LifestealFeature(); // 吸血功能
        public bool IsBulletEnhanced { get; protected set; } = true; // 当前弹夹是否被血库强化
        public float AdditionalCameraSize = 0.5f;
        public virtual void StartAttacking() { }
        public abstract void StopAttacking();
        public virtual void InitGunClip()
        {
            if (gunClip == null)
            {
                gunClip = new GunClip(MaxAmmo);
            }
            gunClip.UpdateClipUI();
        }
        public virtual void Awake()
        {
            InitGunClip();
            attackInterval = new AttackIntervalFeature(AttackInterval);
        }

        public virtual void Attack(Vector2 shootDir)
        {
            Vector2 finalDirection = ApplySpread(shootDir);
            // 计算旋转：根据 shootDir 向量创建对应的 Quaternion 朝向
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector2.right, finalDirection.normalized);
            var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);  // 挂在枪口位置
            bullet.direction = finalDirection;
            bullet.gameObject.SetActive(true);

            ApplyLifestealToBullet(bullet); // 应用吸血功能

            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效

            //镜头震动
            CameraUtils.ShakeMainCamera(CameraShakeIntensity, CameraShakeDuration);
            WeaponAnimator.SetTrigger(ShootAnimatioTrigger);
            StartCoroutine(ShellAnimation1(finalDirection));
            // ShellAnimation2(finalDirection);
        }

        public virtual void KeepAttacking(Vector2 shootDir)
        {
            if (attackInterval.CanAttack() && gunClip.CanShoot()) // 只有在满足攻击间隔且有弹药时才允许攻击
            {
                Attack(shootDir);
                attackInterval.RecordAttackTime();
                gunClip.Shoot(); // 射击时减少弹药量
                reloadTextShown = false; // 有弹药时重置 reload 文本显示标记
            }
            else if (!gunClip.CanShoot() && !reloadTextShown)
            {
                StopAttacking();
                newClip = true;
                if (!gunClip.isReloading)
                {
                    StartCoroutine(PlayFirstDryFireClick());
                    reloadTextShown = true; // 标记已经显示过 reload 文本
                }
            }
        }

        // 全自动武器支持随机散布
        protected Vector2 ApplySpread(Vector2 baseDirection)
        {
            if (spreadAngle <= 0f)
            {
                return baseDirection;
            }
            float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
            Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
            return randomRotation * baseDirection;
        }

        public virtual void Reload()
        {
            if (gunClip != null && gunClip.CanReload())
            {
                if (_reloadCoroutine != null)
                {
                    StopCoroutine(_reloadCoroutine);
                }

                _reloadCoroutine = StartCoroutine(ReloadCoroutine());
            }
        }

        private IEnumerator ReloadCoroutine()
        {
            gunClip.StartReload();

            // 播放换弹音效
            if (reloadSound != null || ReloadTime > 0f)
            {
                _shootClipPlayer = AudioKitManager.Instance.PlayOneShot(reloadSound, volume: ReloadVolume);
                yield return new WaitForSeconds(ReloadTime);
            }
            else
            {
                // 默认换弹时间
                yield return new WaitForSeconds(1.5f);
            }

            gunClip.FinishReload();

            // 根据换弹时血库状态决定当前弹夹是否被强化
            IsBulletEnhanced = BloodBank.Instance != null && BloodBank.Instance.CurrentBloodAmount > 0;

            // 消耗血液（血库为空时也能换弹，只是子弹不会被强化）
            if (IsBulletEnhanced)
            {
                BloodBank.Instance.RemoveBlood(BloodRequired);
            }

            _reloadCoroutine = null;
        }
        protected void ApplyLifestealToBullet(PlayerBullet bullet)
        {
            if (bullet != null)
            {
                bullet.lifestealPercent = Lifesteal.LifestealPercent;
                bullet.isEnhanced = IsBulletEnhanced;
            }
        }

        protected IEnumerator PlayFirstDryFireClick()
        {
            if (gunClip != null)
            {
                Player.DisplayText("[R] to Reload!");
                yield return new WaitForSeconds(0.2f);
                AudioKitManager.Instance?.PlayOneShot("DryFireClick", volume: DryFireClickVolume);
            }
        }

        // 抛壳动画unity原生方案：
        protected IEnumerator ShellAnimation1(Vector2 finalDirection)
        {
            var cartridge = DropManager.Instance.Cartridge.gameObject;
            // 生成弹壳
            GameObject shell = Instantiate(cartridge, transform.position + (Vector3)finalDirection * 0.5f, Quaternion.identity);
            shell.SetActive(true);
            Rigidbody2D rb = shell.GetComponent<Rigidbody2D>();

            // 初始速度,角速度,重力为1自由落体，持续0.5-1秒
            Vector2 velocity = -finalDirection * Random.Range(1.6f, 3f) + Vector2.up * Random.Range(3f, 6f);
            rb.velocity = velocity;
            rb.angularVelocity = Random.Range(-500f, 500f);

            float delay1 = Random.Range(0.5f, 1f);
            yield return new WaitForSeconds(delay1);

            // 修改速度,重力为0.1,角速度,持续0.1-0.3秒,模拟弹壳落地弹跳一次
            rb.velocity = -finalDirection * Random.Range(0.6f, 2f) + Vector2.up * Random.Range(0.35f, 0.6f);
            rb.gravityScale = 0.15f;
            System.Random rand = new();
            int dir = rand.Next(2) == 0 ? 1 : -1;
            rb.angularVelocity = Random.Range(300f, 720f) * dir;
            AudioKitManager.Instance?.PlayOneShot($"bullet_shell ({Random.Range(1, 10 + 1)})", volume: ShellVolume);

            float delay2 = Random.Range(0.3f, 0.5f);
            yield return new WaitForSeconds(delay2);
            // 停止
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
        }

        // 使用QF ActionKit 的方案：
        private void ShellAnimation2(Vector2 finalDirection)
        {
            DropManager.Instance.Cartridge.Instantiate()
            .Position((Vector2)transform.position + finalDirection * 0.5f)
            .Show()
            .Self(self =>
            {
                var velocity = -finalDirection * Random.Range(1.6f, 3f) + Vector2.up * Random.Range(3f, 6f);  // 弹壳抛出速度(射击反方向+向上抛出)
                var spriteRander = self.GetComponent<SpriteRenderer>();
                self.velocity = velocity;
                self.angularVelocity = Random.Range(-720, 720);
                ActionKit.Sequence()
                .Delay(Random.Range(0.5f, 1f), () =>
                {
                    self.velocity = -finalDirection * Random.Range(0.6f, 2f) + Vector2.up * Random.Range(0.35f, 0.6f);
                    self.gravityScale = 0.1f;
                    self.angularVelocity = Random.Range(-720, 720);
                })
                .Parallel(s =>
                {
                    s.Delay(Random.Range(0.15f, 0.3f), () =>
                    {
                        self.angularVelocity = 0;
                        self.gravityScale = 0;
                        self.velocity = Vector2.zero;
                    });
                }).Start(this);
            });
        }

        public void StopReload()
        {
            InitGunClip();
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                _reloadCoroutine = null;
            }
            gunClip.CancelReload();
            AudioKitManager.Instance?.Stop(_shootClipPlayer);
        }

        public void FillClipDirectly()
        {
            StopReload();
            if (gunClip != null)
            {
                gunClip.currentAmmo = gunClip.maxAmmo;
                gunClip.isReloading = false;
                gunClip.UpdateClipUI();
            }
        }


        public void SwitchFromSet()
        {
            attackInterval?.Reset();
            newClip = true;
            reloadTextShown = false;
            StopAttacking();
            StopReload();
            Player.HideText();
        }

        public void SwitchToSet()
        {
            InitGunClip();
            gunClip.UpdateClipUI();
            newClip = true;
        }
        public GunClip GetGunClip() => gunClip;
        public WeaponData Data { get; private set; }

        public void LoadWeaponData(WeaponData weaponData)
        {
            Data = weaponData;
            gunClip.currentAmmo = weaponData.weaponCurrentAmmo;
            gunClip.maxAmmo = weaponData.weaponMaxAmmo;
            gunClip.UpdateClipUI();
        }

        public void SaveWeaponData()
        {
            Data.weaponCurrentAmmo = gunClip.currentAmmo;
            Data.weaponMaxAmmo = gunClip.maxAmmo;
            gunClip.UpdateClipUI();
        }
    }
}