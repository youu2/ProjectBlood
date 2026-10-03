using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectBlood
{
    public class UIGameOverPanelData : UIPanelData
    {
    }
    public partial class UIGameOverPanel : UIPanel
    {
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UIGameOverPanelData ?? new UIGameOverPanelData();
            // please add init code here
            // 面板先被无父实例化(世界旋转=0)，再 SetParent(Common)（默认
            // worldPositionStays=true）：在相机倾斜期间打开时，本地旋转会被烤成
            // Common 世界旋转的反号（实测≈1.728°），导致与本地旋转始终为 0 的
            // 暂停页表现不一致。框架的 SetDefaultSizeOfPanel 不复位旋转，这里复位
            transform.localRotation = Quaternion.identity;
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

        private void Update()
        {

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
