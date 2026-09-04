using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectBlood
{
    // 升级池管理器：维护所有可抽取的强化项，提供随机抽取与应用入口。
    // 挂载方式：在游戏场景中创建空物体（或挂到持久存在的 GameUI 物体上），
    //          把创建好的 UpgradeSO 资产拖入 Upgrade Pool 列表。
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Tooltip("所有强化项配置资产，随机抽取时会自动过滤掉不可用项")]
        [SerializeField] private List<UpgradeSO> upgradePool = new List<UpgradeSO>();

        [Tooltip("每次升级默认抽取数量(UI 卡片数)")]
        [SerializeField] private int defaultDrawCount = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 从池中随机抽取 count 个可用强化。
        // 过滤条件：非空、isInPool 为 true、效果当前可用（武器未拥有/已满级、属性已满 5 次、被动已解锁等均会被排除）。
        // 可用项不足 count 时返回全部可用项；一个都没有时返回空列表（UI 层据此直接恢复游戏）。
        public List<UpgradeSO> GetRandomUpgrades(int count)
        {
            var available = upgradePool
                .Where(so => so != null && so.isInPool && so.effect != null && so.effect.IsAvailable())
                .ToList();

            // Fisher-Yates 洗牌，保证每次升级展示顺序随机
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            if (available.Count > count)
            {
                available = available.GetRange(0, count);
            }
            return available;
        }

        public List<UpgradeSO> GetRandomUpgrades()
        {
            return GetRandomUpgrades(defaultDrawCount);
        }

        // 应用选中的强化：按效果大类分发到 PlayerUpgradeState，武器满级时触发进化
        public void ApplyUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null || upgrade.effect == null) return;

            var effect = upgrade.effect;
            switch (effect.effectType)
            {
                case UpgradeEffectType.BaseStat:
                    PlayerUpgradeState.ApplyStat(effect.statType, effect.statValue);
                    break;

                case UpgradeEffectType.WeaponDamage:
                    bool reachedMaxLevel = PlayerUpgradeState.ApplyWeaponDamage(effect.weaponType, effect.damageBonusPerStack);
                    if (reachedMaxLevel)
                    {
                        TryEvolve(effect.weaponType);
                    }
                    break;

                case UpgradeEffectType.WeaponAmmo:
                    PlayerUpgradeState.ApplyWeaponAmmo(effect.ammoWeaponType, effect.ammoBonusPerStack);
                    break;

                case UpgradeEffectType.Passive:
                    PlayerUpgradeState.UnlockPassive(effect.passiveType);
                    break;
            }
        }

        // 查找目标武器上是否实现了 IWeaponEvolution，有则触发进化
        private void TryEvolve(WeaponType weaponType)
        {
            if (Player.player1 == null) return;

            var weapon = Player.player1.GetWeapon(weaponType);
            if (weapon is IWeaponEvolution evolution && !evolution.IsEvolved)
            {
                evolution.Evolve();
            }
        }
    }
}
