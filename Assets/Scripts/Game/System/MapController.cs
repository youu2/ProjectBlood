using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using UnityEngine.Tilemaps;
using System.Linq;
using QFramework;
using Unity.Collections;

namespace ProjectBlood
{
    public partial class MapController : ViewController
    {
        // Start is called before the first frame update
        public TileBase wall_0;
        public TileBase wall_1;
        public TileBase wall_2;
        public TileBase wall_3;
        public TileBase groundTile;
        public TileBase wallH0;
        public TileBase wallH1;
        public TileBase wallH2;
        public TileBase wallH3;
        // Floor 
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
        public GameObject Enemy;
        public Player player;
        public int currentRoomPosX = 0;
        public GameObject Portal;
        public static MapController instance;

        public class RoomGenerateConfig
        {
            public RoomNode roomNode;
            public HashSet<Direction> doorDirections { get; set; }
            public int roomPosX { get; set; }
            public int roomPosY { get; set; }
            // public int roomWidth;
            // public int roomHeight;
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
            // player.gameObject.SetActive(false); // Disable player at the start, will be enabled in Start() after instantiation
            // Enemy.gameObject.SetActive(false); // Disable enemy prefab at the start, will be enabled in Start() after instantiation
        }
        void Start()
        {
            // 隐藏所有现有的房间实例
            foreach (var room in FindObjectsOfType<Room>())
            {
                room.Hide();
            }

            

            // 全图布局
            // InitRoom -> NormalRoom -> NormalRoom -> ChestRoom -> BossRoom
            var layout = new RoomNode(RoomType.InitRoom);
            layout.NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.ChestRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.NormalRoom)
                        .NextRoom(RoomType.BossRoom);
                        
            // 加入动态房门布局
            // layout是根据RoomType来生成的，所以每个动态网格对应一个RoomType
            var dynamicDoorLayout = new DynaGrid<RoomGenerateConfig>();

            void GenerateRoomBFS(RoomNode roomNode, DynaGrid<RoomGenerateConfig> dynamicDoorLayout)
            {
                // 广度优先遍历生成房间
                // 先做无分支的路径
                var roomQueue = new Queue<RoomGenerateConfig>();
                roomQueue.Enqueue(new RoomGenerateConfig()
                {
                    roomNode = roomNode,
                    roomPosX = 0,
                    roomPosY = 0,
                    doorDirections = new HashSet<Direction>(),
                });

                while(roomQueue.Count > 0)
                {
                    var generateConfig = roomQueue.Dequeue();
                    dynamicDoorLayout[generateConfig.roomPosX, generateConfig.roomPosY] = generateConfig;
                    
                    // 获取可以延申的方向
                    List<Direction> validDirections = new List<Direction>();
                    // 检查四个方向是否有空房间
                    if(dynamicDoorLayout[generateConfig.roomPosX + 1, generateConfig.roomPosY] == null)// 右方  
                    {
                        validDirections.Add(Direction.Right);
                    }
                    if(dynamicDoorLayout[generateConfig.roomPosX - 1, generateConfig.roomPosY] == null)// 左方
                    {
                        validDirections.Add(Direction.Left);
                    }
                    if(dynamicDoorLayout[generateConfig.roomPosX, generateConfig.roomPosY + 1] == null)// 上方
                    {
                        validDirections.Add(Direction.Up);
                    }
                    if(dynamicDoorLayout[generateConfig.roomPosX, generateConfig.roomPosY - 1] == null)// 下方
                    {
                        validDirections.Add(Direction.Down);
                    }

                    foreach(var childRoom in generateConfig.roomNode.Children)
                    {
                        // 随机选择一个方向
                        var nextDirection = validDirections.GetRandomItem();
                        if(nextDirection == Direction.Right)
                        {
                            generateConfig.doorDirections.Add(nextDirection);
                            roomQueue.Enqueue(new RoomGenerateConfig()
                            {
                                roomPosX = generateConfig.roomPosX + 1,
                                roomPosY = generateConfig.roomPosY,
                                roomNode = childRoom,
                                doorDirections = new HashSet<Direction>() {Direction.Left},
                            });
                        }else if(nextDirection == Direction.Left)
                        {
                            generateConfig.doorDirections.Add(nextDirection);
                            roomQueue.Enqueue(new RoomGenerateConfig()
                            {
                                roomPosX = generateConfig.roomPosX - 1,
                                roomPosY = generateConfig.roomPosY,
                                roomNode = childRoom,
                                doorDirections = new HashSet<Direction>() {Direction.Right},
                            });
                        }else if(nextDirection == Direction.Up)
                        {
                            generateConfig.doorDirections.Add(nextDirection);
                            roomQueue.Enqueue(new RoomGenerateConfig()
                            {
                                roomPosX = generateConfig.roomPosX,
                                roomPosY = generateConfig.roomPosY + 1,
                                roomNode = childRoom,
                                doorDirections = new HashSet<Direction>() {Direction.Down},
                            });
                        }else if(nextDirection == Direction.Down)
                        {
                            generateConfig.doorDirections.Add(nextDirection);
                            roomQueue.Enqueue(new RoomGenerateConfig()
                            {
                                roomPosX = generateConfig.roomPosX,
                                roomPosY = generateConfig.roomPosY - 1,
                                roomNode = childRoom,
                                doorDirections = new HashSet<Direction>() {Direction.Up},
                            });
                        }
                    }


                    

                }
            }
            
            
            GenerateRoomBFS(layout, dynamicDoorLayout);

            dynamicDoorLayout.ForEach((x, y, roomGenerateConfig) =>
            {
                GenerateRoomByLayout(x, y, roomGenerateConfig);
            });

            // 依次生成所有房间布局
            void GenerateRoomByLayout(int x, int y, RoomGenerateConfig roomGenerateConfig)
            {
                var currentRoomPosX = x * (RoomConfig.InitRoom.roomMap.First().Length + 5);
                var currentRoomPosY = y * (RoomConfig.InitRoom.roomMap.Count + 5);

                if(roomGenerateConfig.roomNode.roomType == RoomType.InitRoom)
                {
                    GenerateRoom(currentRoomPosX,currentRoomPosY, RoomConfig.InitRoom, roomGenerateConfig);
                    currentRoomPosX += RoomConfig.InitRoom.roomMap.First().Length + 5; // 更新当前房间的X坐标，为下一个房间做准备
                }else if(roomGenerateConfig.roomNode.roomType == RoomType.NormalRoom)
                {
                    GenerateRoom(currentRoomPosX,currentRoomPosY, 
                    RoomConfig.normalRoomConfigList.GetRandomItem(), roomGenerateConfig);
                    currentRoomPosX += RoomConfig.InitRoom.roomMap.First().Length + 5;
                }else if(roomGenerateConfig.roomNode.roomType == RoomType.ChestRoom)
                {
                    GenerateRoom(currentRoomPosX,currentRoomPosY, RoomConfig.ChestRoom, roomGenerateConfig);
                    currentRoomPosX += RoomConfig.ChestRoom.roomMap.First().Length + 5;
                }else if(roomGenerateConfig.roomNode.roomType == RoomType.BossRoom)
                {
                    GenerateRoom(currentRoomPosX,currentRoomPosY, RoomConfig.BossRoom, roomGenerateConfig);
                    currentRoomPosX += RoomConfig.BossRoom.roomMap.First().Length + 5;
                }
            }

            // GenerateRoomByLayout(0, 0, layout);
            GenerateCorridor();
            // 绘制过道（还未支持随机房间布局）
            void GenerateCorridor(){
                dynamicDoorLayout.ForEach((x, y, roomGenerateConfig) =>
                {
                    var currentRoomPosX = x * (RoomConfig.InitRoom.roomMap.First().Length + 5);
                    var currentRoomPosY = y * (RoomConfig.InitRoom.roomMap.Count + 5);
                    var roomWidth = RoomConfig.InitRoom.roomMap.First().Length;
                    var roomHeight = RoomConfig.InitRoom.roomMap.Count;
                    if(roomGenerateConfig.doorDirections.Contains(Direction.Right))// 绘制水平过道
                    {
                        for(int i = 0; i < 5; i++)
                        {
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth + i, currentRoomPosY - roomHeight/2, 0), randFloor);
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth + i, currentRoomPosY - roomHeight/2 + 1, 0), randFloor);
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth + i, currentRoomPosY - roomHeight/2 - 1, 0), randFloor);

                            wallTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth + i, currentRoomPosY - roomHeight/2 + 2, 0), randWall);
                            wallTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth + i, currentRoomPosY - roomHeight/2 - 2, 0), randWall);
                        }
                    }

                    if (roomGenerateConfig.doorDirections.Contains(Direction.Up))// 绘制垂直过道
                    {
                        for(int i = 0; i < 5; i++)
                        {
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth/2, currentRoomPosY + i + 1, 0), randFloor);
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth/2 + 1, currentRoomPosY + i + 1, 0), randFloor);
                            floorTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth/2 - 1, currentRoomPosY + i + 1, 0), randFloor);

                            wallTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth/2 + 2, currentRoomPosY + i + 1, 0), randWall);
                            wallTilemap.SetTile(new Vector3Int(currentRoomPosX + roomWidth/2 - 2, currentRoomPosY + i + 1, 0), randWall);
                        }
                    }
                });
            }

            
        }

        // 生成房间的函数
        void GenerateRoom(int startPosX, int startPosY, RoomConfig roomConfig, RoomGenerateConfig roomGenerateConfig)
        {
            var roomWidth = roomConfig.roomMap.First().Length;
            var roomHeight = roomConfig.roomMap.Count;
            var roomCenter = new Vector2(0.5f + startPosX + roomWidth / 2, 0.5f + startPosY - roomHeight / 2);
            var roomObj = Room.InstantiateWithParent(this)
                            .WithRoomConfig(roomConfig)
                            .WithRoomGenerateConfig(roomGenerateConfig)
                            .Position(roomCenter).Show();

            // 房间碰撞器的大小，用于检测玩家是否进入房间,略小于房间大小
            roomObj.SelfBoxCollider2D.size = new Vector2(roomWidth-3.0f, roomHeight-3.0f);
            
            for (int i = 0; i < roomConfig.roomMap.Count; i++)
            {
                for (int j = 0; j < roomConfig.roomMap[i].Length; j++)
                {
                    var x = j + startPosX;
                    var y = startPosY-i;
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), randFloor); // 每个地方都要铺设地面
                    if (roomConfig.roomMap[i][j] == '2')
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), randWall);
                    }
                    else if (roomConfig.roomMap[i][j] == '1')
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), randWallH);
                    }
                    else if (roomConfig.roomMap[i][j] == 'P')
                    {
                        Player.player1.transform.position = new Vector3(x, y, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'e')
                    {
                        var EnemyPos = new Vector3(x, y, 0);
                        roomObj.AddEnemy(EnemyPos);
                        // var enemy = Instantiate(Enemy);
                        // enemy.transform.position = new Vector3(x, y, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'X')
                    {
                        var boss = Instantiate(Enemy); // 这里暂时使用Enemy,以后可以替换成Boss的prefab
                        boss.transform.position = new Vector3(x, y, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == '#')
                    {
                        var portal = Instantiate(Portal);
                        portal.transform.position = new Vector3(x, y, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'd')
                    {
                        // 创建门并设置属性
                        // var door = Door.InstantiateWithParent(roomObj)
                        // .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                        // .Hide();
                        // roomObj.AddDoor(door);

                        // 根据config的doorDirections判断是否需要绘制这个‘d’
                        var doorDistance = new Vector2(x + 0.5f, y + 0.5f) - roomCenter;
                        if(doorDistance.x.Abs() > doorDistance.y.Abs()) // 说明这个‘d’在左边或右边
                        {
                            if(doorDistance.x > 0 && roomGenerateConfig.doorDirections.Contains(Direction.Right))  // 说明这个‘d’在右边
                            {
                                var door = Door.InstantiateWithParent(roomObj)
                                .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                                .WithDirection(Direction.Right)
                                .Hide();
                                roomObj.AddDoor(door);
                            }else if(doorDistance.x < 0 && roomGenerateConfig.doorDirections.Contains(Direction.Left))  // 说明这个‘d’在左边
                            {
                                var door = Door.InstantiateWithParent(roomObj)
                                .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                                .WithDirection(Direction.Left)
                                .Hide();
                                roomObj.AddDoor(door);
                            }
                            else    // 说明这个‘d’不在路线规划内，绘制墙
                            {
                                wallTilemap.SetTile(new Vector3Int(x, y, 0), randWall);
                            }
                        }else    // 说明这个‘d’在上方或下方
                        {
                            if(doorDistance.y > 0 && roomGenerateConfig.doorDirections.Contains(Direction.Up))  // 说明这个‘d’在上方
                            {
                                var door = Door.InstantiateWithParent(roomObj)
                                .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                                .WithDirection(Direction.Up)
                                .Hide();
                                roomObj.AddDoor(door);
                            }else if(doorDistance.y < 0 && roomGenerateConfig.doorDirections.Contains(Direction.Down))  // 说明这个‘d’在下方
                            {
                                var door = Door.InstantiateWithParent(roomObj)
                                .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                                .WithDirection(Direction.Down)
                                .Hide();
                                roomObj.AddDoor(door);
                            }
                            else    // 说明这个‘d’不在路线规划内，绘制墙
                            {
                                wallTilemap.SetTile(new Vector3Int(x, y, 0), randWallH);
                            }
                        }
            


                    }
                    else if (roomConfig.roomMap[i][j] == 'c')
                    {
                        // 创建宝箱并设置属性
                        var chest = Chest.InstantiateWithParent(roomObj)
                        .Position2D(new Vector3(x + 0.5f, y + 0.5f, 0))
                        .Show();
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
        void OnDestroy()
        {
            instance = null;
        }
    }
}
