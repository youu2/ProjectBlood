using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using QFramework;

public class EnemyBullet : MonoBehaviour
{
    public Vector2 direction;
    public float speed = 10.0f;
    public float damage = 10f; // 子弹伤害值

    void Update()
    {
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 碰撞到玩家时造成伤害
        if (collision.gameObject.CompareTag("Player"))
        {
            Player.player1.TakeDamage(damage);
        }

        // 无论碰撞到什么都销毁子弹
        this.DestroyGameObjGracefully();
    }
}
