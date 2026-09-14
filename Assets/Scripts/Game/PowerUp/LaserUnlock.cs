using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LaserUnlock : DropItem
    {
        public AudioClip CollectSound;
        protected override void Collect()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.Laser.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
