using UnityEngine;
using UnityEngine.UI;

namespace ProjectBlood
{
    /// <summary>
    /// 准心换弹遮罩可视化器：挂在 CursorManager 同一 GameObject 上，
    /// 驱动 NormalFiller（普通换弹进度）与 SpecialFiller（特殊换弹进度）两个 Image.Type=Filled 的子 Image。
    /// 颜色由用户直接在两个 Image 的 Inspector 上自行调整，本组件不接管颜色。
    ///
    /// 两个 Filler 均常驻可见，仅 fillAmount 变化：
    ///   - Idle：Normal=1 常驻填满，Special=0 占位可见
    ///   - 普通换弹中（阶段1）：Normal 0→1，Special 保持 0 占位
    ///   - 普通换弹正常结束（无特殊）：Normal 保持 1，Special 保持 0
    ///   - 普通换弹正常结束（已触发特殊）：Normal 保持 1，Special 0→1 推进
    ///   - 特殊换弹结束/中断：Normal 保持 1，Special 立即归零（仍可见占位）
    ///   - 普通换弹中断（如切枪）：Normal 立即填满；已触发特殊时立即转入阶段2推进 Special
    /// </summary>
    public class CursorReloadVisualizer : MonoBehaviour
    {
        [Tooltip("普通换弹遮罩：Image.Type=Filled，常驻可见。Idle 时 fillAmount=1")]
        [SerializeField] private Image normalFiller;
        [Tooltip("特殊换弹遮罩：Image.Type=Filled，常驻可见占位。仅阶段2推进，结束/中断后归零")]
        [SerializeField] private Image specialFiller;

        private enum State { Idle, NormalReloading, SpecialPending, SpecialReloading }
        private State _state = State.Idle;

        private float _normalReloadStartTime;
        private float _normalReloadDuration;

        private float _specialReloadStartTime;
        private float _specialReloadEndTime;
        // 普通换弹是否正在进行（Started 置位，Finished/Aborted 清除），
        // 供特殊换弹先结束时判断是否回退到阶段1继续推进
        private bool _normalReloadActive;

        void OnEnable()
        {
            ResetToIdle();

            WeaponBase.OnWeaponReloadStarted += HandleNormalReloadStarted;
            WeaponBase.OnWeaponReloadFinished += HandleNormalReloadFinished;
            WeaponBase.OnWeaponReloadAborted += HandleNormalReloadAborted;
            Player.OnSpecialReloadTriggered += HandleSpecialReloadTriggered;
            Player.OnSpecialReloadFinished += HandleSpecialReloadFinished;
        }

        void OnDisable()
        {
            WeaponBase.OnWeaponReloadStarted -= HandleNormalReloadStarted;
            WeaponBase.OnWeaponReloadFinished -= HandleNormalReloadFinished;
            WeaponBase.OnWeaponReloadAborted -= HandleNormalReloadAborted;
            Player.OnSpecialReloadTriggered -= HandleSpecialReloadTriggered;
            Player.OnSpecialReloadFinished -= HandleSpecialReloadFinished;
        }

        void Update()
        {
            float t = Time.time;
            switch (_state)
            {
                case State.NormalReloading:
                case State.SpecialPending:
                    {
                        // SpecialPending（已触发特殊换弹）时普通换弹仍在进行，必须继续推进 NormalFiller，
                        // 否则双击R后遮罩会停滞到普通换弹结束才跳变
                        float p = _normalReloadDuration > 0f
                            ? Mathf.Clamp01((t - _normalReloadStartTime) / _normalReloadDuration)
                            : 1f;
                        if (normalFiller != null) normalFiller.fillAmount = p;
                        break;
                    }
                case State.SpecialReloading:
                    {
                        float duration = _specialReloadEndTime - _specialReloadStartTime;
                        float p = duration > 0f
                            ? Mathf.Clamp01((t - _specialReloadStartTime) / duration)
                            : 1f;
                        if (specialFiller != null) specialFiller.fillAmount = p;
                        break;
                    }
            }
        }

        private void ResetToIdle()
        {
            if (normalFiller != null) normalFiller.fillAmount = 1f;
            if (specialFiller != null) specialFiller.fillAmount = 0f;
            _state = State.Idle;
        }

        private void HandleNormalReloadStarted(WeaponBase weapon)
        {
            _normalReloadActive = true;
            _normalReloadStartTime = Time.time;
            _normalReloadDuration = weapon != null ? weapon.ReloadDuration : 1.5f;
            if (normalFiller != null) normalFiller.fillAmount = 0f;
            // 阶段1进行中 SpecialFiller 保持 0% 占位可见，不隐藏
            if (specialFiller != null) specialFiller.fillAmount = 0f;
            _state = State.NormalReloading;
        }

        private void HandleNormalReloadFinished(WeaponBase weapon)
        {
            _normalReloadActive = false;
            if (normalFiller != null) normalFiller.fillAmount = 1f;

            if (_state == State.SpecialPending)
            {
                StartSpecialFiller();
            }
            else
            {
                _state = State.Idle;
            }
        }

        private void HandleNormalReloadAborted(WeaponBase weapon)
        {
            // 普通换弹中断（如切枪）：Normal 立即填满。
            // 若已触发特殊换弹（SpecialPending），其协程并不随切枪取消，
            // 视为"普通换弹阶段结束"，立即转入阶段2推进 SpecialFiller
            _normalReloadActive = false;
            if (normalFiller != null) normalFiller.fillAmount = 1f;
            if (_state == State.SpecialPending)
            {
                StartSpecialFiller();
            }
            else
            {
                _state = State.Idle;
            }
        }

        private void HandleSpecialReloadTriggered(float delay)
        {
            // 阶段2结束时刻 = 当前时间 + delay（delay = specialReloadDelay）
            _specialReloadEndTime = Time.time + delay;

            switch (_state)
            {
                case State.NormalReloading:
                case State.SpecialPending:
                    // 阶段1仍在跑：等其结束后再切换到 SpecialFiller。
                    // 窗口内重复按R会重启特殊换弹协程并重复广播本事件，此处仅刷新 endTime
                    _state = State.SpecialPending;
                    break;
                case State.SpecialReloading:
                    // 阶段2进行中收到重复触发 = 协程重启重新计时，SpecialFiller 从 0 重新填充
                    _specialReloadStartTime = Time.time;
                    if (specialFiller != null) specialFiller.fillAmount = 0f;
                    break;
                default:
                    // 阶段1已结束：立即开始 SpecialFiller
                    StartSpecialFiller();
                    break;
            }
        }

        private void StartSpecialFiller()
        {
            _specialReloadStartTime = Time.time;
            if (Time.time >= _specialReloadEndTime)
            {
                if (specialFiller != null) specialFiller.fillAmount = 0f;
                _state = State.Idle;
                return;
            }
            if (specialFiller != null) specialFiller.fillAmount = 0f;
            _state = State.SpecialReloading;
        }

        private void HandleSpecialReloadFinished()
        {
            // 特殊换弹结束/中断：Special 立即归零（保持可见占位）
            if (specialFiller != null) specialFiller.fillAmount = 0f;
            if (_state == State.SpecialReloading)
            {
                _state = State.Idle;
            }
            else if (_state == State.SpecialPending)
            {
                // 特殊换弹先于普通换弹结束（ReloadTime > delay 的武器）或被左键中断：
                // 普通换弹仍在进行，回退阶段1继续推进 NormalFiller
                _state = _normalReloadActive ? State.NormalReloading : State.Idle;
            }
        }
    }
}