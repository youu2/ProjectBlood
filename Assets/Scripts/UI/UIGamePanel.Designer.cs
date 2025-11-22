using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:8bdc5c7f-2e3f-42fb-afa3-852607e9b716
	public partial class UIGamePanel
	{
		public const string Name = "UIGamePanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI ExpText;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public TMPro.TextMeshProUGUI TimeText;
		[SerializeField]
		public RectTransform UpgradeRoot;
		[SerializeField]
		public UnityEngine.UI.Button BtnUpgradeDamage;
		[SerializeField]
		public UnityEngine.UI.Button BtnUpgradeAttackSpeed;
		
		private UIGamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ExpText = null;
			LevelText = null;
			TimeText = null;
			UpgradeRoot = null;
			BtnUpgradeDamage = null;
			BtnUpgradeAttackSpeed = null;
			
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
