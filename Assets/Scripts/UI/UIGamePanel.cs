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
				BtnUpgrade.Show();
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			BtnUpgrade.Hide();

			BtnUpgrade.onClick.AddListener(() =>
			{
				Time.timeScale = 1;
				BtnUpgrade.Hide();
				Global.BlazingCircleDamage.Value += 20;
				//BlazingCircle.upgrade();
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
		}
	}
}
