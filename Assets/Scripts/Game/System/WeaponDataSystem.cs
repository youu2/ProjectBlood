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
        // public int weaponId;
        // public string weaponName;
        // public int weaponDamage;
        // public int weaponRange;
        // public int weaponReloadTime;
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
    }
}
