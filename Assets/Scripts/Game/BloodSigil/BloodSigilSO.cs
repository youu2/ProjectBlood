using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印稀有度：仅作掉落分组、权重与配色参考，不影响效果逻辑本身
    public enum BloodSigilRarity
    {
        Common,
        Rare,
        Epic,
        Cursed, // 带代价或负面效果的血印
    }

    // 血印配置资产（数据驱动，对标 UpgradeSO / SkillData）。
    // 通过 Create > 血印系统 > 血印配置 创建：配置名称/描述/图标/掉落权重/可移除，
    // 并挂入若干"效果模块(BloodSigilEffect)"，每个模块由
    // 触发条件 + 结算效果(可多个) + 结束条件(可多个) + 可生效次数 四要素组合而成。
    // 完成后拖入 BloodSigilManager 的血印池列表。
    [CreateAssetMenu(fileName = "Sigil_", menuName = "血印系统/血印配置", order = 0)]
    public class BloodSigilSO : ScriptableObject
    {
        [Header("显示信息")]
        public string sigilName = "新血印";
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("存档")]
        [Tooltip("存档匹配键，创建后不可修改；留空时回退使用资产名")]
        public string id;

        [Header("掉落配置")]
        [Tooltip("是否进入随机抽取池，置 false 可临时禁用而不删除资产")]
        public bool isInPool = true;

        [Tooltip("稀有度（仅作分组/配色用）")]
        public BloodSigilRarity rarity = BloodSigilRarity.Common;

        [Tooltip("加权随机权重（越大越容易被抽到）")]
        [Min(0)] public int dropWeight = 10;

        [Header("规则")]
        [Tooltip("能否被献祭类血印移除；献祭血印本身应设为 false 以免误计")]
        public bool removable = true;

        [Header("效果模块（每个模块=触发条件+结算效果+结束条件+生效次数，可挂多个）")]
        public List<BloodSigilEffect> effects = new List<BloodSigilEffect>();

#if UNITY_EDITOR
        // 编辑期校验：空效果列表、空元素给出警告（仿 UpgradeSO.OnValidate）
        private void OnValidate()
        {
            if (effects.Count == 0)
            {
                Debug.LogWarning($"[BloodSigilSO] {name} 未配置任何效果", this);
                return;
            }
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                    Debug.LogWarning($"[BloodSigilSO] {name} 第 {i} 个效果为空", this);
            }
        }
#endif
    }
}
