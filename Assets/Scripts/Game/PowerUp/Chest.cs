using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Chest : ViewController
    {
        bool isCollected;
        static int currentWeaponIndex = 0;
        [SerializeField] private List<DropItem> weaponUnlockList;
        void Start()
        {
            isCollected = false;
        }

        private void Update()
        {
            if (!Tips.gameObject.activeSelf)
            {
                return;
            }

            if (!Input.GetKeyDown(KeyCode.F))
            {
                return;
            }

            if (!isCollected)
            {
                AudioKitManager.Instance.PlayOneShot("RareLootSFX", volume: 1.0f);
                SelfSpriteRenderer.enabled = false;  // 禁用未开启状态宝箱的渲染器
                ChestOpenSprite.Show();
                isCollected = true;
                Tips.Hide();

                if (currentWeaponIndex < weaponUnlockList.Count)
                {
                    var weaponToGenerate = weaponUnlockList[currentWeaponIndex];
                    ActionKit.DelayFrame(45, () =>
                    {
                        weaponToGenerate.Instantiate()
                        .Position(this.transform.position + new Vector3(0, 1.3f, 0))  // slight offset for better visibility
                        .Show();
                    }).Start(this);

                    // WeaponDataSystem.weaponDataList.Add(weaponDataList[currentWeaponIndex]);
                    Player.player1.UpdateSpecialReloadCost();
                    currentWeaponIndex++;

                }
                else
                {
                    // 延迟 45 帧后生成战利品：优先掉落随机血印，池空时回落为 DirtyBlood
                    ActionKit.DelayFrame(45, () =>
                    {
                        var spawnPos = this.transform.position + new Vector3(0, 1.3f, 0);
                        BloodSigilSO sigil = BloodSigilManager.Instance != null
                            ? BloodSigilManager.Instance.GetRandomSigil()
                            : null;

                        if (sigil != null && DropManager.Instance.BloodSigilDrop != null)
                        {
                            var drop = DropManager.Instance.BloodSigilDrop.Instantiate()
                                .Position(spawnPos);
                            drop.Initialize(sigil);
                            drop.Show();
                        }
                        else
                        {
                            DropManager.Instance.DirtyBlood.Instantiate()
                            .Position(spawnPos)  // slight offset for better visibility
                            .Show();
                        }
                    }).Start(this);
                }
            }
        }

        // private readonly List<DropItem> weaponUnlockList = new()
        // {
        //     DropManager.Instance.MP5Unlock,
        // };

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isCollected)
            {
                Tips.Show();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Tips.Hide();
            }
        }
    }
}