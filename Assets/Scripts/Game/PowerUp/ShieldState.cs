using UnityEngine;
using System.Collections;

namespace ProjectBlood
{
    // 护盾状态管理类
    public class ShieldState
    {
        public bool IsActive { get; private set; }
        public bool IsInGracePeriod { get; private set; }
        public int RemainingBlocks { get; private set; }
        public int MaxBlocks { get; private set; }
        public float GracePeriodDuration { get; private set; }

        private SpriteRenderer shieldSprite;
        private Vector2 playerPosition;
        private MonoBehaviour coroutineRunner;

        public void Initialize(SpriteRenderer sprite, MonoBehaviour runner)
        {
            shieldSprite = sprite;
            coroutineRunner = runner;
        }

        public void Activate(int blockCount, float gracePeriodDuration)
        {
            IsActive = true;
            IsInGracePeriod = true;
            RemainingBlocks = blockCount;
            MaxBlocks = blockCount;
            GracePeriodDuration = gracePeriodDuration;
            
            if (shieldSprite != null)
            {
                shieldSprite.enabled = true;
                shieldSprite.gameObject.SetActive(true);
            }
            
            // 启动保护期协程
            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(GracePeriodCoroutine(gracePeriodDuration));
            }
        }

        private IEnumerator GracePeriodCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            EndGracePeriod();
        }

        public void EndGracePeriod()
        {
            IsInGracePeriod = false;
            
            // GracePeriod结束后检查是否应该碎裂
            if (RemainingBlocks <= 0)
            {
                Deactivate();
            }
        }

        // 处理伤害
        // 参数: playerPos - 玩家位置，用于播放特效
        // 返回: true表示护盾抵挡了伤害
        public bool HandleDamage(Vector2 playerPos)
        {
            if (!IsActive) return false;

            playerPosition = playerPos;
            
            // 无论是否在GracePeriod，都扣除RemainingBlocks
            RemainingBlocks--;

            // 播放格挡特效
            FxManager.PlayShieldBlockFX(playerPos);

            // 只有不在GracePeriod时才检查是否碎裂
            if (!IsInGracePeriod && RemainingBlocks <= 0)
            {
                Deactivate();
                // 播放碎裂特效
                FxManager.PlayShieldBreakFX(playerPos);
                // AudioKitManager.Instance.PlayOneShot("ShieldBreak");
            }
            else
            {
                AudioKitManager.Instance.PlayOneShot("ShieldBlock");
            }

            return true;
        }

        public void Deactivate()
        {
            AudioKitManager.Instance.PlayOneShot("ShieldBreak");
            IsActive = false;
            IsInGracePeriod = false;
            RemainingBlocks = 0;
            
            if (shieldSprite != null)
            {
                shieldSprite.enabled = false;
                shieldSprite.gameObject.SetActive(false);
            }
        }
    }
}
