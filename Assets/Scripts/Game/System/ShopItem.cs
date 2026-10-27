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
            // 保证基于玩家最新状态掷点，且上架过程发生在走廊中、玩家不可见。
            // 读档时 RestoreGoods 可能先于 Start 执行（房间实例化在 MapController.Start 内完成），
            // 这里必须走统一同步，不能无条件隐藏，否则会把已还原的商品图标/价格重新盖掉
            SyncVisual();
        }

        // 视觉状态唯一同步入口：显隐完全由 goods / isCollected 推导。
        // Start、RefreshGoods、RestoreGoods、购买后统一调用，避免多条时序路径各写一遍导致状态撕裂
        private void SyncVisual()
        {
            if (isCollected || goods == null || goods.item == null)
            {
                Icon.Hide();
                PriceText.Hide();
                return;
            }

            Icon.Show();
            PriceText.text = price.ToString();
            PriceText.Show();
        }

        // 存档：返回当前选中商品在 goodsPool 中的索引（未刷新返回 -1）
        public int GetSelectedGoodsIndex()
        {
            if (goods == null) return -1;
            return goodsPool.IndexOf(goods);
        }

        // 读档：按索引直接还原商品，跳过随机掷点，保证存档前后货架商品一致
        public void RestoreGoods(int index)
        {
            if (goods != null || isCollected)
            {
                SyncVisual(); // 幂等：已还原/已购买时只补一次视觉同步
                return;
            }
            if (index < 0 || index >= goodsPool.Count) return; // 越界（prefab 变更）留空，待进房掷点
            var g = goodsPool[index];
            if (g == null || g.item == null) return;
            goods = g;
            price = g.price;
            UpdateIcon(g.item);
            SyncVisual();
        }

        // 幂等刷新：已购买则跳过；已生成则不再重掷，仅补视觉同步
        //（读档时 RestoreGoods 先于 Start 执行，若这里提前 return 什么都不做，
        //  Start 的隐藏/显示时序问题就没有自愈路径）
        // takenKeys：本轮刷新内已被其他货架抽中的唯一商品 key（跨货架去重）
        public void RefreshGoods(HashSet<string> takenKeys = null)
        {
            if (isCollected) return; // 已购买：保持售罄状态

            if (goods == null)
            {
                InitGoodsFromPool(takenKeys);
                if (goods == null)
                {
                    SyncVisual(); // 空店：保持空货架，CanInteract 恒 false
                    return;
                }
                var unique = goods.item.GetComponent<IUniqueShopGoods>();
                if (unique != null) takenKeys?.Add(unique.UniqueKey);
            }

            SyncVisual();
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

                isCollected = true;
                SyncVisual(); // 售罄：与 Start/读档共用同一套显隐规则
                Tips.Hide();

                AudioKitManager.Instance.PlayOneShot("BuySound", volume: 0.8f);
            }
            else
            {
                Player.DisplayText("Too expensive...");
            }
        }
    }
}
