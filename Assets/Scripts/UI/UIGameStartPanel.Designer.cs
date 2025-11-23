using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:a29aff92-553f-4bf2-817d-64397b6f7e27
	public partial class UIGameStartPanel
	{
		public const string Name = "UIGameStartPanel";
		
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
		
		private UIGameStartPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BtnLegacyUpgrade = null;
			LegacyUpgradePanel = null;
			BtnCoinDropRateUpgrade = null;
			BtnCloseUpgradePage = null;
			LegacyHeldText = null;
			
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
