using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 10.0f;
    public float damage = 20.0f;

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

    // void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if (collision.gameObject.CompareTag("Enemy"))
    //     {
    //         //UIKit.OpenPanel<UIGamePassPanel>();
    //         collision.gameObject.GetComponent<Enemy>().TakeDamage(damage);
    //     }
    //     //UIKit.OpenPanel<UIGamePassPanel>();
    // }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //UIKit.OpenPanel<UIGamePassPanel>();
            collision.gameObject.GetComponent<Enemy>().TakeDamage(damage);
        }
        //UIKit.OpenPanel<UIGamePassPanel>();
    }
}
