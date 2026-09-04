namespace ProjectBlood
{
    // 武器类型枚举。项目原有武器通过 WeaponConfig 中的字符串名（"DE"、"MP5" 等）标识，
    // 强化系统需要类型安全的武器标识，故新增此枚举并提供与字符串名的双向映射。
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

    public static class WeaponTypeExtensions
    {
        // 武器字符串名 -> 枚举，未知名返回 None
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

        // 枚举 -> 武器字符串名，与 WeaponConfig / WeaponData.weaponName 保持一致
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
}
