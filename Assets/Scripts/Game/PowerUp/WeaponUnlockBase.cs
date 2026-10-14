using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 武器解锁掉落物基类：已持有同名武器时拒绝拾取，并显示头顶提示
    public abstract class WeaponUnlockBase : InteractableBase, IShopWeightModifier, IUniqueShopGoods
    {
        public AudioClip CollectSound;
        public abstract WeaponConfig Config { get; }

        // 唯一商品 key：同房间同轮刷新内同名武器只上架一个货架
        public string UniqueKey => Config.weaponName;

        // 商店权重：已持有该武器则本次不刷出
        public float GetWeightMultiplier()
            => WeaponDataSystem.HasWeapon(Config.weaponName) ? 0f : 1f;

        protected override void DoInteract()
        {
            if (WeaponDataSystem.HasWeapon(Config.weaponName))
            {
                Player.DisplayText("我不需要这个");
                return;
            }
            AudioKitManager.Instance.PlayOneShot(CollectSound, volume: 0.3f);
            WeaponDataSystem.AddWeapon(Config.NewWeapon());
            Player.player1.UpdateSpecialReloadCost();
            this.DestroyGameObjGracefully();
        }
    }
}
