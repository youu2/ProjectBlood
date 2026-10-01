using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class Portal : MonoBehaviour
{
    // public MapController mapController;
    // public int targetRoomPosX; // 目标房间的X坐标

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 传送玩家到目标房间
            // 先设置为直接通关
            // UIKit.OpenPanel<UIGamePassPanel>();
            // AudioKitManager.Instance.PlayOneShot("WinMusic");
            // 不再在此处停 BGM：加载界面期间保持播放，下一关加载完成后由 MapController.Start 淡出并切换
            MapController.instance.LoadNextLevel();
        }
    }
}
