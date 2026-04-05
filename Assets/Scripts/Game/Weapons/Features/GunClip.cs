using System.Collections;
using UnityEngine;

namespace ProjectBlood
{
    // 管理攻击间隔的类，提供CanAttack方法来判断是否可以攻击，以及RecordAttackTime方法来记录攻击时间
    public class GunClip
    {
        public int maxAmmo; // 最大弹药量
        public int currentAmmo; // 当前弹药量
        public bool isReloading; // 是否正在换弹
        public bool isEmpty => currentAmmo <= 0; // 是否弹药已空
        private readonly AudioSource reloadAudioSource;
        private Coroutine reloadCoroutine; // 保存换弹协程的引用
        public GunClip(int maxAmmo, AudioSource reloadAudioSource)
        {
            this.maxAmmo = maxAmmo;
            this.currentAmmo = maxAmmo; // 初始时弹药量为最大值
            this.isReloading = false; // 初始时不在换弹状态
            this.reloadAudioSource = reloadAudioSource; // 用于播放换弹音效的AudioSource组件
            this.reloadCoroutine = null;
        }
        public void Shoot()
        {
            if (CanShoot())
            {
                currentAmmo--; // 射击时减少弹药量
                UpdateClipUI(); // 射击后更新UI显示的弹药信息
            }
        }
        // public void Reload(AudioClip reloadSound)
        // {
        //     if (!isReloading)
        //     {
        //         isReloading = true;
        //         // 添加换弹动画或音效的逻辑
        //         // 换弹完成后重置弹药量
        //         currentAmmo = maxAmmo;
        //         isReloading = false;
        //         UpdateClipUI(); // 换弹完成后更新UI显示的弹药信息
        //     }
        // }

        // 修改 Reload 方法，支持协程
        public void Reload(AudioClip reloadSound, MonoBehaviour owner = null)
        {
            if (!isReloading && owner != null && currentAmmo < maxAmmo)
            {
                reloadCoroutine = owner.StartCoroutine(ReloadCoroutine(reloadSound));
            }
        }
        
        // 协程实现异步换弹
        private IEnumerator ReloadCoroutine(AudioClip reloadSound)
        {
            isReloading = true;
            
            // 步骤1：播放换弹音效
            if (reloadAudioSource != null && reloadSound != null)
            {
                reloadAudioSource.PlayOneShot(reloadSound);
                
                // 等待音效播放完毕
                yield return new WaitForSeconds(reloadSound.length);
            }
            else
            {
                if(reloadAudioSource == null)
                {
                    Debug.LogWarning("Reload audio source is missing. Please assign an audio source for reload sound effects.");
                }
                // 如果没有音效，至少等待一个合理的换弹时间
                yield return new WaitForSeconds(0.1f);
                // Debug.LogWarning("Reload sound or audio source is missing. Skipping wait time for sound effect.");
            }
            
            // 步骤2：音效播放结束后刷新弹夹
            currentAmmo = maxAmmo;
            isReloading = false;
            reloadCoroutine = null; // 协程结束，重置引用
            UpdateClipUI();
            
            // Debug.Log("currentAmmo: " + currentAmmo);
        }
        
        // 停止换弹流程
        public void StopReload(MonoBehaviour owner = null)
        {
            if (isReloading)
            {
                isReloading = false;
                // 停止换弹协程
                if (reloadCoroutine != null && owner != null)
                {
                    owner.StopCoroutine(reloadCoroutine);
                    reloadCoroutine = null;
                }
                // 停止换弹音效
                if (reloadAudioSource != null && reloadAudioSource.isPlaying)
                {
                    reloadAudioSource.Stop();
                }
            }
        }
        
        public bool CanShoot()
        {
            return !isReloading && currentAmmo > 0; // 只有在不换弹且有弹药时才允许射击
        }
        public void UpdateClipUI()
        {
            GameUI.UpdateClipText(this); // 更新UI显示的弹药信息
        }
    }
}