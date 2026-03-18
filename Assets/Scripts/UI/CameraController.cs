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
        // 当前玩家移动方向
        Vector2 moveDirection = new Vector2(Player.player1.transform.position.x, Player.player1.transform.position.y);
        // 获取当前摄像机位置
        Vector3 currentCameraPosition = transform.position;

        // 摄像机缓动目标位置(调整e的系数越大越慢跟随)
        Vector3 targetPosition = Vector3.Lerp(currentCameraPosition, new Vector3(moveDirection.x, moveDirection.y, -10), 1.0f - Mathf.Exp(-3.0f * Time.deltaTime));
        targetPosition.z = -10; // 保持摄像机在正确的深度位置
        // 摄像机跟随玩家移动
        transform.position = targetPosition;
    }
}
