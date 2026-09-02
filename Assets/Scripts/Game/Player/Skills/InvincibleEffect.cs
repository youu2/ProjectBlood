using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    [CreateAssetMenu(fileName = "InvincibleEffect", menuName = "技能系统/效果/无敌效果")]
    public class InvincibleEffect : SkillEffect
    {
        [Header("无敌设置")]
        [Tooltip("无敌时切换到的层名称（需在 Tags & Layers 中定义）")]
        public string invincibleLayerName = "Invincible";
        List<(GameObject, int)> layersToChange = new List<(GameObject, int)>();

        // 保存原始层，用于恢复
        private int originalLayer;

        // 内部计时器
        private float elapsedTime = 0f;
        private bool isActive = false;

        // 重写 IsDone，表示这是一个持续效果
        public override bool IsDone => !isActive;

        public override void OnStart(EffectContext context)
        {
            GameObject caster = context.caster;
            if (caster == null) return;

            // 保存原始层
            originalLayer = caster.layer;

            // 切换到无敌层
            int invincibleLayer = LayerMask.NameToLayer(invincibleLayerName);
            if (invincibleLayer == -1)
            {
                Debug.LogError($"未找到名为 '{invincibleLayerName}' 的层，请先在 Tags & Layers 中创建！");
                return;
            }

            caster.layer = invincibleLayer;
            // SO 资产的列表会跨使用/跨场景持久化，必须先清空避免残留已销毁的引用
            layersToChange.Clear();
            foreach (Transform child in caster.transform)
            {
                if (child.GetComponent<Collider2D>() != null)
                    layersToChange.Add((child.gameObject, child.gameObject.layer));
            }
            foreach ((GameObject, int) child in layersToChange)
            {
                child.Item1.layer = invincibleLayer;
            }

            isActive = true;
            elapsedTime = 0f;
            Debug.Log($"进入无敌状态，层切换到 {invincibleLayerName}");
        }

        public override void OnUpdate(EffectContext context)
        {
            if (!isActive) return;

            // 累计时间
            elapsedTime += Time.deltaTime;

            // 当持续时间达到技能总时长时结束
            // context.duration 是技能总持续时间，由 SkillData 提供
            if (context.duration > 0f && elapsedTime >= context.duration)
            {
                EndInvincibility(context);
            }
        }

        public override void OnEnd(EffectContext context)
        {
            // 无论如何都恢复层
            EndInvincibility(context);
        }

        // 结束无敌并恢复层
        private void EndInvincibility(EffectContext context)
        {
            if (!isActive) return;

            GameObject caster = context.caster;
            if (caster != null)
            {
                caster.layer = originalLayer;
                foreach ((GameObject, int) child in layersToChange)
                {
                    // 子物体可能在无敌期间被销毁（如武器切换），跳过已销毁的对象
                    if (child.Item1 != null)
                        child.Item1.layer = child.Item2;
                }
            }

            isActive = false;
            Debug.Log("无敌结束，层已恢复");
        }
    }
}