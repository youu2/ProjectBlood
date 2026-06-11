using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 护盾状态管理类
    /// </summary>
    public class ShieldState
    {
        public bool IsActive { get; private set; }
        public bool IsInGracePeriod { get; private set; }
        public int RemainingBlocks { get; private set; }
        public int MaxBlocks { get; private set; }
        public float GracePeriodDuration { get; private set; }

        private SpriteRenderer shieldSprite;
        private Vector2 playerPosition;

        public void Initialize(SpriteRenderer sprite)
        {
            shieldSprite = sprite;
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

        /// <summary>
        /// 处理伤害
        /// </summary>
        /// <param name="playerPos">玩家位置，用于播放特效</param>
        /// <returns>返回true表示护盾抵挡了伤害</returns>
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
            }

            return true;
        }

        public void Deactivate()
        {
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
