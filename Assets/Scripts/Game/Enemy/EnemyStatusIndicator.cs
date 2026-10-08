// 敌人头顶状态指示器："危"（濒死，袖剑可处决）与"!"（近战攻击前摇）
// 挂在敌人预制体下，带两个默认隐藏的世界空间 TMP 子物体。
// 显隐全部由事件（受击/前摇/死亡）驱动，不做每帧轮询。
using TMPro;
using UnityEngine;

namespace ProjectBlood
{
    public class EnemyStatusIndicator : MonoBehaviour
    {
        [Tooltip("濒死提示文本（默认显示\"危\"），留空时按子物体名 DangerText 自动查找")]
        [SerializeField] private TMP_Text dangerText;

        [Tooltip("近战前摇提示文本（红色\"!\"），留空时按子物体名 WindupText 自动查找")]
        [SerializeField] private TMP_Text windupText;

        private void Awake()
        {
            if (dangerText == null) dangerText = transform.Find("DangerText")?.GetComponent<TMP_Text>();
            if (windupText == null) windupText = transform.Find("WindupText")?.GetComponent<TMP_Text>();
            HideAll();
        }

        public void ShowDanger()
        {
            if (dangerText != null) dangerText.gameObject.SetActive(true);
        }

        public void HideDanger()
        {
            if (dangerText != null) dangerText.gameObject.SetActive(false);
        }

        public void ShowWindup()
        {
            if (windupText != null) windupText.gameObject.SetActive(true);
        }

        public void HideWindup()
        {
            if (windupText != null) windupText.gameObject.SetActive(false);
        }

        public void HideAll()
        {
            HideDanger();
            HideWindup();
        }
    }
}
