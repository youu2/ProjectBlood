using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace ProjectBlood
{
	// Generate Id:14a12728-8233-4561-b24f-68aef1310f42
	public partial class UIGamePassPanel
	{
		public const string Name = "UIGamePassPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BtnBackHome;
		
		private UIGamePassPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BtnBackHome = null;
			
			mData = null;
		}
		
		public UIGamePassPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UIGamePassPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIGamePassPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
