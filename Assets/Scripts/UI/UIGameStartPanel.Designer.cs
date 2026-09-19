using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

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
        public UnityEngine.RectTransform LegacyUpgradePanel;
        [SerializeField]
        public UnityEngine.RectTransform UpgradeEntryTemplate;
        [SerializeField]
        public UnityEngine.RectTransform UpgradeEntryContainer;
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
            UpgradeEntryTemplate = null;
            UpgradeEntryContainer = null;
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
