using UnityEngine;

namespace ProjectBlood
{
    [CreateAssetMenu(fileName = "PlayAnimationEffect", menuName = "技能系统/效果/播放动画效果")]
    public class PlayAnimationEffect : SkillEffect
    {
        [Header("动画设置")]
        [Tooltip("要设置的动画参数名（Trigger 或 Bool）")]
        public string parameterName = "Roll";

        [Tooltip("参数类型")]
        public AnimatorControllerParameterType parameterType = AnimatorControllerParameterType.Trigger;

        [Tooltip("如果是 Bool 类型，设置的值")]
        public bool boolValue = true;

        private Animator animator;

        public override void OnStart(EffectContext context)
        {
            animator = Player.player1.PlayerAnimator;
            if (animator == null)
            {
                Debug.LogWarning("施法者没有 Animator 组件");
                return;
            }

            // 添加日志
            Debug.Log($"播放动画效果，参数名: {parameterName}, 类型: {parameterType}");

            switch (parameterType)
            {
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(parameterName);
                    Debug.Log($"触发器已设置: {parameterName}");
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(parameterName, boolValue);
                    Debug.Log($"Bool 已设置: {parameterName} = {boolValue}");
                    break;
            }
        }

        public override void OnEnd(EffectContext context)
        {
            if (animator != null && parameterType == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, !boolValue);
            }
        }
    }
}