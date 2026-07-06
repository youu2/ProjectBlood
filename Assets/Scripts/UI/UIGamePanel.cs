using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	public class UIGamePanelData : UIPanelData
	{
	}
	public partial class UIGamePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			GameUI.ShowGameUI();

			mData = uiData as UIGamePanelData ?? new UIGamePanelData();
			// bind to Global properties
			// update UI when properties change
			Global.currentHP.RegisterWithInitValue(currentHP =>
			{
				HPText.text = "HP: " + Mathf.FloorToInt(currentHP) + "/" + Mathf.FloorToInt(Global.INGAME_MAX_HP.Value);
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.INGAME_MAX_HP.RegisterWithInitValue(maxHP =>
			{
				HPText.text = "HP: " + Mathf.FloorToInt(Global.currentHP.Value) + "/" + Mathf.FloorToInt(maxHP);
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Exp.RegisterWithInitValue(Exp =>
			{
				ExpText.text = "Exp: " + Exp + "/" + Global.MAX_EXP;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Level.RegisterWithInitValue(Level =>
			{
				LevelText.text = "Level: " + Level;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Coin.RegisterWithInitValue(Coin =>
			{
				CoinText.text = Coin.ToString();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Level.Register(Level =>
			{
				Time.timeScale = 0;
				Global.IsGamePaused = true; // 禁用武器操作
				UpgradeRoot.Show();
				// BtnUpgradeDamage.Show();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.RemainingTime.RegisterWithInitValue(Second =>
			{
				TimeText.text = "Wave in: " + Second + "s";
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			// BtnUpgradeDamage.Hide();
			// BtnUpgradeAttackSpeed.Hide();
			UpgradeRoot.Hide();

			BtnUpgradeDamage.onClick.AddListener(() =>
			{
				Time.timeScale = 1;
				Global.IsGamePaused = false; // 重新启用开火
				UpgradeRoot.Hide();
				PlayerUpgrade.UpgradeDamage();
			});

			BtnUpgradeHP.onClick.AddListener(() =>
			{
				Time.timeScale = 1;
				Global.IsGamePaused = false; // 重新启用开火
				UpgradeRoot.Hide();
				PlayerUpgrade.UpgradeHP();
			});

			// display Legacy Point
			Global.LegacyPoint.RegisterWithInitValue(legacy =>
			{
				LegacyText.text = "Lagacy: " + legacy;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
		}
	}
}
