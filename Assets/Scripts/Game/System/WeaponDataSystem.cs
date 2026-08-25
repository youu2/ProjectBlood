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
        public static WeaponConfig DE = new WeaponConfig()
        {
            weaponName = "DE",
            weaponCurrentAmmo = 8,
            weaponMaxAmmo = 8,
        };
        public static WeaponConfig MP5 = new WeaponConfig()
        {
            weaponName = "MP5",
            weaponCurrentAmmo = 30,
            weaponMaxAmmo = 30,
        };
        public static WeaponConfig ShotGun = new WeaponConfig()
        {
            weaponName = "ShotGun",
            weaponCurrentAmmo = 6,
            weaponMaxAmmo = 6,
        };
        public static WeaponConfig AK = new WeaponConfig()
        {
            weaponName = "AK",
            weaponCurrentAmmo = 30,
            weaponMaxAmmo = 30,
        };
        public static WeaponConfig AWP = new WeaponConfig()
        {
            weaponName = "AWP",
            weaponCurrentAmmo = 10,
            weaponMaxAmmo = 10,
        };
        public static WeaponConfig Laser = new WeaponConfig()
        {
            weaponName = "Laser",
            weaponCurrentAmmo = 120,
            weaponMaxAmmo = 120,
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
