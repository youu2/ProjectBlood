using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LevelsConfig : ViewController
    {
        public RoomNode InitRoom = new(RoomType.InitRoom);
    }

    public class Level1
    {
        public static LevelsConfig Config = new LevelsConfig().Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom);
                })
                .NextRoom(RoomType.ShopRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom, branch =>
                    {
                        branch.NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ChestRoom);
            }


        }

        );
    }
}