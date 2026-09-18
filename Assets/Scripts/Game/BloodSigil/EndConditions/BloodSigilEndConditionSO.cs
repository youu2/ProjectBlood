using UnityEngine;

namespace ProjectBlood
{
    // 血印结束条件配置（四要素之一）。枚举驱动：
    //   duration        仅 Duration 类型生效；
    //   healthCompare / healthThreshold 仅 HealthThreshold 类型生效，
    //   比较运算符与判定逻辑与触发条件共用 BloodSigilHealthThreshold。
    // 一个模块可挂多个结束条件，由模块上的 EndMatchMode 决定"满足其一(Any)"还是"全部满足(All)"。
    [CreateAssetMenu(fileName = "SigilEnd_", menuName = "血印系统/结束条件", order = 11)]
    public class BloodSigilEndConditionSO : ScriptableObject
    {
        [Tooltip("结束条件类型")]
        public BloodSigilEndConditionType endConditionType = BloodSigilEndConditionType.Unlimited;

        [Tooltip("持续秒数（仅 Duration 类型生效）")]
        [Min(0f)] public float duration = 1f;

        [Tooltip("血量比较运算符（仅 HealthThreshold 类型生效）")]
        public BloodSigilHealthCompare healthCompare = BloodSigilHealthCompare.LessThan;

        [Tooltip("血量百分比阈值（仅 HealthThreshold 类型生效，0~1，0.3=30%，1=满血）")]
        [Range(0f, 1f)] public float healthThreshold = 0.3f;
    }

    // 多结束条件的匹配模式
    public enum BloodSigilEndMatchMode
    {
        // 满足任一结束条件即结束
        Any = 0,
        // 必须全部结束条件同时满足才结束
        All = 1,
    }
}
