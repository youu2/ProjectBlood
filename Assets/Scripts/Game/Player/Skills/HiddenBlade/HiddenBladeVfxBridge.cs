using System;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 袖剑刺击特效桥接组件：必须与刺击 Animator 挂在同一个 GameObject 上
    /// （Unity 动画事件只能发给 Animator 所在物体）。
    /// 第一版伤害由处决效果按真实时间计时触发，本组件只负责播放 trigger；
    /// 同时预留动画事件转发入口：未来若改用"动画事件卡命中帧"，
    /// 在动画帧上调用 AnimationEvent_StabHit()，订阅此 C# 事件即可。
    /// 注意：Animator 的 Update Mode 需设为 Unscaled Time。
    /// </summary>
    public class HiddenBladeVfxBridge : MonoBehaviour
    {
        [Tooltip("刺击特效 Animator（留空自动取本物体上的 Animator）")]
        [SerializeField] private Animator vfxAnimator;

        [Tooltip("处决目标（濒死）播放的 trigger 名")]
        [SerializeField] private string executionTriggerName = "Execution";

        [Tooltip("普通刺击目标播放的 trigger 名")]
        [SerializeField] private string stabTriggerName = "Stab";

        /// <summary>未来动画事件驱动伤害时订阅（事件参数：是否处决目标无法从动画事件取得，另行约定）。</summary>
        public event Action OnStabHit;

        private void Awake()
        {
            if (vfxAnimator == null) vfxAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// 按目标类型触发对应刺击动画，并把特效物体一次性转向目标（世界空间）。
        /// 仅在命中帧调用一次：处决期间输入锁定、玩家朝向冻结，无需每帧追踪。
        /// </summary>
        /// <param name="isExecutionTarget">是否为濒死处决目标（决定播哪个 trigger）</param>
        /// <param name="worldDirection">施法者→目标的世界空间方向（零向量时保持当前朝向）</param>
        public void TriggerStab(bool isExecutionTarget, Vector2 worldDirection)
        {
            FaceTarget(worldDirection);
            if (vfxAnimator == null) return;
            vfxAnimator.SetTrigger(isExecutionTarget ? executionTriggerName : stabTriggerName);
        }

        // 与武器瞄准同一套算法：Atan2 角度 → Z 轴旋转。
        // 改为直接计算 localRotation：玩家 flipX（localScale.x = -1）时，
        // Unity 反算 localRotation 的公式在某些角度边界不精确，
        // 直接给 localRotation 赋值可以完全控制视觉朝向。
        private void FaceTarget(Vector2 worldDirection)
        {
            if (worldDirection.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;

            // 检测玩家是否处于 flipX 状态（父级或自身的 x 缩放为负）
            bool flipped = Mathf.Sign(transform.lossyScale.x) < 0f;
            if (flipped)
            {
                // 镜像后视觉角度 = 180° - 原角度（绕 Y 轴翻转）
                angle = 180f - angle;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>预留：动画命中帧事件的接收入口（方法名与 Animation Event 中填写一致）。</summary>
        public void AnimationEvent_StabHit()
        {
            OnStabHit?.Invoke();
        }
    }
}