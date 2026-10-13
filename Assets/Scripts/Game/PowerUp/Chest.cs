using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Chest : InteractableBase
    {
        [SerializeField] private List<InteractableBase> weaponUnlockList;

        // 扫描掉落池：返回第一个玩家未持有的武器掉落物，全已持有或池空返回 null
        private InteractableBase FindNextUnownedWeapon()
        {
            foreach (var item in weaponUnlockList)
            {
                if (item == null) continue;
                var unlock = item.GetComponent<WeaponUnlockBase>();
                if (unlock == null) continue;
                if (!WeaponDataSystem.HasWeapon(unlock.Config.weaponName)) return item;
            }
            return null;
        }

        void Start()
        {
            isCollected = false;
        }

        protected override void DoInteract()
        {
            AudioKitManager.Instance.PlayOneShot("RareLootSFX", volume: 1.0f);
            SelfSpriteRenderer.enabled = false;  // 禁用未开启状态宝箱的渲染器
            ChestOpenSprite.Show();
            isCollected = true;
            Tips.Hide();

            var weaponToGenerate = FindNextUnownedWeapon();
            if (weaponToGenerate != null)
            {
                ActionKit.DelayFrame(45, () =>
                {
                    weaponToGenerate.Instantiate()
                    .Position(this.transform.position + new Vector3(0, 1.3f, 0))  // slight offset for better visibility
                    .Show();
                }).Start(this);
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

        // private readonly List<DropItem> weaponUnlockList = new()
        // {
        //     DropManager.Instance.MP5Unlock,
        // };
    }
}