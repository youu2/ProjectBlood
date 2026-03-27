namespace ProjectBlood
{
    // 管理攻击间隔的类，提供CanAttack方法来判断是否可以攻击，以及RecordAttackTime方法来记录攻击时间
    public class GunClip
    {
        public int maxAmmo; // 最大弹药量
        public int currentAmmo; // 当前弹药量
        public bool isReloading; // 是否正在换弹
        public bool isEmpty => currentAmmo <= 0; // 是否弹药已空
        public GunClip(int maxAmmo)
        {
            this.maxAmmo = maxAmmo;
            this.currentAmmo = maxAmmo; // 初始时弹药量为最大值
            this.isReloading = false; // 初始时不在换弹状态
        }
        public void reload()
        {
            if (!isReloading)
            {
                isReloading = true;
                // 添加换弹动画或音效的逻辑
                // 换弹完成后重置弹药量
                currentAmmo = maxAmmo;
                isReloading = false;
            }
        }
        public bool CanShoot()
        {
            return !isReloading && currentAmmo > 0; // 只有在不换弹且有弹药时才允许射击
        }
    }
}