using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectBlood;
using QFramework;

public class Portal : MonoBehaviour
{
    public MapController mapController;
    public int targetRoomPosX; // 目标房间的X坐标

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 传送玩家到目标房间
            // 先设置为直接通关
            UIKit.OpenPanel<UIGamePassPanel>();
        }
    }
}
