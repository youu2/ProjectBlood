using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class ShotGunUnlock : DropItem
    {
        public AudioClip CollectSound;
        protected override void Collect()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.ShotGun.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
