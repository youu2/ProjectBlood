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
                    var portal = Instantiate(Portal);
                    portal.transform.position = worldPos;
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

        // 处理门的放置逻辑，根据房间连接方向决定是否放置门或墙 参数：x - 网格X坐标，y - 网格Y坐标，roomCenter - 房间中心，roomObj - 房间实例，roomGenerateConfig - 房间生成配置
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