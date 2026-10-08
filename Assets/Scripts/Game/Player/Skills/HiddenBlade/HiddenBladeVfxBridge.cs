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

        /// <summary>按目标类型触发对应刺击动画。</summary>
        public void TriggerStab(bool isExecutionTarget)
        {
            if (vfxAnimator == null) return;
            vfxAnimator.SetTrigger(isExecutionTarget ? executionTriggerName : stabTriggerName);
        }

        /// <summary>预留：动画命中帧事件的接收入口（方法名与 Animation Event 中填写一致）。</summary>
        public void AnimationEvent_StabHit()
        {
            OnStabHit?.Invoke();
        }
    }
}
