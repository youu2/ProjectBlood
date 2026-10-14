using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 商店商品初始化钩子：生成物需要运行时绑定数据（如随机血印）时实现此接口
    // CanDeliver 在扣款前调用；通过后同一同步帧内执行 InitializeFromShop，必然成功，无需退款路径
    public interface IShopInitializable
    {
        bool CanDeliver();
        void InitializeFromShop();
    }

    // 商店权重修饰钩子：返回权重乘数（0 = 本次不出现，1 = 不变）
    public interface IShopWeightModifier
    {
        float GetWeightMultiplier();
    }

    // 商店唯一商品契约：同一次刷新内同 key 商品全房间只允许上架一个
    //（如武器：玩家至多持有一把，同房间重复上架必然产生买后无效的废品货架）
    public interface IUniqueShopGoods
    {
        string UniqueKey { get; }
    }

    [System.Serializable]
    public class ShopGoods
    {
        public ViewController item;
        public int price = 1;
        [Min(1)] public int count = 1;
        [Min(0f)] public float weight = 1f;
    }

    public partial class ShopItem : InteractableBase
    {
        [Header("商品池")]
        public List<ShopGoods> goodsPool = new List<ShopGoods>();

        protected ShopGoods goods;
        protected int price;

        public override bool CanInteract => !isCollected && goods != null && goods.item != null;

        void Start()
        {
            // 货架初始为空：商品生成延迟到预进场触发（ShopRoomZone）或进房兜底时机，
            // 保证基于玩家最新状态掷点，且上架过程发生在走廊中、玩家不可见
            Icon.Hide();
            PriceText.Hide();
        }

        // 幂等刷新：已生成或已购买则跳过；由 ShopRoomZone 统一触发，全场货架同帧上架
        // takenKeys：本轮刷新内已被其他货架抽中的唯一商品 key（跨货架去重）
        public void RefreshGoods(HashSet<string> takenKeys = null)
        {
            if (goods != null || isCollected) return;
            InitGoodsFromPool(takenKeys);
            if (goods == null) return; // 空店：保持空货架，CanInteract 恒 false
            var unique = goods.item.GetComponent<IUniqueShopGoods>();
            if (unique != null) takenKeys?.Add(unique.UniqueKey);
            Icon.Show();
            PriceText.text = price.ToString();
            PriceText.Show();
        }

        // 有效权重 = 基础权重 × 动态乘数（0 表示本次不出现；已被本轮其他货架抽中的唯一商品也为 0）
        private float EffectiveWeight(ShopGoods g, HashSet<string> takenKeys)
        {
            if (g == null || g.item == null) return 0f;
            var unique = g.item.GetComponent<IUniqueShopGoods>();
            if (unique != null && takenKeys != null && takenKeys.Contains(unique.UniqueKey)) return 0f;
            float mul = g.item is IShopWeightModifier m ? m.GetWeightMultiplier() : 1f;
            return Mathf.Max(0f, g.weight) * Mathf.Max(0f, mul);
        }

        private void InitGoodsFromPool(HashSet<string> takenKeys)
        {
            if (goods != null)
            {
                return;
            }

            float total = 0f;
            foreach (var g in goodsPool) total += EffectiveWeight(g, takenKeys);
            if (total <= 0f) return;

            float roll = Random.Range(0f, total);
            foreach (var g in goodsPool)
            {
                float w = EffectiveWeight(g, takenKeys);
                if (w <= 0f) continue;
                roll -= w;
                if (roll <= 0f)
                {
                    goods = g;
                    price = g.price;
                    UpdateIcon(g.item);
                    break;
                }
            }
        }

        private void UpdateIcon(ViewController item)
        {
            var spriteRenderer = item.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Icon.sprite = spriteRenderer.sprite;
            }
        }

        protected override void DoInteract()
        {
            if (goods == null || goods.item == null)
            {
                return;
            }

            if (goods.item is IShopInitializable pending && !pending.CanDeliver())
            {
                Player.DisplayText("已售罄");
                return;
            }

            if (Global.Coin.Value >= price)
            {
                Global.SpendCoin(price);

                int count = Mathf.Max(1, goods.count);
                for (int i = 0; i < count; i++)
                {
                    var clone = goods.item.Instantiate()
                        .Position(transform.Position2D() + new Vector2(0, 0.2f));
                    (clone as IShopInitializable)?.InitializeFromShop();
                    clone.Show();
                }

                Icon.Hide();
                PriceText.Hide();
                Tips.Hide();
                isCollected = true;

                AudioKitManager.Instance.PlayOneShot("BuySound", volume: 0.8f);
            }
            else
            {
                Player.DisplayText("Too expensive...");
            }
        }
    }
}
