using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectBlood
{
    /// <summary>
    /// 暂停页血印槽位的悬停交互：
    /// 鼠标移入时在详情文本框显示"血印名字 + 换行 + 效果描述"，移出时隐藏详情。
    /// 由 PausePageController 在打开暂停页时按已解锁血印动态挂载。
    /// </summary>
    public class PauseSigilSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BloodSigilSO sigilData;       // 本槽位对应的血印配置
        private TextMeshProUGUI detailText;   // 血印详情文本框

        public void Initialize(BloodSigilSO sigil, TextMeshProUGUI detailLabel)
        {
            sigilData = sigil;
            detailText = detailLabel;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (sigilData == null || detailText == null) return;

            detailText.text = sigilData.sigilName + "\n\n" + sigilData.description;
            detailText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (detailText != null)
            {
                detailText.gameObject.SetActive(false);
            }
        }
    }
}
