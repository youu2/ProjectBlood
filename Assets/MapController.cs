using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour
{
    // Start is called before the first frame update
    public TileBase groundTile;
    public Tilemap groundTilemap;
    public List<string> InitRoom = new List<string>()
    {
        "1111111111",
        "1000000001",
        "1000000001",
        "1000000001",
        "1000000001",
        "1000000001",
        "1000000001",
        "1000000001",
        "1000000001",
        "1111111111"
    };
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
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
