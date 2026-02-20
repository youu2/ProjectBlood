using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using UnityEngine;

public class CameraControler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(Player.player1 == null)
        {
            return;
        }
        transform.position = new Vector3(Player.player1.transform.position.x, Player.player1.transform.position.y + 0.5f, transform.position.z);
    }
}
