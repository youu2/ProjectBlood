using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace ProjectBlood
{
	public partial class ShopItem : ViewController
	{
		[Header("商品池")]
		public List<DropItem> goodsPool = new List<DropItem>();

		protected DropItem goods;
		protected int price;
		private bool saleOut = false;

		public ShopItem(DropItem goods, int price)
		{
			this.goods = goods;
			this.price = price;
		}

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
			
			if (selectedGoods != null)
			{
				goods = selectedGoods;
				price = selectedGoods.price;
				UpdateIcon(selectedGoods);
			}
		}

		private void UpdateIcon(DropItem item)
		{
			var spriteRenderer = item.GetComponentInChildren<SpriteRenderer>();
			if (spriteRenderer != null)
			{
				Icon.sprite = spriteRenderer.sprite;
			}
		}

		private void Update()
		{
			if (!Tips.gameObject.activeSelf)
			{
				return;
			}

			if (!Input.GetKeyDown(KeyCode.F))
			{
				return;
			}

			if (goods == null)
			{
				return;
			}

			if (Global.Coin.Value >= price)
			{
				Global.SpendCoin(price);
				goods.Instantiate()
					.Position(transform.Position2D() + new Vector2(0, 0.2f))
					.Show();
				
				Icon.Hide();
				PriceText.Hide();
				Tips.Hide();
				saleOut = true;
				
				AudioKitManager.Instance.PlayOneShot("BuySound", volume: 0.8f);
			}
			else
			{
				Player.DisplayText("Too expensive...");
			}
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.CompareTag("Player") && !saleOut && goods != null)
			{
				Tips.Show();
			}
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			if (other.CompareTag("Player"))
			{
				Tips.Hide();
			}
		}
	}
}
