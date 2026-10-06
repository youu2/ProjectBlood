using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class AutomaticWeapon : WeaponBase
    {
        protected bool hasFired = false;    // 记录是否真的射击过，避免无弹药时也会播放射击结束音效的问题
        protected AudioPlayer _shootLoopPlayer;
        [SerializeField] protected AudioClip OneShotSound;// 开火时播放一次的音效
        [SerializeField] protected AudioClip ShootEndSound;// 射击结束时播放一次的音效
        [SerializeField] protected float OnShotVolume = 0.65f;
        [SerializeField] protected float ShootEndVolume = 0.65f;

        public override void StartAttacking()
        {
            if (gunClip?.CanShoot() ?? false)
            {
                // 播放单发音效和循环音效
                AudioKitManager.Instance?.PlayOneShot(OneShotSound, volume: OnShotVolume);
                int randomIndex = Random.Range(0, ShootSounds.Count);
                _shootLoopPlayer = AudioKitManager.Instance?.PlayLoop(ShootSounds[randomIndex], volume: FireVolume);
                newClip = false;
                hasFired = true;
            }
        }

        public override void KeepAttacking(Vector2 shootDir)
        {
            base.KeepAttacking(shootDir);

            // 开火事件结算（如血印"开火切枪"）可能在 base 内部同步把当前武器切走：
            // 此时 SwitchFromSet 已对本枪 StopAttacking（循环音已停）并置 newClip=true。
            // 若继续按 newClip 续播，会给已切走的枪重新播一层循环音且覆盖播放器引用，
            // 该循环音此后永远无法被停止 → 每切一轮叠加一层。
            if (Player.player1 == null || Player.player1.currentWeapon != this) return;

            // 全程按住左键换弹后，要重新开始播放射击循环音效
            if (newClip && gunClip != null && gunClip.CanShoot())
            {
                StartAttacking();
                newClip = false;
            }
        }

        public override void StopAttacking()
        {
            if (hasFired)   // 避免没弹药时松开左键也会播放射击结束音效的问题
            {
                AudioKitManager.Instance?.PlayOneShot(ShootEndSound, volume: ShootEndVolume);
            }
            AudioKitManager.Instance?.Stop(_shootLoopPlayer);
            _shootLoopPlayer = null;
            hasFired = false;
        }

        // 销毁兜底：玩家死亡/场景卸载时武器对象随之销毁，
        // 此时可能仍按住左键（收不到鼠标抬起），循环开火音必须在此停掉。
        // 只停音频，不调用完整 StopAttacking：销毁顺序不保证，WeaponAnimator 等组件可能已失效。
        protected virtual void OnDestroy()
        {
            AudioKitManager.Instance?.Stop(_shootLoopPlayer);
            _shootLoopPlayer = null;
        }
    }
}