using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:1f997979-8211-45d3-9979-8cbaac7870ab
	public partial class UIGameStartPanel
	{
		public const string Name = "UIGameStartPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BtnStartGame;
		[SerializeField]
		public UnityEngine.UI.Button BtnLegacyUpgrade;
		[SerializeField]
		public UnityEngine.UI.Image LegacyUpgradePanel;
		[SerializeField]
		public UnityEngine.UI.Button BtnCoinDropRateUpgrade;
		[SerializeField]
		public UnityEngine.UI.Button BtnCloseUpgradePage;
		[SerializeField]
		public TMPro.TextMeshProUGUI LegacyHeldText;
		[SerializeField]
		public TMPro.TextMeshProUGUI TittleText;
		
		private UIGameStartPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BtnStartGame = null;
			BtnLegacyUpgrade = null;
			LegacyUpgradePanel = null;
			BtnCoinDropRateUpgrade = null;
			BtnCloseUpgradePage = null;
			LegacyHeldText = null;
			TittleText = null;
			
			mData = null;
		}
		
		public UIGameStartPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIGameStartPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIGameStartPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
