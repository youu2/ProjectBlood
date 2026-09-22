// 手写占位文件：请在 Unity 中挂载 BossHealthBar 后，用 QFramework 绑定两个 Image 并重新生成本文件
// 重新生成时保持字段名 BossBarRed / BossBarGreen 即可，业务代码在 BossHealthBar.cs 中不受影响
using UnityEngine;

namespace ProjectBlood
{
    public partial class BossHealthBar
    {
        public UnityEngine.UI.Image BossBarRed;

        public UnityEngine.UI.Image BossBarWhite;

    }
}