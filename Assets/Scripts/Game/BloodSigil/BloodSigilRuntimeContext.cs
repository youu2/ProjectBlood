using System.Collections.Generic;

namespace ProjectBlood
{
    // 单个已解锁血印的运行时句柄：持有该血印每个效果模块(BloodSigilEffect)的运行时状态，
    // 与配置中的 effects 列表一一对应。SO 资产不写运行时字段，状态随本句柄随生随灭。
    public class BloodSigilRuntimeContext
    {
        public BloodSigilSO Sigil { get; }

        // 与 Sigil.effects 顺序一致的模块运行时
        public List<BloodSigilModuleRuntime> Modules { get; }

        public BloodSigilRuntimeContext(BloodSigilSO sigil)
        {
            Sigil = sigil;
            Modules = new List<BloodSigilModuleRuntime>();
            if (sigil.effects != null)
            {
                foreach (var module in sigil.effects)
                {
                    if (module != null)
                        Modules.Add(new BloodSigilModuleRuntime(sigil, module));
                }
            }
        }
    }
}
