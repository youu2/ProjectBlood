using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour
{
    // Start is called before the first frame update
    public TileBase groundTile;
    public Tilemap groundTilemap;
    public GameObject Enemy;
    public Player player;

    /*
        初始地图设计：10x10格，边界为（'1'），内部地面（' '） 玩家（'P'） 敌人（'e'）
    */
    public List<string> InitRoom{get ; set ;} = new List<string>()
    {
        "1111111111",
        "1        1",
        "1    e   1",
        "1        1",
        "1        1",
        "1        1",
        "1        1",
        "1    P   1",
        "1        1",
        "1111111111"
    };
    void Awake()
    {
        // player.gameObject.SetActive(false); // Disable player at the start, will be enabled in Start() after instantiation
        // Enemy.gameObject.SetActive(false); // Disable enemy prefab at the start, will be enabled in Start() after instantiation
    }
    void Start()
    {
        for (int i = 0; i < InitRoom.Count; i++)
        {
            for (int j = 0; j < InitRoom[i].Length; j++)
            {
                if (InitRoom[i][j] == '1')
                {
                    groundTilemap.SetTile(new Vector3Int(j, -i, 0), groundTile);
                }
                else if (InitRoom[i][j] == 'P')
                {
                    //var player1 = Instantiate(player);
                    Player.player1.transform.position = new Vector3(j + 0.5f, -i + 0.5f, 0);
                }
                else if (InitRoom[i][j] == 'e')
                {
                    var enemy = Instantiate(Enemy);
                    enemy.transform.position = new Vector3(j + 0.5f, -i + 0.5f, 0);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
