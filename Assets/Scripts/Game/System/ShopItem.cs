using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 商店商品初始化钩子：生成物需要运行时绑定数据（如随机血印）时实现此接口
    public interface IShopInitializable
    {
        void InitializeFromShop();
    }

    [System.Serializable]
    public class ShopGoods
    {
        public ViewController item;
        public int price = 1;
        [Min(1)] public int count = 1;
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
            InitGoodsFromPool();

            if (goods != null)
            {
                Icon.Show();
                price = goods.price;
            }

            PriceText.text = price.ToString();
            PriceText.Show();
        }

        private void InitGoodsFromPool()
        {
            if (goods != null)
            {
                return;
            }

            if (goodsPool.Count == 0)
            {
                return;
            }

            int randomIndex = Random.Range(0, goodsPool.Count);
            var selectedGoods = goodsPool[randomIndex];

            if (selectedGoods != null && selectedGoods.item != null)
            {
                goods = selectedGoods;
                price = selectedGoods.price;
                UpdateIcon(selectedGoods.item);
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

            if (Global.Coin.Value >= price)
            {
                Global.SpendCoin(price);

                int count = Mathf.Max(1, goods.count);
                for (int i = 0; i < count; i++)
                {
                    var clone = goods.item.Instantiate()
                        .Position(transform.Position2D() + new Vector2(0, 0.2f));
                    if (clone is IShopInitializable init) init.InitializeFromShop();
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
