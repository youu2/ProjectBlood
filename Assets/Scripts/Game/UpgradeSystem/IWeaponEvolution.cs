namespace ProjectBlood
{
    // 武器进化接口：某把武器强化满级（5 次）后，由 UpgradeManager 调用 Evolve() 触发特殊形态。
    // 后续由具体武器类实现（例如 AWP：长按换弹合成 1 发 8 倍伤害子弹），
    // 实现类挂载在武器对象上即可被自动识别（武器对象本身就是 WeaponBase 子类）。
    public interface IWeaponEvolution
    {
        WeaponType WeaponType { get; }  // 该进化对应的武器类型
        bool IsEvolved { get; }        // 是否已进化，防止重复触发
        void Evolve();                 // 满级时触发的进化逻辑
    }
}
