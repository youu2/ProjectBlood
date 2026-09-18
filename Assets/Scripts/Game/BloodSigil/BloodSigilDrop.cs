using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 血印掉落物（可交互物）：靠近显示名字与效果描述，按 F 刻印。
    // 生成时由调用方（宝箱/敌人/商店）通过 Initialize 绑定具体血印，
    // 刻印后解锁对应血印并销毁自身。
    public partial class BloodSigilDrop : InteractableBase
    {
        private BloodSigilSO sigil;

        // 缓存子物体上的 SpriteRenderer（掉落物克隆体为独立实例，缓存安全）
        private SpriteRenderer cachedSpriteRenderer;

        // 绑定掉落物对应的血印配置（由生成方调用），用血印 SO 中的 icon 替换预制体默认图标，
        // 并将名字/描述写入 Name 文本框
        public void Initialize(BloodSigilSO so)
        {
            sigil = so;
            if (so == null || so.icon == null) return;
            SelfSpriteRenderer.sprite = so.icon;
            Name.text = $"{so.sigilName}";
            Tips.text = $"\n{so.description}";
        }

        // 靠近时额外显示名字/描述文本，离开时隐藏
        protected override void OnEnterRange(){
            Name.Show(); 
            Tips.Show();
        }
        protected override void OnExitRange(){
            Name.Hide(); 
            Tips.Hide(); 
        }

        protected override void DoInteract()
        {
            if (sigil == null)
            {
                Debug.LogWarning("[BloodSigilDrop] 掉落物未绑定血印，已忽略");
                this.DestroyGameObjGracefully();
                return;
            }
            isCollected = true;
            Tips.Hide();
            Name.Hide();
            AudioKitManager.Instance.PlayOneShot("SigilPickUp", volume: 1.0f);
            BloodSigilManager.Instance?.Unlock(sigil);
            this.DestroyGameObjGracefully();
        }
    }
}
