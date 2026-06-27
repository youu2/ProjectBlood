using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace ProjectBlood
{
	public class UIGamePassPanelData : UIPanelData
	{
	}
	public partial class UIGamePassPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIGamePassPanelData ?? new UIGamePassPanelData();

			Time.timeScale = 0;
			Global.IsGamePaused = true;
			ActionKit.OnUpdate.Register(() =>
			{
				if (Input.GetKeyDown(KeyCode.Space))
				{
					this.CloseSelf();
					Global.ResetLevel();
					GameUI.ShowLoadingPage("InGame");
				}
			}).UnRegisterWhenGameObjectDestroyed(gameObject);

			BtnBackHome.onClick.AddListener(() =>
			{
				this.CloseSelf();
				GameUI.ShowLoadingPage("GameStart");
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
