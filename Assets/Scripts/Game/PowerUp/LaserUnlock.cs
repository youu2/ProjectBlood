using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LaserUnlock : InteractableBase
    {
        public AudioClip CollectSound;
        protected override void DoInteract()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.Laser.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
