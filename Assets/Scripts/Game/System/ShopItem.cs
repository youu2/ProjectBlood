using UnityEngine;
using QFramework;

namespace ProjectBlood
{
	public partial class ShopItem : ViewController
	{
		public DropItem goods;
		public int price;
		private bool saleOut = false;
		public ShopItem(DropItem goods, int price)
		{
			this.goods = goods;
			this.price = price;
		}
		public void Awake()
		{
			price = 1;
			PriceText.text = price.ToString();
			PriceText.Show();
		}
		private void Update()
		{
			if (Tips.gameObject.activeSelf)
			{
				if (Input.GetKeyDown(KeyCode.F))
				{
					if(Global.Coin.Value >= price)
					{
						Global.SpendCoin(1);
						goods.Instantiate()
							.Position(transform.Position2D() + new Vector2(0, 0.2f))
							.Show();
						Icon.Hide();
						PriceText.Hide();
						Tips.Hide();
						saleOut = true;
						AudioKitManager.Instance.PlayOneShot("BuySound", volume: 0.8f);
					}else
					{
						Player.DisplayText("Too expensive...");
					}
					
				}
			}
		}
		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.CompareTag("Player") && !saleOut)
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
