using System.Collections;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public class FireFlash
    {
        bool showedFlash = false;
        public void Flash(Vector2 position, Vector2 direction, float duration = 0.1f)
        {
            // 只在第一帧设置位置和方向，使枪火不再跟随枪口上抬移动
            if (!showedFlash)
            {
                // 在枪口位置激活枪口火焰特效，并让它朝向瞄准方向
                Player.player1.FireFlash.Position2D(position);
                Player.player1.FireFlash.transform.right = direction; // 让枪口光效朝向射击方向
                Player.player1.FireFlash.Show(); // 播放枪口光效动画
                showedFlash = true;
            }

            // 启动一个协程，在几帧后隐藏枪口光效
            Player.player1.StartCoroutine(HideFlashAfterFrames(duration));

        }
        private IEnumerator HideFlashAfterFrames(float duration)
        {
            yield return new WaitForSeconds(duration);
            Player.player1.FireFlash.Hide();
            showedFlash = false;
        }
    }
}