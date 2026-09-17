using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印触发条件配置（四要素之一）。
    // 采用"枚举 + 基础参数字段"的单 SO 方案：所有触发类型用同一结构表达，
    // 无多态序列化，天然可由 JSON/表格行直接映射（triggerType + skillNames）。
    // 检测由 BloodSigilState 统一完成，本类只负责 Match 判断，不持有运行时状态。
    [CreateAssetMenu(fileName = "SigilTrigger_", menuName = "血印系统/触发条件", order = 10)]
    public class BloodSigilTriggerSO : ScriptableObject
    {
        [Tooltip("触发条件类型")]
        public BloodSigilTriggerType triggerType = BloodSigilTriggerType.OnAcquire;

        [Tooltip("技能名过滤（仅 SkillCast 类型生效，与 SkillData.skillName 完全一致）；留空表示任意技能均可触发")]
        public List<string> skillNames = new List<string>();

        // 判断一次游戏事件是否满足本触发条件
        public bool Matches(BloodSigilFireContext ctx)
        {
            if (ctx.TriggerType != triggerType) return false;

            if (triggerType == BloodSigilTriggerType.SkillCast)
            {
                // 空列表 = 不限制技能；否则必须命中列表
                if (skillNames != null && skillNames.Count > 0)
                    return skillNames.Contains(ctx.SkillName);
            }
            return true;
        }
    }
}
