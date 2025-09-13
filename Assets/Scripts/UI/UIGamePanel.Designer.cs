using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:cf5abf12-2685-48b3-a1e4-e2ac2906966e
	public partial class UIGamePanel
	{
		public const string Name = "UIGamePanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI ExpText;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public UnityEngine.UI.Button BtnUpgrade;
		
		private UIGamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ExpText = null;
			LevelText = null;
			BtnUpgrade = null;
			
			mData = null;
		}
		
		public UIGamePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIGamePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIGamePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
