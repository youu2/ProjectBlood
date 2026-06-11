using UnityEngine;
using QFramework;

namespace ProjectBlood
{
    public class Shield : DropItem
    {
        public int initialBlockCount = 5;
        public float initialDuration = 5f;

        void Awake()
        {
            autoCollectOnRoomFinish = false; // 护盾不会在房间完成后自动飞向玩家
        }

        protected override void Collect()
        {
            AudioKit.PlaySound("HpPickup", volume: 0.6f);
            Player.player1.ActivateShield(initialBlockCount, initialDuration);
            this.DestroyGameObjGracefully();
        }
    }
}