using System.Collections;
using System.Collections.Generic;
using ProjectBlood;
using QFramework;
using UnityEngine;

public class Portal : InteractableBase
{
    // public MapController mapController;
    // public int targetRoomPosX; // 目标房间的X坐标

    protected override void DoInteract()
    {
        // 传送玩家到目标房间
        // 不再在此处停 BGM：加载界面期间保持播放，下一关加载完成后由 MapController.Start 淡出并切换
        MapController.instance.LoadNextLevel();
    }
}
