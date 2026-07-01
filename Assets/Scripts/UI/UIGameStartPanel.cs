using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace ProjectBlood
{
	public class UIGameStartPanelData : UIPanelData
	{
	}
	public partial class UIGameStartPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIGameStartPanelData ?? new UIGameStartPanelData();
			Time.timeScale = 0;
			Global.IsGamePaused = true;
			// Load Legacy Point from PlayerPrefs
			Global.LegacyPoint.Value = PlayerPrefs.GetInt("LegacyPoint", 0);
			// Register change callback
			Global.LegacyPoint.RegisterWithInitValue(legacy =>
			{
				PlayerPrefs.SetInt("LegacyPoint", legacy);
				LegacyHeldText.text = "Lagacy: " + legacy;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			BtnStartGame.onClick.AddListener(() =>
			{
				this.CloseSelf();
				Global.ResetLevel();
				Time.timeScale = 1.0f;
				GameUI.ShowLoadingPage("InGame");
			});

			BtnLegacyUpgrade.onClick.AddListener(() =>
			{
				LegacyUpgradePanel.Show();
				TittleText.Hide();
			});
			BtnCloseUpgradePage.onClick.AddListener(() =>
			{
				LegacyUpgradePanel.Hide();
				TittleText.Show();
			});
			BtnCoinDropRateUpgrade.onClick.AddListener(() =>
			{
				if (Global.LegacyPoint.Value >= 5)
				{
					Global.LegacyPoint.Value -= 5;
					Global.CoinDropRate.Value += 0.05f;
					Debug.Log("Coin Drop Rate upgraded to " + Global.CoinDropRate.Value);
				}
				else
				{
					Debug.Log("Not enough Legacy Point! Your Current Drop Rate: " + Global.CoinDropRate.Value);
					return;
				}
			});

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
			Time.timeScale = 1;
			Global.IsGamePaused = false;
		}
	}
}
