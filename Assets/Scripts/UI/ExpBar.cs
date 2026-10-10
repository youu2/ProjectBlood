// 经验条管理：数据源 Global.Exp / Global.MAX_EXP / Global.Level
// 获取经验时遮罩从左向右增长（fillAmount = 当前经验 / 升级所需经验），
// 升级后遮罩重置为 0。
// 用法：挂载到经验条节点，在 Inspector 中把经验条 Image 拖到 expBarImage。
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    public class ExpBar : MonoBehaviour
    {
        [Header("UI 引用")]
        [Tooltip("经验条填充 Image。运行时会自动确保 Image Type = Filled、Fill Method = Horizontal、Origin = Left（从左往右填充）")]
        public Image expBarImage;

        // 订阅放在 Awake，与血条一致：节点初始状态可能被禁用，Awake 仍会执行
        private void Awake()
        {
            EnsureFilledImage();

            // 当前经验变化：按"当前等级内经验占比"刷新遮罩，从左向右增长
            Global.Exp.RegisterWithInitValue(exp => RefreshFill())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 升级所需经验会随等级变化（Global.AddExp），存档导入时也会恢复，需同步刷新比例
            Global.MAX_EXP.RegisterWithInitValue(maxExp => RefreshFill())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 升级瞬间遮罩重置为 0。
            // AddExp 中的事件顺序为 Level++（重置 0）→ Exp 扣除 → MAX_EXP 重算；
            // 每颗经验球固定 +1，扣除后 Exp 必为 0，最终遮罩稳定在 0
            Global.Level.RegisterWithInitValue(level => ResetFill())
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        // 确保 Image 是横向 Filled 类型，否则 fillAmount 不会产生遮罩效果
        private void EnsureFilledImage()
        {
            if (expBarImage == null)
            {
                Debug.LogWarning("ExpBar 未在 Inspector 中配置经验条 Image（expBarImage），经验条不会显示");
                return;
            }

            expBarImage.type = Image.Type.Filled;
            expBarImage.fillMethod = Image.FillMethod.Horizontal;
            expBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            expBarImage.fillAmount = 0f;
        }

        // 当前经验占比 → fillAmount（含除零保护，钳制 0~1）
        private void RefreshFill()
        {
            if (expBarImage == null) return;

            float maxExp = Global.MAX_EXP.Value;
            float fill = maxExp > 0f
                ? Mathf.Clamp01((float)Global.Exp.Value / maxExp)
                : 0f;
            expBarImage.fillAmount = fill;
        }

        // 升级后遮罩重置为 0
        private void ResetFill()
        {
            if (expBarImage == null) return;
            expBarImage.fillAmount = 0f;
        }
    }
}
