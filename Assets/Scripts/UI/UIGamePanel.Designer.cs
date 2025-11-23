using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:559eca44-c421-4025-b88b-127e4bb734b2
	public partial class UIGamePanel
	{
		public const string Name = "UIGamePanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI ExpText;
		[SerializeField]
		public TMPro.TextMeshProUGUI LegacyText;
		[SerializeField]
		public TMPro.TextMeshProUGUI CoinText;
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
			LegacyText = null;
			CoinText = null;
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
