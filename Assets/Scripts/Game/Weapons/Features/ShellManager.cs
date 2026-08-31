using System.Collections;
using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;

namespace ProjectBlood
{
    public class ShellManager : MonoBehaviour
    {
        public float ShellVolume = 0.5f;
        public float delay2Release = 10f;
        public void PlayShellAnimation(Vector2 finalDirection, Transform weaponTransform)
        {
            gameObject.SetActive(true);
            StartCoroutine(ShellAnimation1(finalDirection, weaponTransform));
            // ShellAnimation2(finalDirection, weaponTransform);    // 抛壳QF解决方案
        }

        // 抛壳动画unity原生方案：
        protected IEnumerator ShellAnimation1(Vector2 finalDirection, Transform weaponTransform)
        {
            // var shell = DropManager.Instance.Shell.gameObject;
            // 生成弹壳
            // GameObject shellObj = Instantiate(shell, weaponTransform.position + (Vector3)finalDirection * 0.5f, Quaternion.identity);
            gameObject.SetActive(true);
            transform.SetPositionAndRotation(weaponTransform.position, weaponTransform.rotation);
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            // 初始速度,角速度,重力为1自由落体，持续0.5-1秒
            rb.gravityScale = 1f;
            Vector2 velocity = -finalDirection * Random.Range(1.6f, 3f) + Vector2.up * Random.Range(3f, 6f);
            rb.velocity = velocity;
            rb.angularVelocity = Random.Range(-500f, 500f);

            float delay1 = Random.Range(0.5f, 1f);
            yield return new WaitForSeconds(delay1);

            // 修改速度,重力为0.1,角速度,持续0.1-0.3秒,模拟弹壳落地弹跳一次
            rb.velocity = -finalDirection * Random.Range(0.6f, 2f) + Vector2.up * Random.Range(0.35f, 0.6f);
            rb.gravityScale = 0.15f;
            System.Random rand = new();
            int dir = rand.Next(2) == 0 ? 1 : -1;
            rb.angularVelocity = Random.Range(300f, 720f) * dir;
            AudioKitManager.Instance?.PlayOneShot($"bullet_shell ({Random.Range(1, 10 + 1)})", volume: ShellVolume);

            float delay2 = Random.Range(0.3f, 0.5f);
            yield return new WaitForSeconds(delay2);
            // 停止
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;

            // 延迟20秒后释放弹壳回池子
            yield return new WaitForSeconds(delay2Release);
            ShellPool.instance.shellPool.Release(gameObject);
        }

        // 使用QF ActionKit 的抛壳方案：
        private void ShellAnimation2(Vector2 finalDirection, Transform weaponTransform)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            var velocity = -finalDirection * Random.Range(1.6f, 3f) + Vector2.up * Random.Range(3f, 6f);  // 弹壳抛出速度(射击反方向+向上抛出)
            var spriteRander = GetComponent<SpriteRenderer>();
            rb.velocity = velocity;
            rb.angularVelocity = Random.Range(-720, 720);
            ActionKit.Sequence()
            .Delay(Random.Range(0.5f, 1f), () =>
            {
                rb.velocity = -finalDirection * Random.Range(0.6f, 2f) + Vector2.up * Random.Range(0.35f, 0.6f);
                rb.gravityScale = 0.1f;
                rb.angularVelocity = Random.Range(-720, 720);
                AudioKitManager.Instance?.PlayOneShot($"bullet_shell ({Random.Range(1, 10 + 1)})", volume: ShellVolume);
            })
            .Parallel(s =>
            {
                s.Delay(Random.Range(0.15f, 0.3f), () =>
                {
                    rb.angularVelocity = 0;
                    rb.gravityScale = 0;
                    rb.velocity = Vector2.zero;
                });
            }).Start(this);
        }
    }
}
