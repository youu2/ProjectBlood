using QFramework;
using System.Collections;
using UnityEngine;

namespace ProjectBlood
{
    public class FireFlash
    {
        public void Flash(Vector2 position, Vector2 direction)
        {
            // 在position位置创建一个枪口火焰特效，并让它朝向direction方向
            Player.player1.FireFlash.Position2D(position);
            Player.player1.FireFlash.transform.right = direction; // 让枪口光效朝向射击方向
            Player.player1.FireFlash.Show(); // 播放枪口光效动画
            // 启动一个协程，在几帧后隐藏枪口光效
            Player.player1.StartCoroutine(HideFlashAfterFrames(2)); // 2帧
            
        }
        private IEnumerator HideFlashAfterFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null; // 等待一帧
            }
            Player.player1.FireFlash.Hide();
        }
    }
}