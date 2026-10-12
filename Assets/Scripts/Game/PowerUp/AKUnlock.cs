using ProjectBlood;
using QFramework;
using UnityEngine;

public class AKUnlock : InteractableBase
{
    public AudioClip CollectSound;
    protected override void DoInteract()
    {
        AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
        WeaponDataSystem.weaponDataList.Add(WeaponConfig.AK.NewWeapon());
        Player.player1.UpdateSpecialReloadCost();
        this.DestroyGameObjGracefully();
    }
}

