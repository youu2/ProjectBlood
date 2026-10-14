using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    // 商店房预进场触发区：玩家在走廊即将进入商店时（'S' 位置生成），提前一次性刷新全场货架，
    // 使商品基于玩家最新状态生成，且上架过程发生在走廊中、玩家不可见。
    // 主路径：自身触发器被玩家踏入；兜底：进房事件（zone 尺寸不足或读档时玩家已在店内）。
    // 注意：触发区尺寸以刚越过门线为限，不得伸入相邻战斗房内部——
    // 战斗结算期间血量/金币仍在变化，过早刷新会导致动态权重取到中间状态。
    public partial class ShopRoomZone : ViewController
    {
        private Room cachedRoom;

        private void Start()
        {
            cachedRoom = GetComponentInParent<Room>();
            Room.OnPlayerEnteredRoom += HandleRoomEntered;
        }

        private void OnDestroy()
        {
            // 静态事件必须退订，防止跨关卡泄漏死引用
            Room.OnPlayerEnteredRoom -= HandleRoomEntered;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            RefreshRoomShops();
        }

        private void HandleRoomEntered(Room room)
        {
            if (room != cachedRoom) return;
            RefreshRoomShops();
        }

        // 单点触发全场刷新：所有货架同帧上架，避免逐个弹出的伪影（RefreshGoods 幂等，可安全重复触发）
        // 注：ShopItem 与 ShopRoomZone 同为 Room 的子物体（兄弟关系），必须以 Room 为根搜索；
        // 以 zone 自身为根会得到空集，导致货架永不刷新
        private void RefreshRoomShops()
        {
            if (cachedRoom == null) cachedRoom = GetComponentInParent<Room>();
            if (cachedRoom == null) return;
            var takenKeys = new HashSet<string>();
            foreach (var shop in cachedRoom.GetComponentsInChildren<ShopItem>())
            {
                shop.RefreshGoods(takenKeys);
            }
        }
    }
}
