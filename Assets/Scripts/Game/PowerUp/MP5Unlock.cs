using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class MP5Unlock : DropItem
    {
        public AudioClip CollectSound;
        protected override void Collect()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.MP5.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
