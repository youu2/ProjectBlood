using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectBlood
{
    public class WeaponData
    {
        public string weaponName;
        public int weaponCurrentAmmo;
        public int weaponMaxAmmo;
        public WeaponConfig weaponConfig;
    }

    public class WeaponConfig
    {
        public string weaponName;
        public int weaponCurrentAmmo;
        public int weaponMaxAmmo;
        public int poolPrewarmCount; // 子弹池预热数量，0 表示不预热(如 Laser)
        public static WeaponConfig DE = new WeaponConfig()
        {
            weaponName = "DE",
            weaponCurrentAmmo = 8,
            weaponMaxAmmo = 8,
            poolPrewarmCount = 3,
        };
        public static WeaponConfig MP5 = new WeaponConfig()
        {
            weaponName = "MP5",
            weaponCurrentAmmo = 30,
            weaponMaxAmmo = 30,
            poolPrewarmCount = 30, // 高射速全自动
        };
        public static WeaponConfig ShotGun = new WeaponConfig()
        {
            weaponName = "ShotGun",
            weaponCurrentAmmo = 6,
            weaponMaxAmmo = 6,
            poolPrewarmCount = 15,
        };
        public static WeaponConfig AK = new WeaponConfig()
        {
            weaponName = "AK",
            weaponCurrentAmmo = 30,
            weaponMaxAmmo = 30,
            poolPrewarmCount = 15,
        };
        public static WeaponConfig AWP = new WeaponConfig()
        {
            weaponName = "AWP",
            weaponCurrentAmmo = 10,
            weaponMaxAmmo = 10,
            poolPrewarmCount = 3, // 栓动极低射速
        };
        public static WeaponConfig Laser = new WeaponConfig()
        {
            weaponName = "Laser",
            weaponCurrentAmmo = 120,
            weaponMaxAmmo = 120,
            poolPrewarmCount = 0, // 射线武器无子弹池
        };
        // 全武器总表，供子弹池无视解锁状态统一预热。
        // 注意：C# 静态字段初始化按声明顺序执行，此列表必须声明在上面 6 个实例之后
        public static readonly List<WeaponConfig> All = new()
        {
            DE, MP5, ShotGun, AK, AWP, Laser,
        };
        public WeaponData NewWeapon()
        {
            return new WeaponData()
            {
                weaponName = weaponName,
                weaponConfig = this,
                weaponCurrentAmmo = weaponCurrentAmmo,
                weaponMaxAmmo = weaponMaxAmmo,
            };
        }
    }
    public class WeaponDataSystem : MonoBehaviour
    {
        public static List<WeaponData> weaponDataList = new()
        {
            WeaponConfig.DE.NewWeapon(),
        };

        // 查询玩家是否已持有指定名称的武器
        public static bool HasWeapon(string weaponName)
            => weaponDataList.Exists(w => w.weaponName == weaponName);

        // 统一添加入口：已持有同名武器时拒绝，防止重复装备
        public static bool AddWeapon(WeaponData data)
        {
            if (HasWeapon(data.weaponName)) return false;
            weaponDataList.Add(data);
            return true;
        }

        // 存档导出：重建 weaponDataList 快照
        public static void ExportTo(RunSaveData data)
        {
            data.weapons.Clear();
            foreach (var wd in weaponDataList)
            {
                data.weapons.Add(new WeaponSaveEntry
                {
                    weaponName = wd.weaponName,
                    currentAmmo = wd.weaponCurrentAmmo,
                    maxAmmo = wd.weaponMaxAmmo,
                });
            }
        }

        // 存档导入：按武器名匹配 WeaponConfig 重建列表，忽略未知武器
        public static void ImportFrom(RunSaveData data)
        {
            weaponDataList.Clear();
            foreach (var entry in data.weapons)
            {
                var config = WeaponConfig.All.Find(c => c.weaponName == entry.weaponName);
                if (config == null) continue;
                var wd = config.NewWeapon();
                wd.weaponCurrentAmmo = entry.currentAmmo;
                wd.weaponMaxAmmo = entry.maxAmmo;
                weaponDataList.Add(wd);
            }
            // 兜底：至少保留初始武器，避免空手
            if (weaponDataList.Count == 0)
                weaponDataList.Add(WeaponConfig.DE.NewWeapon());
        }
    }
}
