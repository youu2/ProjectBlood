using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector2 direction;
    public float speed = 10.0f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 移动
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
    }
}
