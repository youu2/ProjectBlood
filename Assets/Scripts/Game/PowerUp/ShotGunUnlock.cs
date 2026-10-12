using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class ShotGunUnlock : InteractableBase
    {
        public AudioClip CollectSound;
        protected override void DoInteract()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.ShotGun.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
