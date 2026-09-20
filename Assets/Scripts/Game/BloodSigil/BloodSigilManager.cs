using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 血印池管理器（MonoBehaviour 单例，对标 UpgradeManager）。
    // 职责：持有所有血印配置资产，提供加权随机抽取（排除已解锁/非法）与解锁入口。
    // 挂载方式：在场景中创建空物体，把创建好的 BloodSigilSO 资产拖入 Sigil Pool 列表。
    // 状态层（已解锁集合/上下文）全部在静态 BloodSigilState 中，本类仅做池配置与入口。
    public class BloodSigilManager : MonoBehaviour
    {
        public static BloodSigilManager Instance { get; private set; }

        [Tooltip("所有血印配置资产，随机抽取时自动过滤已解锁/不可用项")]
        [SerializeField] private List<BloodSigilSO> sigilPool = new List<BloodSigilSO>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 局外养成：游戏开始时按养成等级解锁随机不重复血印（代次防护保证每局只应用一次）
            BloodSigilState.ApplyGlobalRandomSigils();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 从池中按 dropWeight 加权随机抽取一个未解锁的血印。
        // 过滤：非空、isInPool、未解锁。池空时返回 null（调用方回落旧掉落）。
        public BloodSigilSO GetRandomSigil()
        {
            var available = new List<BloodSigilSO>();
            int totalWeight = 0;
            foreach (var sigil in sigilPool)
            {
                if (sigil == null || !sigil.isInPool) continue;
                if (BloodSigilState.IsUnlocked(sigil)) continue;
                if (sigil.dropWeight <= 0) continue;
                available.Add(sigil);
                totalWeight += sigil.dropWeight;
            }

            if (available.Count == 0 || totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var sigil in available)
            {
                cumulative += sigil.dropWeight;
                if (roll < cumulative) return sigil;
            }
            return available[available.Count - 1];
        }

        // 解锁入口：转调 BloodSigilState，重复时安全失败
        public void Unlock(BloodSigilSO so)
        {
            BloodSigilState.Unlock(so);
        }
    }
}
