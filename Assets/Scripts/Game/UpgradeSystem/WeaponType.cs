using System;
using System.Collections.Generic;

namespace ProjectBlood
{
    // 武器类型枚举。项目原有武器通过 WeaponConfig 中的字符串名("DE","MP5" 等)标识,
    // 强化系统需要类型安全的武器标识,故新增此枚举并提供与字符串名的双向映射。
    public enum WeaponType
    {
        None = 0,
        DE,
        MP5,
        ShotGun,
        AK,
        AWP,
        Laser,
    }

    // 武器多选掩码：[Flags] 让 Inspector 显示为勾选框,一条强化效果可同时作用于多把武器。
    // 与 WeaponType 的对应关系：WeaponType 枚举值 1..N 依次对应 bit 0..N-1,
    // 新增武器时在两个枚举末尾各追加一项即可自动获得新掩码位。
    [Flags]
    public enum WeaponTypeFlags
    {
        None = 0,
        DE = 1 << 0,
        MP5 = 1 << 1,
        ShotGun = 1 << 2,
        AK = 1 << 3,
        AWP = 1 << 4,
        Laser = 1 << 5,
    }

    public static class WeaponTypeExtensions
    {
        // 武器字符串名 -> 枚举,未知名返回 None
        public static WeaponType FromName(string weaponName)
        {
            switch (weaponName)
            {
                case "DE": return WeaponType.DE;
                case "MP5": return WeaponType.MP5;
                case "ShotGun": return WeaponType.ShotGun;
                case "AK": return WeaponType.AK;
                case "AWP": return WeaponType.AWP;
                case "Laser": return WeaponType.Laser;
                default: return WeaponType.None;
            }
        }

        // 枚举 -> 武器字符串名,与 WeaponConfig / WeaponData.weaponName 保持一致
        public static string ToWeaponName(this WeaponType type)
        {
            switch (type)
            {
                case WeaponType.DE: return "DE";
                case WeaponType.MP5: return "MP5";
                case WeaponType.ShotGun: return "ShotGun";
                case WeaponType.AK: return "AK";
                case WeaponType.AWP: return "AWP";
                case WeaponType.Laser: return "Laser";
                default: return null;
            }
        }
    }

    public static class WeaponTypeFlagsExtensions
    {
        // 单个武器类型转掩码位
        public static WeaponTypeFlags ToFlag(this WeaponType type)
            => type == WeaponType.None ? WeaponTypeFlags.None : (WeaponTypeFlags)(1 << ((int)type - 1));

        // 掩码中是否包含某个武器
        public static bool Contains(this WeaponTypeFlags flags, WeaponType type)
            => type != WeaponType.None && (flags & type.ToFlag()) != WeaponTypeFlags.None;

        // 展开掩码为武器类型列表(按 WeaponType 枚举定义顺序)
        public static List<WeaponType> ToWeaponTypes(this WeaponTypeFlags flags)
        {
            var list = new List<WeaponType>();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                if (flags.Contains(type)) list.Add(type);
            }
            return list;
        }
    }
}
