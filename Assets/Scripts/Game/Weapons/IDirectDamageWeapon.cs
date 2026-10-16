namespace ProjectBlood
{
    /// <summary>
    /// 直伤武器：不通过子弹预制体飞行、伤害直接在武器侧即时结算的武器
    /// （Laser / ShotGun / AWP）。暂停页伤害面板据此读取基础伤害，
    /// 替代旧的"读 BulletPrefab 上 PlayerBullet.damage"路径。
    /// </summary>
    public interface IDirectDamageWeapon
    {
        float BaseDamage { get; }
    }
}
