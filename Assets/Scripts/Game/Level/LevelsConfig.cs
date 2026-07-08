using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class LevelsConfig : ViewController
    {
        public RoomNode InitRoom = new(RoomType.InitRoom);
        public string LevelName;
        // public int difficulty;
    }

    // 每个关卡有3种不同的布局，随机选择一种
    public class Level1_1
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "1 - 1";
        })
        .Self(self =>
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
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom);
                    })
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom);
            }
        });
    }

    public class Level1_2
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "1 - 2";
        })
        .Self(self =>
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
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom);
            }
        });
    }

    public class Level1_3
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "1 - 3";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ShopRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ChestRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.ChestRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ShopRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom);
                });
            }
        });
    }

    public class Level2_1
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "2 - 1";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    });
                });
            }
        });
    }

    public class Level2_2
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "2 - 2";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                });
            }
        });
    }

    public class Level2_3
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "2 - 3";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.ChestRoom)
                        .NextRoom(RoomType.NormalRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ShopRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom);
                });
            }
        });
    }

    public class Level3_1
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "3 - 1";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom)
                        .NextRoom(RoomType.NormalRoom);
                    });
                });
            }
        });
    }

    public class Level3_2
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "3 - 2";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ShopRoom)
                        .NextRoom(RoomType.NormalRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ChestRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                });
            }
        });
    }

    public class Level3_3
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "3 - 3";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.ShopRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.ChestRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.NormalRoom);
                    })
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.ShopRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.NormalRoom);
                });
            }
        });
    }

    public class Level_X
    {
        public static LevelsConfig Config = new LevelsConfig()
        .Self(self =>
        {
            self.LevelName = "?";
        })
        .Self(self =>
        {
            var randomConfigIndex = Random.Range(0, 3);
            if (randomConfigIndex == 0)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.ShopRoom);
                })
                .NextRoom(RoomType.NormalRoom)
                .NextRoom(RoomType.BossRoom);
            }
            else if (randomConfigIndex == 1)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.NormalRoom, branch =>
                    {
                        branch.NextRoom(RoomType.ShopRoom)
                        .NextRoom(RoomType.ChestRoom);
                    })
                    .NextRoom(RoomType.BossRoom);
                })
                .NextRoom(RoomType.NormalRoom);
            }
            else if (randomConfigIndex == 2)
            {
                self.InitRoom
                .NextRoom(RoomType.NormalRoom, branch =>
                {
                    branch.NextRoom(RoomType.ChestRoom)
                    .NextRoom(RoomType.NormalRoom)
                    .NextRoom(RoomType.BossRoom);

                    branch.NextRoom(RoomType.ShopRoom)
                    .NextRoom(RoomType.NormalRoom);
                });
            }
        });
    }
}