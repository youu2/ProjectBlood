using System.Collections;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 霰弹枪弹道矩形特效：纯视觉组件，无任何碰撞体与判定。
    /// 由 ShotGun 在开火当帧生成（伤害已由 OverlapBoxAll 即时结算完毕），
    /// 摆放到判定矩形的位置/角度/尺寸后播放淡出，结束后自毁。
    /// 预制体要求：SpriteRenderer（任意 1x1 比例矩形精灵）+ 本组件。
    /// </summary>
    public class ShotgunBlastFx : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.5f; // 淡出时长，与设计口径一致

        private SpriteRenderer _spriteRenderer;
        private Vector2 _baseSize;   // 精灵在 scale=1 时的自身尺寸，用于换算目标长宽
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseSize = _spriteRenderer.sprite.bounds.size;
        }

        /// <summary>按判定矩形的长宽拉伸自身并开始淡出。</summary>
        public void Play(float length, float width)
        {
            transform.localScale = new Vector3(length / _baseSize.x, width / _baseSize.y, 1f);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            Color color = _spriteRenderer.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - elapsed / fadeDuration;
                _spriteRenderer.color = color;
                yield return null;
            }
            // 用自毁而非回池：武器销毁（玩家死亡/切场景）时不会产生孤儿池对象，
            // 霰弹枪射速低（约 1 秒 5 个），GC 压力可忽略
            Destroy(gameObject);
        }
    }
}
