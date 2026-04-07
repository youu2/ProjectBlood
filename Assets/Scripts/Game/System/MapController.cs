using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using UnityEngine.Tilemaps;
using System.Linq;
using QFramework;

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
        void Awake()
        {
            instance = this;
            // player.gameObject.SetActive(false); // Disable player at the start, will be enabled in Start() after instantiation
            // Enemy.gameObject.SetActive(false); // Disable enemy prefab at the start, will be enabled in Start() after instantiation
        }
        void Start()
        {
            GenerateRoom(currentRoomPosX,RoomConfig.InitRoom);
            currentRoomPosX += RoomConfig.InitRoom.roomMap.First().Length + 5; // 更新当前房间的X坐标，为下一个房间做准备
            GenerateRoom(currentRoomPosX,RoomConfig.NormalRoom);
            currentRoomPosX += RoomConfig.NormalRoom.roomMap.First().Length + 5;
            GenerateRoom(currentRoomPosX,RoomConfig.BossRoom);
        }

        // 生成房间的函数
        void GenerateRoom(int startPosX, RoomConfig roomConfig)
        {
            var roomWidth = roomConfig.roomMap.First().Length;
            var roomHeight = roomConfig.roomMap.Count;
            var roomCenter = new Vector2(0.15f + startPosX + roomWidth / 2, 1.3f - roomHeight / 2);
            var roomObj = Room.InstantiateWithParent(this).WithRoomConfig(roomConfig)
                              .Position(roomCenter).Show();

            // 房间碰撞器的大小，用于检测玩家是否进入房间,略小于房间大小
            roomObj.SelfBoxCollider2D.size = new Vector2(roomWidth-3.0f, roomHeight-3.0f);
            
            for (int i = 0; i < roomConfig.roomMap.Count; i++)
            {
                for (int j = 0; j < roomConfig.roomMap[i].Length; j++)
                {
                    floorTilemap.SetTile(new Vector3Int(j + startPosX, -i, 0), randFloor); // 每个地方都要铺设地面
                    if (roomConfig.roomMap[i][j] == '2')
                    {
                        wallTilemap.SetTile(new Vector3Int(j + startPosX, -i, 0), randWall);
                    }
                    else if (roomConfig.roomMap[i][j] == '1')
                    {
                        wallTilemap.SetTile(new Vector3Int(j + startPosX, -i, 0), randWallH);
                    }
                    else if (roomConfig.roomMap[i][j] == 'P')
                    {
                        Player.player1.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'e')
                    {
                        var EnemyPos = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                        roomObj.AddEnemy(EnemyPos);
                        // var enemy = Instantiate(Enemy);
                        // enemy.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'X')
                    {
                        var boss = Instantiate(Enemy); // 这里暂时使用Enemy,以后可以替换成Boss的prefab
                        boss.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == '#')
                    {
                        var portal = Instantiate(Portal);
                        portal.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                    }
                    else if (roomConfig.roomMap[i][j] == 'd')
                    {
                        // 创建门并设置属性
                        var door = Door.InstantiateWithParent(roomObj)
                        .Position2D(new Vector3(j + 0.656f + startPosX, -i + 0.683f, 0))
                        .Hide();
                        roomObj.AddDoor(door);
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
