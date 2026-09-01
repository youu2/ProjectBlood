using UnityEngine;

[CreateAssetMenu(fileName = "InvincibleEffect", menuName = "技能系统/效果/无敌效果")]
public class InvincibleEffect : SkillEffect
{
    [Header("无敌设置")]
    [Tooltip("无敌时切换到的层名称（需在 Tags & Layers 中定义）")]
    public string invincibleLayerName = "Invincible";

    // 保存原始层，用于恢复
    private int originalLayer;

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
        Debug.Log($"进入无敌状态，层切换到 {invincibleLayerName}");
    }

    public override void OnEnd(EffectContext context)
    {
        GameObject caster = context.caster;
        if (caster == null) return;

        // 恢复原始层
        caster.layer = originalLayer;
        Debug.Log("无敌结束，层已恢复");
    }
}