using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using QFramework;
using Unity.Collections;

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

        // 动态门布局网格，存储每个房间的生成配置（房间节点、门方向、网格坐标）
        public DynaGrid<RoomGenerateConfig> DynamicDoorLayout { get; private set; }
        // 房间实例网格，存储已生成的房间对象引用
        public DynaGrid<Room> RoomGrid { get; private set; }

        public class RoomGenerateConfig
        {
            public RoomNode roomNode;
            public HashSet<Direction> doorDirections { get; set; }
            public int roomPosX { get; set; }
            public int roomPosY { get; set; }
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
            HideAllExistingRooms();
            InitializeLevel(Level1_1.Config);
        }

        // 隐藏场景中所有已存在的房间实例
        private void HideAllExistingRooms()
        {
            foreach (var room in FindObjectsOfType<Room>())
            {
                room.Hide();
            }
        }

        // 初始化关卡，生成房间布局和连接通道 参数：levelConfig - 关卡配置
        private void InitializeLevel(LevelsConfig levelConfig)
        {
            var layout = levelConfig.InitRoom;

            GenerateRoomLayoutBFS(layout);
            GenerateRoomsFromLayout();
            GenerateCorridors();

            Room.FindRoom();
        }

        // 使用广度优先搜索(BFS)生成房间布局，通过预测权重控制分支方向，避免生成死路 参数：rootRoom - 根房间节点（起始房间）
        private void GenerateRoomLayoutBFS(RoomNode rootRoom)
        {
            int predictWeight = 0;

            while (!TryGenerateRoomLayout(rootRoom, predictWeight))
            {
                predictWeight++;
                DynamicDoorLayout.Clear();
            }
        }

        // 尝试生成房间布局的核心算法 参数：rootRoom - 根房间节点，predictWeight - 预测权重（越高越倾向选择最优方向） 返回是否成功生成布局
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

                if (generateConfig.roomNode.Children.Count > validDirections.Count)
                {
                    Debug.LogWarning("没有足够的可以延伸的方向");
                    return false;
                }

                foreach (var childRoom in generateConfig.roomNode.Children)
                {
                    var directionsWithCount = LevelGenHelper.PredictDirectionWithCount(
                        generateConfig.roomPosX, generateConfig.roomPosY, DynamicDoorLayout);

                    directionsWithCount.Sort((a, b) => b.Count - a.Count);

                    if (directionsWithCount.Count == 0)
                    {
                        Debug.LogWarning("没有可以延伸的方向");
                        return false;
                    }

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
                return directionsWithCount.First().Direction;
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

        // 根据房间类型创建房间实例 参数：gridX - 网格X坐标，gridY - 网格Y坐标，roomGenerateConfig - 房间生成配置 返回创建的房间实例
        private Room CreateRoomByType(int gridX, int gridY, RoomGenerateConfig roomGenerateConfig)
        {
            var currentRoomPosX = gridX * (RoomConfig.InitRoom.roomMap.First().Length + 5);
            var currentRoomPosY = gridY * (RoomConfig.InitRoom.roomMap.Count + 5);

            switch (roomGenerateConfig.roomNode.roomType)
            {
                case RoomType.InitRoom:
                    var initRoom = GenerateRoom(currentRoomPosX, currentRoomPosY, RoomConfig.InitRoom, roomGenerateConfig);
                    Global.currentRoom = initRoom;
                    initRoom.roomState = Room.RoomState.Finished;
                    return initRoom;

                case RoomType.NormalRoom:
                    return GenerateRoom(currentRoomPosX, currentRoomPosY, RoomConfig.normalRoomConfigList.GetRandomItem(), roomGenerateConfig);

                case RoomType.ChestRoom:
                    return GenerateRoom(currentRoomPosX, currentRoomPosY, RoomConfig.ChestRoom, roomGenerateConfig);

                case RoomType.ShopRoom:
                    return GenerateRoom(currentRoomPosX, currentRoomPosY, RoomConfig.ShopRoom, roomGenerateConfig);

                case RoomType.BossRoom:
                    return GenerateRoom(currentRoomPosX, currentRoomPosY, RoomConfig.BossRoom, roomGenerateConfig);

                default:
                    return null;
            }
        }

        // 生成房间之间的连接通道
        private void GenerateCorridors()
        {
            DynamicDoorLayout.ForEach((x, y, roomGenerateConfig) =>
            {
                var currentRoomPosX = x * (RoomConfig.InitRoom.roomMap.First().Length + 5);
                var currentRoomPosY = y * (RoomConfig.InitRoom.roomMap.Count + 5);
                var roomWidth = RoomConfig.InitRoom.roomMap.First().Length;
                var roomHeight = RoomConfig.InitRoom.roomMap.Count;

                if (roomGenerateConfig.doorDirections.Contains(Direction.Right))
                {
                    DrawHorizontalCorridor(currentRoomPosX, currentRoomPosY, roomWidth, roomHeight);
                }

                if (roomGenerateConfig.doorDirections.Contains(Direction.Up))
                {
                    DrawVerticalCorridor(currentRoomPosX, currentRoomPosY, roomWidth, roomHeight);
                }
            });
        }

        // 绘制水平通道（向右延伸） 参数：roomPosX - 房间X坐标，roomPosY - 房间Y坐标，roomWidth - 房间宽度，roomHeight - 房间高度
        private void DrawHorizontalCorridor(int roomPosX, int roomPosY, int roomWidth, int roomHeight)
        {
            int corridorY = roomPosY - roomHeight / 2;

            for (int i = 0; i < 5; i++)
            {
                floorTilemap.SetTile(new Vector3Int(roomPosX + roomWidth + i, corridorY, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(roomPosX + roomWidth + i, corridorY + 1, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(roomPosX + roomWidth + i, corridorY - 1, 0), randFloor);

                wallTilemap.SetTile(new Vector3Int(roomPosX + roomWidth + i, corridorY + 2, 0), randWall);
                wallTilemap.SetTile(new Vector3Int(roomPosX + roomWidth + i, corridorY - 2, 0), randWall);
            }
        }

        // 绘制垂直通道（向上延伸） 参数：roomPosX - 房间X坐标，roomPosY - 房间Y坐标，roomWidth - 房间宽度，roomHeight - 房间高度
        private void DrawVerticalCorridor(int roomPosX, int roomPosY, int roomWidth, int roomHeight)
        {
            int corridorX = roomPosX + roomWidth / 2;

            for (int i = 0; i < 5; i++)
            {
                floorTilemap.SetTile(new Vector3Int(corridorX, roomPosY + i + 1, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(corridorX + 1, roomPosY + i + 1, 0), randFloor);
                floorTilemap.SetTile(new Vector3Int(corridorX - 1, roomPosY + i + 1, 0), randFloor);

                wallTilemap.SetTile(new Vector3Int(corridorX + 2, roomPosY + i + 1, 0), randWall);
                wallTilemap.SetTile(new Vector3Int(corridorX - 2, roomPosY + i + 1, 0), randWall);
            }
        }

        // 生成单个房间的具体实现，根据房间配置绘制墙体、地板和放置游戏对象 参数：startPosX - 起始X坐标，startPosY - 起始Y坐标，roomConfig - 房间配置，roomGenerateConfig - 房间生成配置 返回生成的房间实例
        Room GenerateRoom(int startPosX, int startPosY, RoomConfig roomConfig, RoomGenerateConfig roomGenerateConfig)
        {
            var roomWidth = roomConfig.roomMap.First().Length;
            var roomHeight = roomConfig.roomMap.Count;
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

            foreach (var room in FindObjectsOfType<Room>())
            {
                Destroy(room.gameObject);
            }

            wallTilemap.ClearAllTiles();
            floorTilemap.ClearAllTiles();

            RoomGrid.Clear();
            DynamicDoorLayout.Clear();

            InitializeLevel(Level1_2.Config);
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