using UnityEngine;

namespace ProjectBlood
{
    // 血印结算效果基类（四要素之一，多态扩展点 + 注册机制）。
    //
    // 生命周期（由 BloodSigilState 在模块状态机中驱动，子类按需重写）：
    //   OnApply   —— 模块从"未激活"变为"激活"时调用一次（持续型效果在此生效：加属性/增伤/CD减免）
    //   OnRefresh —— 模块已激活时再次触发（限时刷新/叠层累加：持续时间由引擎重置，效果在此累加层数）
    //   OnRemove  —— 模块结束/血印移除时调用一次，与 OnApply 对称撤销
    //   OnImmediate —— 每次触发都立即执行的瞬时效果（护盾、免疫充能、献祭等），与激活状态无关
    //
    // 查询钩子（由引擎统一聚合，默认中性实现）：
    //   GetDamageMultiplier —— 输出伤害乘法系数
    //
    // 运行时状态规则：本类是 SO 资产（跨局共享），所有运行时数据必须写入传入的
    // BloodSigilModuleRuntime 状态袋，禁止写入自身字段。
    //
    // 扩展新结算效果：新建子类实现上述钩子 + [CreateAssetMenu] 注册到创建菜单，
    // 并在 BloodSigilOutcomeLibrary 中登记一个稳定的 OutcomeKey 即可被表格/JSON 引用。
    public abstract class BloodSigilOutcomeSO : ScriptableObject
    {
        // 结算效果的作用域，决定玩家场景重建（过传送门/读档）后是否需要随
        // BloodSigilState.OnPlayerSpawned 对新 Player 实例重新生效。
        //   PersistentState —— 效果写入静态/持久系统（PlayerUpgradeState 台账、Global 数值、
        //                      BloodBank 等），这些状态跨场景存活且已被关卡存档覆盖，
        //                      模块激活时只应用一次，场景重建/读档均不得重复应用，否则会无限叠加。
        //   PlayerInstance  —— 效果只作用于 Player 实例字段/组件（如自动瞄准开关），
        //                      新场景的 Player 是全新实例，需要每次生成后重新应用。
        public enum OutcomeScope
        {
            PersistentState = 0,
            PlayerInstance = 1,
        }

        [Tooltip("效果注册键（表格/JSON 驱动时用此键引用该效果资产，需在血印效果注册表中唯一）")]
        [SerializeField] private string outcomeKey;

        // 稳定的注册键；未填写时回落到资产名，保证总有可用标识
        public string OutcomeKey => string.IsNullOrWhiteSpace(outcomeKey) ? name : outcomeKey;

        // 作用域：默认持久状态（绝大多数数值类效果）；仅纯玩家实例效果重写为 PlayerInstance
        public virtual OutcomeScope Scope => OutcomeScope.PersistentState;

        // ===== 持续型生命周期（模块处于激活期间生效）=====

        public virtual void OnApply(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx) { }

        public virtual void OnRefresh(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx) { }

        public virtual void OnRemove(BloodSigilModuleRuntime rt) { }

        // ===== 瞬时结算（每次触发都执行）=====

        public virtual void OnImmediate(BloodSigilModuleRuntime rt, in BloodSigilFireContext ctx) { }

        // ===== 查询 =====

        public virtual float GetDamageMultiplier(BloodSigilModuleRuntime rt, WeaponType weapon) => 1f;
    }
}
