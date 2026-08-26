using System;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public partial class Room : ViewController
    {
        // 定义事件:每次当玩家进入房间时触发
        public static event Action<Room> OnPlayerEnteredRoom;
        private List<Vector3> enemyPosList = new List<Vector3>();
        private List<Door> doorList = new List<Door>();
        public RoomConfig roomConfig { get; private set; }
        private HashSet<IDamageable> enemySet = new HashSet<IDamageable>();
        public RoomState roomState = RoomState.Unknown;
        public MapController.RoomGenerateConfig roomGenerateConfig { get; private set; }
        private List<EnemyWaveConfig> enemyWaveConfigList = new List<EnemyWaveConfig>();
        private EnemyWaveConfig currentEnemyWaveConfig = null;
        public DynaGrid<PathSearchingHelper.TileNode> PathSearchingGrid { get; private set; }
        public int colorIndex = -1;

        public Vector3Int roomSize { get; set; }
        public Vector3Int roomPos { get; set; }
        public Vector3Int LB { get; set; }
        public Vector3Int RT { get; set; }



        public void InitPathSearchingGrid()
        {
            if (roomConfig.roomType == RoomType.NormalRoom || roomConfig.roomType == RoomType.BossRoom)
            {
                PathSearchingGrid = new DynaGrid<PathSearchingHelper.TileNode>();
                for (int x = LB.x; x < RT.x; x++)
                {
                    for (int y = LB.y; y < RT.y; y++)
                    {
                        var walkable = MapController.instance.wallTilemap.GetTile(new Vector3Int(x, y, 0)) == null;
                        PathSearchingGrid[x, y] = new PathSearchingHelper.TileNode(PathSearchingGrid);
                        PathSearchingGrid[x, y].Init(new PathSearchingHelper.TileCoords() { Position = new Vector3Int(x, y, 0) }, walkable);
                    }
                }
                PathSearchingGrid.ForEach(node => node.CacheNeighbors());
            }
        }

        public enum RoomState
        {
            Unknown,    // 玩家探测范围外的房间，不会在小地图显示
            Init,   // 玩家观测到的房间，但是没进入过
            Battle,
            Finished,   // 战斗通关的房间
            Idle,   // 无强制性战斗的房间，可拾取物不会飞向玩家
        }
        public Room WithRoomConfig(RoomConfig roomConfig)
        {
            this.roomConfig = roomConfig;
            return this;
        }
        public Room WithRoomGenerateConfig(MapController.RoomGenerateConfig roomGenerateConfig)
        {
            this.roomGenerateConfig = roomGenerateConfig;
            return this;
        }

        void GenerateEnemy(EnemyWaveConfig waveConfig)
        {
            enemyWaveConfigList.RemoveAt(0);
            // 每次根据难度分数随机生成敌人
            var enemyCount = waveConfig.Enemy2GenList.Count;
            // 按照离玩家的距离排序enemyPosList，距离玩家远的敌人优先生成
            var pos2Gen = enemyPosList
                .OrderByDescending(pos => (Player.player1.Position2D() - pos.ToVector2()).magnitude)
                .Take(enemyCount).ToList();

            // 设置所有敌人的位置和房间
            foreach (GameObject enemy2Gen in waveConfig.Enemy2GenList)
            {
                enemy2Gen.Show();
                enemy2Gen.transform.position = pos2Gen.GetAndRemoveRandomItem();
                var enemy = enemy2Gen.GetComponent<IDamageable>();
                enemy.Room = this;
                if (enemy != null)
                {
                    enemySet.Add(enemy);
                }
            }
        }

        private void Update()
        {
            if (Time.frameCount % 30 == 0)
            {
                // enemySet.RemoveWhere(enemy => enemy.IsDying);

                if (enemySet.Count == 0 && roomState == RoomState.Battle)
                {
                    // 所有敌人死亡后，生成下一批敌人
                    if (enemyWaveConfigList.Count > 0)
                    {
                        var wave = enemyWaveConfigList[0];
                        GenerateEnemy(wave);
                    }
                    else
                    {
                        roomState = RoomState.Finished;
                        AudioKitManager.Instance.PlayOneShot("DoorOpeningSfx");
                        foreach (var door in doorList)
                        {
                            door.Hide();
                        }
                        return;
                    }
                }
            }
        }

        // 保存一个房间配置中所有的敌人位置
        public void AddEnemy(Vector3 enemyPos)
        {
            enemyPosList.Add(enemyPos);
        }

        public static void FindRoom()
        {
            if (Global.currentRoom == null || Global.currentRoom.roomGenerateConfig == null)
            {
                return;
            }

            MapController.instance.RoomGrid.ForEach((x, y, room) =>
            {
                if (room == null || room.roomGenerateConfig == null)
                {
                    return;
                }

                var playerX = Global.currentRoom.roomGenerateConfig.roomPosX;
                var playerY = Global.currentRoom.roomGenerateConfig.roomPosY;
                // 玩家会发现当前房间的相邻房间
                if (room.roomState == Room.RoomState.Unknown
                && IsAdjacentRoom(Global.currentRoom, x, y, playerX, playerY))
                {
                    room.roomState = Room.RoomState.Init;
                }
            });
        }

        private static bool IsAdjacentRoom(Room room, int x, int y, int playerX, int playerY)
        {
            var gapX = x - playerX;
            var gapY = y - playerY;
            if ((gapX == 1 && gapY == 0 && HasThisDoor(room, MapController.Direction.Right))
            || (gapX == -1 && gapY == 0 && HasThisDoor(room, MapController.Direction.Left))
            || (gapX == 0 && gapY == 1 && HasThisDoor(room, MapController.Direction.Up))
            || (gapX == 0 && gapY == -1 && HasThisDoor(room, MapController.Direction.Down)))
            {
                return true;
            }
            return false;
        }

        private static bool HasThisDoor(Room room, MapController.Direction dir)
        {
            if (room == null || room.doorList == null)
            {
                return false;
            }

            foreach (var door in room.doorList)
            {
                if (door != null && door.direction == dir)
                {
                    return true;
                }
            }
            return false;
        }

        private MapController.Direction DirOfRoom()
        {
            return MapController.Direction.Down;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (roomConfig == null || roomGenerateConfig == null)
            {
                return;
            }

            Global.currentRoom = this;  // 非战斗房间也要更新currentRoom

            if (roomState == RoomState.Unknown)
            {
                roomState = RoomState.Init;
            }

            FindRoom(); // 非战斗房间也要更新周围房间

            // 触发玩家进入房间事件
            OnPlayerEnteredRoom?.Invoke(this);

            if (roomConfig.roomType == RoomType.NormalRoom)
            {
                if (roomState == RoomState.Init)
                {
                    roomState = RoomState.Battle;

                    var difficultyLevel = Global.currentDifficulty; // 通常是0 - 9
                    var difficultyScore = UnityEngine.Random.Range(3, 6 + 1) + 2 * difficultyLevel - 1 / (difficultyLevel + 1);
                    // 第一关都是1分敌人，所以限制第一关的difficultyScore来避免生成过多的低级敌人

                    // 难度等级影响敌人波次，0-2为1-2波，3-5为1-3波，6-8为2-4波，9为3-5波
                    var waveCount = UnityEngine.Random.Range(Math.Max(1, difficultyLevel / 3), difficultyLevel / 3 + 2 + 1);

                    for (int i = 0; i < waveCount; i++)
                    {
                        var targetScore = difficultyScore;
                        var waveConfig = new EnemyWaveConfig();

                        while (targetScore > 0)
                        {
                            // 限制可生成敌人的种类
                            var enemyScore2Gen = Math.Min(UnityEngine.Random.Range(1, Mathf.Min(difficultyLevel + 1 + 1, EnemyFactory.Instance.enemyList.Count + 1)), targetScore);
                            targetScore -= enemyScore2Gen;
                            waveConfig.Enemy2GenList.Add(EnemyFactory.EnemyByScore(enemyScore2Gen));
                        }
                        enemyWaveConfigList.Add(waveConfig);
                    }

                    if (doorList != null)
                    {
                        foreach (var door in doorList)
                        {
                            if (door != null)
                            {
                                door.Show();
                            }
                        }
                    }
                    AudioKitManager.Instance.PlayOneShot("DoorClosingSfx");
                }
            }
            else
            {
                roomState = RoomState.Idle;
            }
        }

        public void AddDoor(Door door)
        {
            doorList.Add(door);
        }

        public HashSet<IDamageable> GetEnemies()
        {
            return enemySet;
        }
    }
}
