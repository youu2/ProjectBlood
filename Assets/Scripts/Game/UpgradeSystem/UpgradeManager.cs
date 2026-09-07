using System.Collections.Generic;
using UnityEngine;

namespace ProjectBlood
{
    // 升级池管理器：维护所有可抽取的强化项,提供随机抽取与应用入口。
    // 挂载方式：在游戏场景中创建空物体(或挂到持久存在的 GameUI 物体上),
    //          把创建好的 UpgradeSO 资产拖入 Upgrade Pool 列表。
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Tooltip("所有强化项配置资产,随机抽取时会自动过滤掉不可用项")]
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
        // 过滤条件：非空、isInPool 为 true、配置校验通过(无效/冲突配置不进入池)、效果当前可用
        //(组合效果全有或全无：任一条目不可用即整体排除,如武器未拥有/已满级、属性已满 5 次、被动已解锁)。
        // 可用项不足 count 时返回全部可用项；一个都没有时返回空列表(UI 层据此直接恢复游戏)。
        public List<UpgradeSO> GetRandomUpgrades(int count)
        {
            var available = new List<UpgradeSO>();
            foreach (var upgrade in upgradePool)
            {
                if (upgrade == null || !upgrade.isInPool || upgrade.effect == null) continue;

                var errors = new List<string>();
                if (!upgrade.effect.Validate(errors))
                {
                    Debug.LogWarning($"[UpgradeManager] 强化资产 {upgrade.name} 配置无效,已从池中排除：{string.Join("；", errors)}", upgrade);
                    continue;
                }
                if (!upgrade.effect.IsAvailable()) continue;
                available.Add(upgrade);
            }

            // Fisher-Yates 洗牌,保证每次升级展示顺序随机
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

        // 应用选中的组合强化：按条目顺序逐条应用；武器伤害/弹夹条目按掩码逐武器应用,
        // 任一武器伤害达到满级即触发该武器的进化。无效配置在此兜底拒绝。
        public void ApplyUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null || upgrade.effect == null) return;

            var effect = upgrade.effect;
            var errors = new List<string>();
            if (!effect.Validate(errors))
            {
                Debug.LogError($"[UpgradeManager] 强化 {upgrade.name} 配置无效,拒绝应用：{string.Join("；", errors)}", upgrade);
                return;
            }

            foreach (var stat in effect.stats)
            {
                PlayerUpgradeState.ApplyStat(stat.statType, stat.value);
            }

            foreach (var damage in effect.weaponDamages)
            {
                foreach (var weaponType in damage.weapons.ToWeaponTypes())
                {
                    if (PlayerUpgradeState.ApplyWeaponDamage(weaponType, damage.damageBonusPerStack))
                    {
                        TryEvolve(weaponType);
                    }
                }
            }

            foreach (var ammo in effect.weaponAmmos)
            {
                foreach (var weaponType in ammo.weapons.ToWeaponTypes())
                {
                    PlayerUpgradeState.ApplyWeaponAmmo(weaponType, ammo.ammoBonusPerStack);
                }
            }

            foreach (var passive in effect.passives)
            {
                PlayerUpgradeState.UnlockPassive(passive);
            }
        }

        // 查找目标武器上是否实现了 IWeaponEvolution,有则触发进化
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
