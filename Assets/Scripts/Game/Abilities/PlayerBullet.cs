using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 5.0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 移动
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);

        // 让子弹图案跟着方向转
        // 默认朝右是 0 度，所以直接用 Atan2 算角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0, 0, angle);
    }
}
