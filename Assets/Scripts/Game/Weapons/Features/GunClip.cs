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

        // 使用事件重构弹夹相关逻辑（音效播放，换弹协程）
        public GunClip(int maxAmmo)
        {
            this.maxAmmo = maxAmmo;
            this.currentAmmo = maxAmmo; // 初始时弹药量为最大值
            this.isReloading = false; // 初始时不在换弹状态
        }
        public void Shoot()
        {
            if (CanShoot())
            {
                currentAmmo--; // 射击时减少弹药量
                UpdateClipUI(); // 射击后更新UI显示的弹药信息
            }
        }

        public bool CanReload()
        {
            return !isReloading && currentAmmo < maxAmmo;
        }

        public void StartReload()
        {
            if (CanReload())
            {
                isReloading = true;
            }
        }

        public void FinishReload()
        {
            currentAmmo = maxAmmo;
            isReloading = false;
            UpdateClipUI();
        }

        public void CancelReload()
        {
            isReloading = false;
        }

        // 停止换弹流程
        public void StopReload()
        {
            if (isReloading)
            {
                isReloading = false;
            }
        }

        public bool CanShoot()
        {
            return !isReloading && !isEmpty; // 只有在不换弹且有弹药时才允许射击
        }
        public void UpdateClipUI()
        {
            GameUI.UpdateClipText(this); // 更新UI显示的弹药信息
        }
    }
}