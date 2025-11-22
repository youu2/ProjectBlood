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
			mData = uiData as UIGamePanelData ?? new UIGamePanelData();
			// please add init code here

			Global.Exp.RegisterWithInitValue(Exp =>
			{
				ExpText.text = "Exp: " + Exp;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Level.RegisterWithInitValue(Level =>
			{
				LevelText.text = "Level: " + Level;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.Level.Register(Level =>
			{
				Time.timeScale = 0;
				UpgradeRoot.Show();
				// BtnUpgradeDamage.Show();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			Global.RemainingTime.RegisterWithInitValue(Second =>
			{
				TimeText.text = "Countdown: " + Second;
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			// BtnUpgradeDamage.Hide();
			// BtnUpgradeAttackSpeed.Hide();
			UpgradeRoot.Hide();

			BtnUpgradeDamage.onClick.AddListener(() =>
			{
				Time.timeScale = 1;
				// BtnUpgradeDamage.Hide();
				UpgradeRoot.Hide();
				Global.BlazingCircleDamage.Value *= 1.2f;
				//BlazingCircle.upgrade();
			});

			BtnUpgradeAttackSpeed.onClick.AddListener(() =>
			{
				Time.timeScale = 1;
				// BtnUpgradeAttackSpeed.Hide();
				UpgradeRoot.Hide();
				Global.BCAttackInterval.Value *= 0.91f;
			});

			// ActionKit.OnUpdate.Register(() =>
			// {
			// 	Global.RemainingTime.Value -= Time.deltaTime;
			// }).UnRegisterWhenGameObjectDestroyed(gameObject);

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
