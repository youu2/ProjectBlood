using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using UnityEngine;

public class CameraControler : MonoBehaviour
{
    // Start is called before the first frame update
    public Player player;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 0.5f, transform.position.z);
    }
}
