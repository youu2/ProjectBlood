using QFramework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ProjectBlood
{
    [ViewControllerChild]
    public abstract class WeaponBase : ViewController
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
        public BloodBank BloodBank { get; set; } // 血液银行引用
        public LifestealFeature Lifesteal { get; set; } = new LifestealFeature(); // 吸血功能
        public bool IsBulletEnhanced { get; protected set; } = true; // 当前弹夹是否被血库强化
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
            var bullet = Instantiate(Bullet, Bullet.transform.position, bulletRotation);
            bullet.direction = finalDirection;
            bullet.gameObject.SetActive(true);
            ApplyLifestealToBullet(bullet);

            fireFlash.Flash(bullet.transform.position, shootDir); // 显示枪口火焰特效

            //镜头震动
            CameraUtils.ShakeMainCamera(CameraShakeIntensity, CameraShakeDuration);
            WeaponAnimator.SetTrigger(ShootAnimatioTrigger);
        }

        public virtual void KeepAttacking()
        {
            Vector2 shootDir = transform.right;
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
                    PlayFirstDryFireClick();
                    reloadTextShown = true; // 标记已经显示过 reload 文本
                }
            }
            TryPlayDryFireClick();
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
            IsBulletEnhanced = BloodBank != null && BloodBank.CurrentBloodAmount > 0;

            // 消耗血液（血库为空时也能换弹，只是子弹不会被强化）
            if (IsBulletEnhanced)
            {
                BloodBank.RemoveBlood(BloodRequired);
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

        protected void PlayFirstDryFireClick()
        {
            if (gunClip != null)
            {
                Player.DisplayText("[R] to Reload!");
                AudioKitManager.Instance?.PlayOneShot("DryFireClick", volume: DryFireClickVolume);
            }
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

        protected void TryPlayDryFireClick()
        {
            if (Time.frameCount % 50 == 0 && attackInterval.CanAttack() && gunClip != null && !gunClip.isReloading)
            {
                AudioKitManager.Instance?.PlayOneShot("DryFireClick", volume: DryFireClickVolume);
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
    }
}