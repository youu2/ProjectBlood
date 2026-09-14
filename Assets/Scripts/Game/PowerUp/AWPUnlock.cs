using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class AWPUnlock : DropItem
    {
        public AudioClip CollectSound;
        protected override void Collect()
        {
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.weaponDataList.Add(WeaponConfig.AWP.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
