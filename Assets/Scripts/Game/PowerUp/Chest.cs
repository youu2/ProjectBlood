using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Chest : ViewController
    {
        bool isCollected;
        static int currentWeaponIndex = 0;
        void Start()
        {
            isCollected = false;
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            // Check if the collider belongs to the player
            if (collider.GetComponent<CollectBox>() != null && !isCollected)
            {
                AudioKitManager.Instance.PlayOneShot("RareLootSFX", volume: 1.0f);
                SelfSpriteRenderer.enabled = false;  // 禁用未开启状态宝箱的渲染器
                ChestOpenSprite.Show();
                isCollected = true;


                if (currentWeaponIndex < weaponDataList.Count)
                {

                    WeaponDataSystem.weaponDataList.Add(weaponDataList[currentWeaponIndex]);
                    Player.player1.UpdateSpecialReloadCost();
                    currentWeaponIndex++;

                }
                else
                {
                    // 延迟 45 帧后生成战利品
                    ActionKit.DelayFrame(45, () =>
                    {
                        DropManager.Instance.DirtyBlood.Instantiate()
                        .Position(this.transform.position + new Vector3(0, 1.3f, 0))  // slight offset for better visibility
                        .Show();
                    }).Start(this);
                }
            }
        }
        private readonly List<WeaponData> weaponDataList = new(){
            WeaponConfig.MP5.NewWeapon(),
            WeaponConfig.ShotGun.NewWeapon(),
            WeaponConfig.AK.NewWeapon(),
            WeaponConfig.AWP.NewWeapon(),
            WeaponConfig.Laser.NewWeapon(),
        };
    }
}