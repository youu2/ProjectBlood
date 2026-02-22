using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using UnityEngine.Tilemaps;
using System.Linq;

public class MapController : MonoBehaviour
{
    // Start is called before the first frame update
    public TileBase groundTile;
    public Tilemap groundTilemap;
    public GameObject Enemy;
    public Player player;
    public int currentRoomPosX = 0;
    public GameObject Portal;

    /*
        初始地图设计：10x10格，边界为（'1'），内部地面（' '） 玩家（'P'） 敌人（'e'）
    */
    public List<string> InitRoom{get ; set ;} = new List<string>()
    {
        "111111111111111111",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1        P       1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "111111111111111111"
    };

    public List<string> NormalRoom{get ; set ;} = new List<string>()
    {
        "111111111111111111",
        "1                1",
        "1                1",
        "1     e    e     1",
        "1                1",
        "1       e        1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "111111111111111111"
    };

    public List<string> BossRoom{get ; set ;} = new List<string>()
    {
        "111111111111111111",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1       #        1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "1                1",
        "111111111111111111"
    };
    void Awake()
    {
        // player.gameObject.SetActive(false); // Disable player at the start, will be enabled in Start() after instantiation
        // Enemy.gameObject.SetActive(false); // Disable enemy prefab at the start, will be enabled in Start() after instantiation
    }
    void Start()
    {
        GenerateRoom(currentRoomPosX,InitRoom);
        currentRoomPosX += InitRoom.First().Length + 5; // 更新当前房间的X坐标，为下一个房间做准备
        GenerateRoom(currentRoomPosX,NormalRoom);
        currentRoomPosX += InitRoom.First().Length + 5;
        GenerateRoom(currentRoomPosX,BossRoom);
    }

    // 生成房间的函数
    void GenerateRoom(int startPosX, List<string> room)
    {
        for (int i = 0; i < room.Count; i++)
        {
            for (int j = 0; j < room[i].Length; j++)
            {
                if (room[i][j] == '1')
                {
                    groundTilemap.SetTile(new Vector3Int(j + startPosX, -i, 0), groundTile);
                }
                else if (room[i][j] == 'P')
                {
                    Player.player1.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                }
                else if (room[i][j] == 'e')
                {
                    var enemy = Instantiate(Enemy);
                    enemy.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                }
                else if (room[i][j] == 'X')
                {
                    var boss = Instantiate(Enemy); // 这里暂时使用Enemy,以后可以替换成Boss的prefab
                    boss.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                }
                else if (room[i][j] == '#')
                {
                    var portal = Instantiate(Portal);
                    portal.transform.position = new Vector3(j + 0.5f + startPosX, -i + 0.5f, 0);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
