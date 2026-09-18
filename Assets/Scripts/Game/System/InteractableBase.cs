using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    /// 可交互物契约：靠近显示提示，按 F 触发交互
    public interface IInteractable
    {
        bool CanInteract { get; }
        void OnInteract();
    }

    /// 可交互物基类：统一处理 Tips 显隐、F 键派发与防重入，子类只需实现 DoInteract。
    /// 现有实现：Chest、ShopItem、BloodSigilDrop
    public abstract class InteractableBase : ViewController, IInteractable
    {
        public TMPro.TextMeshProUGUI Tips;

        protected bool isCollected;

        public virtual bool CanInteract => !isCollected;

        // 子类实现具体交互逻辑（基类已负责防重入与按键检测）
        protected abstract void DoInteract();

        // 履行 IInteractable 契约：外部可统一通过接口调用
        public void OnInteract()
        {
            if (!CanInteract) return;
            DoInteract();
        }

        // 进入/离开范围钩子，供子类追加自定义 UI 显隐
        protected virtual void OnEnterRange() { }
        protected virtual void OnExitRange() { }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || !CanInteract) return;
            Tips.Show();
            OnEnterRange();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            Tips.Hide();
            OnExitRange();
        }

        private void Update()
        {
            if (!Tips.gameObject.activeSelf) return;
            if (!Input.GetKeyDown(KeyCode.F)) return;
            OnInteract();
        }
    }
}
