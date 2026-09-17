using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 血印掉落物（对标 Shield / MP5Unlock）。
    // 生成时由调用方（宝箱/敌人/商店）通过 Initialize 绑定具体血印，
    // 拾取后解锁对应血印并销毁自身。
    public partial class BloodSigilDrop : DropItem
    {
        private BloodSigilSO sigil;

        // 缓存子物体上的 SpriteRenderer（掉落物克隆体为独立实例，缓存安全）
        private SpriteRenderer cachedSpriteRenderer;

        // 绑定掉落物对应的血印配置（由生成方调用），同时用血印 SO 中的 icon 替换预制体默认图标
        public void Initialize(BloodSigilSO so)
        {
            sigil = so;
            if (so == null || so.icon == null) return;
            SelfSpriteRenderer.sprite = so.icon;
        }

        protected override void Collect()
        {
            if (sigil == null)
            {
                Debug.LogWarning("[BloodSigilDrop] 掉落物未绑定血印，已忽略");
                this.DestroyGameObjGracefully();
                return;
            }
            AudioKitManager.Instance.PlayOneShot("SigilPickUp", volume: 1.0f);
            BloodSigilManager.Instance?.Unlock(sigil);
            this.DestroyGameObjGracefully();
        }
    }
}