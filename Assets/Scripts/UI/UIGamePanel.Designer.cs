using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:b694a3ec-b243-45bf-84b7-a8c0a443f73f
	public partial class UIGamePanel
	{
		public const string Name = "UIGamePanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI HPText;
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
		// 升级选项按钮已改为 UIGamePanel.optionCards（3 张动态填充卡片），旧的硬编码按钮字段移除

		private UIGamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			HPText = null;
			ExpText = null;
			LegacyText = null;
			CoinText = null;
			LevelText = null;
			TimeText = null;
			UpgradeRoot = null;

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
