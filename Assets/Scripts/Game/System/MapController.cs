using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ProjectBlood
{
    public partial class MapController : ViewController
    {
        public TileBase wall_0;
        public TileBase wall_1;
        public TileBase wall_2;
        public TileBase wall_3;
        public TileBase groundTile;
        public TileBase wallH0;
        public TileBase wallH1;
        public TileBase wallH2;
        public TileBase wallH3;

        public TileBase floor_0;
        public TileBase floor_1;
        public TileBase floor_2;
        public TileBase floor_3;
        // 随机选择一个墙壁tile
        public TileBase randWall => new TileBase[] { wall_0, wall_1, wall_2, wall_3 }[Random.Range(0, 4)];
        public TileBase randWallH => new TileBase[] { wallH0, wallH1, wallH2, wallH3 }[Random.Range(0, 4)];
        public TileBase randFloor => new TileBase[] { floor_0, floor_1, floor_2, floor_3 }[Random.Range(0, 4)];

        public Tilemap wallTilemap;
        public Tilemap floorTilemap;
        public GameObject Portal;

        // Boss 预制体列表：按楼层（1-3层）分别配置，X-1/X-2 的 BossRoom 直接出传送门，X-3 才出 Boss
        // 索引 0 = 第1层(1-3)，1 = 第2层(2-3)，2 = 第3层(3-3)。如果只配了1个，所有楼层都用它。
        public List<GameObject> BossPrefabs = new List<GameObject>();

        public static MapController instance;

        // 相邻标准房间之间的走廊长度（格子数），同时决定房间定位步长
        private const int CorridorLength = 10;
        // 相邻房间之间走廊的最小长度（格子数），由此推导房间模板尺寸上限：最大边长 = 标准边长 + CorridorLength - MinCorridorLength
        private const int MinCorridorLength = 2;
        // 标准房间尺寸（以 InitRoom 模板为准），所有房间中心固定在槽位中心，非标准尺寸通过左上角偏移吸收
        private static int StandardRoomWidth => RoomConfig.InitRoom.Width;
        private static int StandardRoomHeight => RoomConfig.InitRoom.Height;
        // 布局槽位步长（标准房间占位 + 一条走廊）
        private static int SlotStepX => StandardRoomWidth + CorridorLength;
        private static int SlotStepY => StandardRoomHeight + CorridorLength;

        // 动态门布局网格，存储要生成的每个房间的生成配置（房间节点、门方向、网格坐标）
        public DynaGrid<RoomGenerateConfig> DynamicDoorLayout { get; private set; }
        // 房间实例网格，存储已生成的房间对象引用
        public DynaGrid<Room> RoomGrid { get; private set; }

        public class RoomGenerateConfig
        {
            public RoomNode roomNode;
            public HashSet<Direction> doorDirections { get; set; }
            public int roomPosX { get; set; }
            public int roomPosY { get; set; }
            // 该房间实际选用的房间配置（决定实际尺寸，用于中心对齐定位与过道自适应长度）
            public RoomConfig roomConfig { get; set; }
        }

        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
        }

        void Awake()
        {
            instance = this;
            RoomGrid = new DynaGrid<Room>();
            DynamicDoorLayout = new DynaGrid<RoomGenerateConfig>();
        }

        void Start()
        {
            Global.IsGamePaused = false;
            // 读档还原：存在待恢复载荷时走还原路径，跳过 BFS 随机生成
            if (RunSaveService.PendingRestore != null)
            {
                RestoreFromSave(RunSaveService.PendingRestore);
                RunSaveService.PendingRestore = null;
                return;
            }
            // 如果打通了所有关卡，打开游戏通关面板
            if (Global.currentDifficulty >= Global.LevelConfigs.Count)
            {
                UIKit.OpenPanel<UIGamePassPanel>();
                return;
            }
            else
            {
                InitializeLevel(Global.LevelConfigs[Global.currentDifficulty]);
            }
        }


        // 初始化关卡，生成房间布局和连接通道 参数：levelConfig - 关卡配置
        private void InitializeLevel(LevelsConfig levelConfig)
        {
            GameUI.ShowLevelText(levelConfig.LevelName, duration: 3);

            var layout = levelConfig.InitRoom;

            GenerateRoomLayoutBFS(layout);  // 生成房间布局
            GenerateRoomsFromLayout();  // 从布局生成房间
            GenerateCorridors();  // 根据房间布局和门方向生成连接通道
            ShadowCaster2DGenerator.Generate(wallTilemap);  // 为围墙/掩体/走廊墙生成阴影投影

            Room.FindRoom();

            // 关卡检查点：新关卡生成完成后存档（过传送门进入下一关 / 新游戏开局都会经过这里）
            RunSaveService.SaveNow();
        }

        // BFS生成房间布局，通过预测权重控制分支方向，避免生成死路
        private void GenerateRoomLayoutBFS(RoomNode rootRoom)
        {
            int predictWeight = 0;

            // 尝试生成房间布局，直到成功生成
            // 如果生成失败，增加预测权重，重新尝试
            while (!TryGenerateRoomLayout(rootRoom, predictWeight))
            {
                predictWeight++;
                DynamicDoorLayout.Clear();
            }
        }
        // 尝试生成整个地图所有房间布局，返回是否成功生成
        // 参数：rootRoom - 根房间节点，predictWeight - 预测权重（越高越可能选择最优方向）
        private bool TryGenerateRoomLayout(RoomNode rootRoom, int predictWeight)
        {
            var roomQueue = new Queue<RoomGenerateConfig>();
            roomQueue.Enqueue(new RoomGenerateConfig()
            {
                roomNode = rootRoom,
                roomPosX = 0,
                roomPosY = 0,
                doorDirections = new HashSet<Direction>(),
            });

            while (roomQueue.Count > 0)
            {
                var generateConfig = roomQueue.Dequeue();
                DynamicDoorLayout[generateConfig.roomPosX, generateConfig.roomPosY] = generateConfig;

                List<Direction> validDirections = LevelGenHelper.GetValidDirections(
                    generateConfig.roomPosX, generateConfig.roomPosY, DynamicDoorLayout);

                if (generateConfig.roomNode.Childrens.Count > validDirections.Count)
                {
                    // Debug.LogWarning("没有足够的可以延伸的方向");
                    return false;
                }

                // 遍历当前房间的所有子房间, 即广度优先遍历
                foreach (var childRoom in generateConfig.roomNode.Childrens)
                {
                    // 预测当前房间的每一个可选生成方向的下一个房间位置的可选方向数量, 返回一个(方向, 可选方向数量)列表
                    var directionsWithCount = LevelGenHelper.PredictDirectionWithCount(
                        generateConfig.roomPosX, generateConfig.roomPosY, DynamicDoorLayout);

                    directionsWithCount.Sort((a, b) => b.Count - a.Count);

                    if (directionsWithCount.Count == 0)
                    {
                        // Debug.LogWarning("没有可以延伸的方向");
                        return false;
                    }

                    // 基于预测权重选择下一个房间延伸方向，最优方向或随机选择一个方向
                    Direction nextDirection = SelectNextDirection(directionsWithCount, predictWeight);
                    RoomGenerateConfig newRoomConfig = CreateRoomConfig(generateConfig, childRoom, nextDirection);

                    if (newRoomConfig != null)
                    {
                        generateConfig.doorDirections.Add(nextDirection);
                        DynamicDoorLayout[newRoomConfig.roomPosX, newRoomConfig.roomPosY] = newRoomConfig;
                        roomQueue.Enqueue(newRoomConfig);
                    }
                }
            }

            return true;
        }

        // 根据预测权重选择下一个房间延伸方向 参数：directionsWithCount - 带预测计数的方向列表，predictWeight - 预测权重 返回选中的方向
        private Direction SelectNextDirection(List<LevelGenHelper.DirectionWithCount> directionsWithCount, int predictWeight)
        {
            if (Random.Range(0, 100) < predictWeight)
            {
                return directionsWithCount.First().Direction;   // 最优方向，生成失败次数越多，越倾向选择最优方向
            }
            else
            {
                return directionsWithCount.GetAndRemoveRandomItem().Direction;
            }
        }

        // 根据方向创建新的房间配置 参数：parentConfig - 父房间配置，childRoom - 子房间节点，direction - 延伸方向 返回新房间配置（失败返回null）
        private RoomGenerateConfig CreateRoomConfig(RoomGenerateConfig parentConfig, RoomNode childRoom, Direction direction)
        {
            int newX = parentConfig.roomPosX;
            int newY = parentConfig.roomPosY;
            HashSet<Direction> doorDir = new HashSet<Direction>();

            switch (direction)
            {
                case Direction.Right:
                    newX = parentConfig.roomPosX + 1;
                    doorDir.Add(Direction.Left);
                    break;
                case Direction.Left:
                    newX = parentConfig.roomPosX - 1;
                    doorDir.Add(Direction.Right);
                    break;
                case Direction.Up:
                    newY = parentConfig.roomPosY + 1;
                    doorDir.Add(Direction.Down);
                    break;
                case Direction.Down:
                    newY = parentConfig.roomPosY - 1;
                    doorDir.Add(Direction.Up);
                    break;
            }

            return new RoomGenerateConfig()
            {
                roomPosX = newX,
                roomPosY = newY,
                roomNode = childRoom,
                doorDirections = doorDir,
            };
        }

        // 根据布局生成所有房间实例
        private void GenerateRoomsFromLayout()
        {
            DynamicDoorLayout.ForEach((x, y, roomGenerateConfig) =>
            {
                var room = CreateRoomByType(x, y, roomGenerateConfig);
                RoomGrid[x, y] = room;
            });
        }

        // 根据房间类型创建房间实例（房间中心固定在槽位中心，尺寸差异通过左上角偏移吸收，保证门和过道对齐） 参数：gridX - 网格X坐标，gridY - 网格Y坐标，roomGenerateConfig - 房间生成配置 返回创建的房间实例
        private Room CreateRoomByType(int gridX, int gridY, RoomGenerateConfig roomGenerateConfig)
        {
            var roomConfig = roomGenerateConfig.roomNode.roomType switch
            {
                RoomType.InitRoom => RoomConfig.InitRoom,
                RoomType.NormalRoom => RoomConfig.normalRoomConfigList.GetRandomItem(),
                RoomType.ChestRoom => RoomConfig.ChestRoom,
                RoomType.ShopRoom => RoomConfig.ShopRoom,
                RoomType.BossRoom => RoomConfig.BossRoom,
                _ => null,
            };

            if (roomConfig == null)
            {
                return null;
            }

            roomGenerateConfig.roomConfig = roomConfig;

            // 房间从左上角开始向下绘制：宽了左移、高了上移（y 增大），中心才保持不变
            var currentRoomPosX = RoomStartPosX(roomGenerateConfig);
            var currentRoomPosY = RoomStartPosY(roomGenerateConfig);

            if (roomGenerateConfig.roomNode.roomType == RoomType.InitRoom)
            {
                var initRoom = GenerateRoom(currentRoomPosX, currentRoomPosY, roomConfig, roomGenerateConfig);
                Global.currentRoom = initRoom;
                initRoom.roomState = Room.RoomState.Finished;
                return initRoom;
            }

            return GenerateRoom(currentRoomPosX, currentRoomPosY, roomConfig, roomGenerateConfig);
        }

        // 生成房间之间的连接通道（长度由两端房间实际尺寸自适应，两端中心不变因此门和过道永远对齐）
        private void GenerateCorridors()
        {
            DynamicDoorLayout.ForEach((x, y, roomGenerateConfig) =>
            {
                if (roomGenerateConfig.doorDirections.Contains(Direction.Right))
                {
                    DrawHorizontalCorridor(roomGenerateConfig, DynamicDoorLayout[x + 1, y]);
                }

                if (roomGenerateConfig.doorDirections.Contains(Direction.Up))
                {
                    DrawVerticalCorridor(roomGenerateConfig, DynamicDoorLayout[x, y + 1]);
                }
            });
        }

        // 房间左上角格坐标：槽位原点加上中心对齐偏移（大房间向左/上偏移，小房间相反）
        private int RoomStartPosX(RoomGenerateConfig config) => config.roomPosX * SlotStepX - (config.roomConfig.Width - StandardRoomWidth) / 2;
        private int RoomStartPosY(RoomGenerateConfig config) => config.roomPosY * SlotStepY + (config.roomConfig.Height - StandardRoomHeight) / 2;

        // 房间中心所在行/列（与房间尺寸无关，恒等于槽位中心）
        private int RoomCenterPosX(RoomGenerateConfig config) => RoomStartPosX(config) + (config.roomConfig.Width - 1) / 2;
        private int RoomCenterPosY(RoomGenerateConfig config) => RoomStartPosY(config) - (config.roomConfig.Height - 1) / 2;

        // 绘制水平通道（向右延伸）：从当前房间右墙外一格画到右侧房间左墙前一格，长度由两端房间实际尺寸决定
        private void DrawHorizontalCorridor(RoomGenerateConfig current, RoomGenerateConfig next)
        {
            int startX = RoomStartPosX(current) + current.roomConfig.Width;
            int length = RoomStartPosX(next) - startX;
            int corridorY = RoomCenterPosY(current);

            if (length < MinCorridorLength)
            {
                Debug.LogError($"水平过道长度 {length} 低于最小值 {MinCorridorLength}，房间模板尺寸过大，" +
                               $"最大边长不能超过 {StandardRoomWidth + CorridorLength - MinCorridorLength}");
            }

            if (length <= 0) return;

            for (int i = 0; i < length; i++)
            {
                floorTilemap.SetTile(new Vector3Int(startX + i, corridorY + 1, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(startX + i, corridorY, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(startX + i, corridorY - 1, 0), randFloor);

                wallTilemap.SetTile(new Vector3Int(startX + i, corridorY + 2, 0), randWall);
                wallTilemap.SetTile(new Vector3Int(startX + i, corridorY - 2, 0), randWall);
            }
        }

        // 绘制垂直通道（向上延伸）：从当前房间上墙外一行画到上方房间下墙前一格，长度由两端房间实际尺寸决定
        private void DrawVerticalCorridor(RoomGenerateConfig current, RoomGenerateConfig next)
        {
            int startY = RoomStartPosY(current) + 1;
            int length = RoomStartPosY(next) - next.roomConfig.Height - startY + 1;
            int corridorX = RoomCenterPosX(current);

            if (length < MinCorridorLength)
            {
                Debug.LogError($"垂直过道长度 {length} 低于最小值 {MinCorridorLength}，房间模板尺寸过大，" +
                               $"最大边长不能超过 {StandardRoomHeight + CorridorLength - MinCorridorLength}");
            }

            if (length <= 0) return;

            for (int i = 0; i < length; i++)
            {
                floorTilemap.SetTile(new Vector3Int(corridorX + 1, startY + i, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(corridorX, startY + i, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(corridorX - 1, startY + i, 0), randFloor);

                wallTilemap.SetTile(new Vector3Int(corridorX + 2, startY + i, 0), randWall);
                wallTilemap.SetTile(new Vector3Int(corridorX - 2, startY + i, 0), randWall);
            }
        }

        // 生成单个房间的具体实现，根据房间配置绘制墙体、地板和放置游戏对象 参数：startPosX - 起始X坐标，startPosY - 起始Y坐标，roomConfig - 房间配置，roomGenerateConfig - 房间生成配置 返回生成的房间实例
        Room GenerateRoom(int startPosX, int startPosY, RoomConfig roomConfig, RoomGenerateConfig roomGenerateConfig)
        {
            var roomWidth = roomConfig.Width;
            var roomHeight = roomConfig.Height;
            var roomCenter = new Vector2(0.5f + startPosX + roomWidth / 2, 0.5f + startPosY - roomHeight / 2);

            var roomObj = Room.InstantiateWithParent(this)
                            .WithRoomConfig(roomConfig)
                            .WithRoomGenerateConfig(roomGenerateConfig)
                            .Position(roomCenter).Show();

            roomObj.SelfBoxCollider2D.size = new Vector2(roomWidth - 3.0f, roomHeight - 3.0f);

            for (int i = 0; i < roomConfig.roomMap.Count; i++)
            {
                for (int j = 0; j < roomConfig.roomMap[i].Length; j++)
                {
                    var x = j + startPosX;
                    var y = startPosY - i;

                    floorTilemap.SetTile(new Vector3Int(x, y, 0), randFloor);

                    char tileType = roomConfig.roomMap[i][j];
                    HandleTileType(tileType, x, y, roomCenter, roomObj, roomGenerateConfig);
                }
            }
            roomObj.LB = new Vector3Int(startPosX, startPosY - roomHeight, 0);
            roomObj.RT = new Vector3Int(startPosX + roomWidth, startPosY, 0);
            roomObj.InitPathSearchingGrid();

            return roomObj;
        }

        // 根据瓦片类型处理不同的游戏对象放置逻辑 参数：tileType - 瓦片类型字符，x - X坐标，y - Y坐标，roomCenter - 房间中心坐标，roomObj - 房间实例，roomGenerateConfig - 房间生成配置
        private void HandleTileType(char tileType, int x, int y, Vector2 roomCenter, Room roomObj, RoomGenerateConfig roomGenerateConfig)
        {
            float worldX = x + 0.5f;
            float worldY = y + 0.5f;
            Vector3 worldPos = new Vector3(worldX, worldY, 0);

            switch (tileType)
            {
                case '2':
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), randWall);
                    break;

                case '3':
                    // 房间内掩体，与墙同砖块渲染，投影由 ShadowCaster2DGenerator 全图扫描统一处理
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), randWall);
                    break;

                case '1':
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), randWallH);
                    break;

                case 'P':
                    Player.player1.transform.position = worldPos;
                    break;

                case 'e':
                    roomObj.AddEnemy(worldPos);
                    break;

                case 'X':
                    var boss = Instantiate(Enemy1);
                    boss.transform.position = worldPos;
                    break;

                case '#':
                    // '#' 是 Boss 房中心挂点：
                    // X-3 关卡（1-3、2-3、3-3）在这里生成 Boss，传送门先隐藏，Boss 死后才显示
                    // X-1 / X-2 关卡直接生成传送门（玩家可跳过）
                    if (Global.currentDifficulty % 3 == 2)
                    {
                        SpawnBoss(worldPos, roomObj);
                    }
                    else
                    {
                        var portal = Instantiate(Portal);
                        portal.transform.position = worldPos;
                    }
                    break;

                case 'd':
                    HandleDoorPlacement(x, y, roomCenter, roomObj, roomGenerateConfig);
                    break;

                case 'c':
                    Chest.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .Show();
                    break;

                case 's':
                    ShopItem.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .Show();
                    break;
            }
        }

        // 在 Boss 房中心生成 Boss：生成 Boss 本体 + 一个隐藏的传送门（Boss 死后显示）
        private void SpawnBoss(Vector3 worldPos, Room roomObj)
        {
            // 按楼层选 Boss 预制体：currentDifficulty / 3 得到楼层索引（0/1/2）
            int floorIndex = Global.currentDifficulty / 3;
            GameObject bossPrefab = null;

            if (BossPrefabs.Count > 0)
            {
                // 如果配了对应楼层的就用，否则用第一个
                if (floorIndex < BossPrefabs.Count && BossPrefabs[floorIndex] != null)
                {
                    bossPrefab = BossPrefabs[floorIndex];
                }
                else
                {
                    bossPrefab = BossPrefabs[0];
                }
            }

            if (bossPrefab == null)
            {
                Debug.LogError("BossPrefabs 未配置 Boss 预制体！X-3 关卡将无法生成 Boss。");
                // 保底：直接出传送门，避免卡死
                var fallbackPortal = Instantiate(Portal);
                fallbackPortal.transform.position = worldPos;
                return;
            }

            // 生成 Boss
            var bossObj = Instantiate(bossPrefab);
            bossObj.transform.position = worldPos;

            var bossComp = bossObj.GetComponent<BossBase>();
            if (bossComp != null)
            {
                // 把 Boss 注册到房间，这样 Boss 死亡后 Room.Update 会自动开门
                bossComp.Room = roomObj;
                roomObj.GetEnemies().Add(bossComp);

                // 生成一个隐藏的传送门，Boss 死亡时由 BossBase.Death 负责显示
                var portal = Instantiate(Portal);
                portal.transform.position = worldPos;
                portal.SetActive(false);
                bossComp.portal = portal;
            }
            else
            {
                Debug.LogError($"Boss 预制体 {bossPrefab.name} 没有 BossBase 组件！");
            }
        }

        // 处理门的放置逻辑，根据房间连接方向决定是否放置门或墙 参数：x - 网格X坐标，y - 网格Y坐标，roomCenter - 房间中心坐标，roomObj - 房间实例，roomGenerateConfig - 房间生成配置
        private void HandleDoorPlacement(int x, int y, Vector2 roomCenter, Room roomObj, RoomGenerateConfig roomGenerateConfig)
        {
            Vector3 worldPos = new Vector3(x + 0.5f, y + 0.5f, 0);
            var doorDistance = worldPos - (Vector3)roomCenter;

            if (Mathf.Abs(doorDistance.x) > Mathf.Abs(doorDistance.y))
            {
                if (doorDistance.x > 0 && roomGenerateConfig.doorDirections.Contains(Direction.Right))
                {
                    var door = Door.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .WithDirection(Direction.Right)
                        .Hide();
                    roomObj.AddDoor(door);
                }
                else if (doorDistance.x < 0 && roomGenerateConfig.doorDirections.Contains(Direction.Left))
                {
                    var door = Door.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .WithDirection(Direction.Left)
                        .Hide();
                    roomObj.AddDoor(door);
                }
                else
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), randWall);
                }
            }
            else
            {
                if (doorDistance.y > 0 && roomGenerateConfig.doorDirections.Contains(Direction.Up))
                {
                    var door = Door.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .WithDirection(Direction.Up)
                        .Hide();
                    roomObj.AddDoor(door);
                }
                else if (doorDistance.y < 0 && roomGenerateConfig.doorDirections.Contains(Direction.Down))
                {
                    var door = Door.InstantiateWithParent(roomObj)
                        .Position2D(worldPos)
                        .WithDirection(Direction.Down)
                        .Hide();
                    roomObj.AddDoor(door);
                }
                else
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), randWallH);
                }
            }
        }

        // ============================== 存档：地图导出 ==============================

        // 把当前布局/房间状态写入存档（地图与房间层）
        public void ExportTo(RunSaveData data)
        {
            data.levelName = Global.LevelConfigs[Global.currentDifficulty].LevelName;
            data.rooms.Clear();
            data.discoveredRooms.Clear();

            DynamicDoorLayout.ForEach((x, y, cfg) =>
            {
                if (cfg == null || cfg.roomConfig == null) return;
                var room = RoomGrid[x, y];
                var entry = new RoomSaveEntry
                {
                    gridX = x,
                    gridY = y,
                    roomType = (int)cfg.roomNode.roomType,
                    tileRows = new List<string>(cfg.roomConfig.roomMap),
                    roomState = room != null ? (int)room.roomState : (int)Room.RoomState.Unknown,
                };
                foreach (var dir in cfg.doorDirections)
                    entry.doorDirs.Add((int)dir);

                // 宝箱/商店收集状态
                room?.CollectSaveState(entry);

                data.rooms.Add(entry);

                // 已发现 = Init/Battle/Finished/Idle（即非 Unknown）
                if (room != null && room.roomState != Room.RoomState.Unknown)
                    data.discoveredRooms.Add($"{x},{y}");
            });
        }

        // ============================== 存档：还原路径 ==============================

        // 按存档数据重建整张地图（跳过 BFS），恢复房间状态与玩家位置
        private void RestoreFromSave(RunSaveData data)
        {
            var levelConfig = Global.LevelConfigs[data.difficultyIndex];
            GameUI.ShowLevelText(levelConfig.LevelName, duration: 3);

            // 1. 按存档重建布局网格（房间中心仍固定槽位中心，走廊绘制逻辑复用）
            foreach (var entry in data.rooms)
            {
                var roomType = (RoomType)entry.roomType;
                var roomConfig = RoomConfig.FromTileRows(roomType, entry.tileRows);
                var doorDirs = new HashSet<Direction>();
                foreach (var d in entry.doorDirs)
                    doorDirs.Add((Direction)d);

                var genConfig = new RoomGenerateConfig
                {
                    roomNode = new RoomNode(roomType),
                    roomPosX = entry.gridX,
                    roomPosY = entry.gridY,
                    doorDirections = doorDirs,
                    roomConfig = roomConfig,
                };
                DynamicDoorLayout[entry.gridX, entry.gridY] = genConfig;
            }

            // 2. 生成房间实例（不重置 Global.currentRoom，由后面按玩家坐标设置）
            DynamicDoorLayout.ForEach((x, y, cfg) =>
            {
                var entry = data.rooms.Find(r => r.gridX == x && r.gridY == y);
                var room = CreateRoomFromSave(x, y, cfg, entry);
                RoomGrid[x, y] = room;
            });

            // 3. 走廊与阴影
            GenerateCorridors();
            ShadowCaster2DGenerator.Generate(wallTilemap);

            // 4. 装饰痕迹：已完成的战斗房间生成血迹/尸体，直观标识房间已清理
            foreach (var entry in data.rooms)
            {
                var state = (Room.RoomState)entry.roomState;
                if (state != Room.RoomState.Finished) continue;
                var room = RoomGrid[entry.gridX, entry.gridY];
                if (room != null)
                    FxManager.SpawnBattleTraces(room);
            }

            // 5. 全局状态恢复（血印、升级、武器、全局数值 —— 依赖先还原 Room 环境再做事件驱动）
            Global.ImportFrom(data);
            WeaponDataSystem.ImportFrom(data);
            PlayerUpgradeState.ImportFrom(data, id => UpgradeManager.Instance?.FindById(id));
            BloodSigilState.ImportFrom(data, id => BloodSigilManager.Instance?.FindById(id));

            // 6. 地面掉落物（含血印掉落）
            RunSaveService.RestoreDrops(data);

            // 7. 玩家位置（存档记录的是 grid 格坐标，转世界坐标放中心）
            Player.player1.transform.position = new Vector3(data.playerGridX + 0.5f, data.playerGridY + 0.5f, 0);
            Global.currentRoom = FindRoomAtWorldPos(Player.player1.transform.position);

            Room.FindRoom();
        }

        // 还原单个房间：复用 GenerateRoom 绘制瓦片与门，但用存档的房间模板，
        // 并按存档的房间状态设置（Finished 不生成敌人，非 Finished 重新生成满血敌人）
        private Room CreateRoomFromSave(int gridX, int gridY, RoomGenerateConfig cfg, RoomSaveEntry entry)
        {
            var startX = RoomStartPosX(cfg);
            var startY = RoomStartPosY(cfg);
            var savedState = (Room.RoomState)entry.roomState;
            var room = GenerateRoom(startX, startY, cfg.roomConfig, cfg);

            // GenerateRoom 的 HandleTileType 已对 'e'/'#'/'c'/'s' 做了生成，
            // 这里按存档的房间状态修正：
            if (savedState == Room.RoomState.Finished || savedState == Room.RoomState.Idle)
            {
                // 已完成房间：移除所有敌人；X-3 Boss 房需保证传送门可见（Boss 死后才显示传送门，
                // 读档还原时 Boss 不再生成，但传送门必须可通行）
                foreach (var enemy in room.GetEnemies().ToList())
                {
                    if (enemy is Component c && c != null)
                    {
                        if (enemy is BossBase boss && boss.portal != null)
                            boss.portal.SetActive(true);    // 直接显示 Boss 关联的隐藏传送门
                        Destroy(c.gameObject);
                    }
                }
                room.GetEnemies().Clear();
            }
            else if (cfg.roomNode.roomType == RoomType.BossRoom && savedState == Room.RoomState.Battle)
            {
                // 战斗中退出：回滚为未开始（Boss 满血、门未锁），下次进入重新触发战斗
                savedState = Room.RoomState.Init;
            }

            room.roomState = savedState;

            // 宝箱/商店按存档标记隐藏已收集项
            room.RestoreSaveState(entry);

            return room;
        }

        // 按世界坐标反查所在房间（用于还原玩家所在房间）
        private Room FindRoomAtWorldPos(Vector3 worldPos)
        {
            Room result = null;
            RoomGrid.ForEach((x, y, room) =>
            {
                if (result != null || room == null) return;
                if (worldPos.x >= room.LB.x && worldPos.x <= room.RT.x
                    && worldPos.y >= room.LB.y && worldPos.y <= room.RT.y)
                {
                    result = room;
                }
            });
            return result;
        }

        public void LoadNextLevel()
        {
            Global.currentDifficulty += 1;
            if (Global.currentDifficulty >= Global.LevelConfigs.Count)
            {
                UIKit.OpenPanel<UIGamePassPanel>();
            }
            else
            {
                GameUI.ShowLoadingPage(SceneManager.GetActiveScene().name);
            }
        }

        void Update()
        {
        }

        void OnDestroy()
        {
            instance = null;
        }
    }
}