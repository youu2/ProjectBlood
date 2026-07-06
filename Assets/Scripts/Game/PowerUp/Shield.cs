using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    // 只是用来激活护盾状态的道具，不是护盾本身
    public class Shield : DropItem
    {
        private int initialBlockCount;  // 护盾能抵挡的伤害次数
        private float initialDuration;  // 护盾无敌期时间（最短持续时间）

        void Awake()
        {
            initialBlockCount = 5;
            initialDuration = 5f;
            autoCollectOnRoomFinish = false; // 护盾不会在房间完成后自动飞向玩家
            price = 10;
        }

        // 收集护盾道具时，激活玩家身上的护盾
        protected override void Collect()
        {
            AudioKitManager.Instance.PlayOneShot("ShieldPickUp", volume: 1.0f);
            Player.player1.ActivateShield(initialBlockCount, initialDuration);
            this.DestroyGameObjGracefully();
        }
    }
}