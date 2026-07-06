using UnityEngine;
using System.Collections;

namespace ProjectBlood
{
    // 护盾状态管理类
    public class ShieldState
    {
        private bool _isActive;
        private bool _isInvincible;
        private int _remainingBlocks;
        private int _maxBlocks;
        private float _invincibleDuration;
        private SpriteRenderer shieldSprite;
        private Vector2 playerPosition;
        private MonoBehaviour _coroutineRunner; // 需要一个MonoBehaviour来启动协程
        // btw，runner只是我喜欢游戏的命名，实际就是指Player对象（或者是我设想中还未实现的类玩家敌人Titan）

        public void Initialize(SpriteRenderer sprite, MonoBehaviour runner)
        {
            shieldSprite = sprite;
            _coroutineRunner = runner;
        }

        public void Activate(int blockCount, float gracePeriodDuration)
        {
            _isActive = true;
            _isInvincible = true;
            _maxBlocks = blockCount;
            _remainingBlocks = _maxBlocks;
            _invincibleDuration = gracePeriodDuration;

            if (shieldSprite != null)
            {
                shieldSprite.enabled = true;
                shieldSprite.gameObject.SetActive(true);
            }

            // 启动保护期协程
            if (_coroutineRunner != null)
            {
                _coroutineRunner.StartCoroutine(InvincibleCoroutine(_invincibleDuration));
            }
        }

        private IEnumerator InvincibleCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            EndInvincible();
        }

        public void EndInvincible()
        {
            _isInvincible = false;

            // Invincible结束后检查是否应该碎裂
            if (_remainingBlocks <= 0)
            {
                Deactivate();
            }
        }

        // 处理伤害，播放格挡特效并检查是否碎裂护盾
        public bool HandleDamage(Vector2 playerPos)
        {
            if (!_isActive) return false;

            playerPosition = playerPos;

            // 无论是否在Invincible，都扣除格挡次数
            _remainingBlocks--;

            // 播放格挡特效
            FxManager.PlayShieldBlockFX(playerPosition);

            // 只有不在Invincible时才检查是否碎裂
            if (!_isInvincible && _remainingBlocks <= 0)
            {
                Deactivate();
                // 播放碎裂特效
                FxManager.PlayShieldBreakFX(playerPosition);
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
            _isActive = false;
            _isInvincible = false;
            _remainingBlocks = 0;

            if (shieldSprite != null)
            {
                shieldSprite.enabled = false;
                shieldSprite.gameObject.SetActive(false);
            }
        }
    }
}
