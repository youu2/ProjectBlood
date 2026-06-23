# Blood 游戏架构设计文档

## 1. 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         游戏主架构 (Global)                                  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                  QFramework 框架层                                    │  │
│  │  AudioKit │ ResKit │ UIKit │ BindableProperty (响应式数据绑定)         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                              ▲                                            │
│  ┌───────────────────┐  ┌───────────────────────────────┐  ┌───────────┐   │
│  │      UI 层         │  │           业务逻辑层            │  │   数据层    │   │
│  │                   │  │                               │  │           │   │
│  │ UIGamePanel       │  │ PlayerController              │  │ Global    │   │
│  │ UIGameOver        │  │ EnemySystem                   │  │ RoomConfig│   │
│  │ UIGamePass        │  │ WeaponSystem                  │  │ Waves     │   │
│  │ UIGameStart       │  │   ├── LifestealFeature        │  │           │   │
│  │ UIMap             │  │   └── GunClip / BloodBank     │  └───────────┘   │
│  └───────────────────┘  │ MapController                 │                  │
│                         │   ├── RoomGrid                │                  │
│                         │   └── DynamicDoorLayout       │                  │
│                         │ WaveSystem                    │                  │
│                         │ DropManager                   │                  │
│                         │   ├── Shield / DirtyBlood     │                  │
│                         │   ├── PureBlood (纯血)        │                  │
│                         │   └── Coin (金币)              │                  │
│                         │ ShieldState                   │                  │
│                         │ ShopSystem                    │                  │
│                         │   └── ShopItem (商品池随机)    │                  │
│                         └───────────────────────────────┘                  │
│                              │                                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                     Unity 引擎层                                      │  │
│  │  Physics2D │ Tilemap │ Coroutine │ PlayerPrefs │ DynaGrid            │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

***

## 2. 模块划分说明

| 模块                   | 职责说明                                    | 划分理由                                                               |
| -------------------- | --------------------------------------- | ------------------------------------------------------------------ |
| **Global**           | 管理全局游戏状态（HP、经验、金币、等级、传承点），提供全局数据访问和操作接口 | 采用单例模式集中管理游戏核心数据，使用 BindableProperty 实现响应式更新，确保 UI 与数据实时同步，降低模块间耦合 |
| **PlayerController** | 负责玩家输入处理、移动、自动瞄准、武器切换和战斗逻辑              | 将玩家相关逻辑聚合，便于维护和扩展，武器系统通过接口 (IWeapon) 实现解耦                          |
| **EnemySystem**      | 管理敌人 AI 行为（追击、攻击、死亡），包含多种敌人类型           | 采用状态机模式实现复杂敌人行为，通过 IDamageable 接口统一伤害系统                            |
| **EnemyFactory**     | 单例工厂类，集中管理敌人预制体，按难度分数生成对应敌人             | 解耦敌人生成与配置，便于扩展新敌人类型，支持难度曲线控制                                       |
| **WeaponSystem**     | 管理武器切换、射击、换弹逻辑，支持多种武器类型                 | 定义 IWeapon 接口抽象武器行为，通过特性系统 (Features) 扩展武器能力，提高可扩展性                |
| **MapGenerator**     | 基于 BFS 算法生成程序化地图，管理房间布局和连接              | 将地图生成逻辑独立，便于调整地图算法和房间配置，支持动态门布局                                    |
| **LevelsConfig**     | 关卡配置系统，通过树形结构预设多种关卡布局，支持难度分级            | 替换硬编码地图布局，支持随机布局选择，增加重玩价值，便于调整关卡难度曲线                               |
| **WaveSystem**       | 管理敌人波次生成和难度递增                           | 实现动态敌人池机制，根据波次自动解锁更强敌人，控制战斗节奏                                      |
| **DropManager**      | 管理敌人掉落物生成（经验、金币、脏血、护盾、纯血）               | 集中处理掉落逻辑，便于调整掉落概率和物品类型，支持自动飞向玩家机制                                  |
| **PureBloodSystem**  | 管理纯血道具的飞射、追踪和拾取治疗逻辑                     | 将吸血效果转化为可拾取道具，增加操作手感和视觉反馈                                          |
| **ShopSystem**       | 管理商店商品随机出货和购买逻辑                         | 提供金币消耗途径，增加游戏经济系统完整性                                               |
| **MiniMapSystem**    | 管理小地图渲染、房间状态显示、玩家位置追踪                   | 实时显示玩家周围房间，渐进式发现机制，增强探索体验                                          |
| **ShieldSystem**     | 管理护盾激活、保护期、格挡次数和碎裂逻辑                    | 提供临时伤害抵挡能力，保护期机制增加策略深度                                             |
| **LifestealSystem**  | 管理武器吸血特性、等级升级、吸血量计算                     | 支持武器成长，增加战斗续航能力，丰富战斗策略                                             |
| **UI 层**             | 管理游戏界面（主菜单、HUD、结算界面）                    | 遵循 MVVM 模式，通过 BindableProperty 实现数据驱动 UI 更新                        |

***

## 3. 模块设计说明

> **状态说明**：✅ 已实施 | 🔄 进行中 | 📋 计划中

### 设计 1：框架选择 - 为什么使用 QFramework ✅

为项目选择 QFramework 作为开发框架，主要看中文档，配套教程完善，封装了 UI、音频、数据绑定等常用功能，能省掉不少自己造轮子的工作，轻度尝试框架下的开发体验

> 详细设计见：[QFramework使用目的说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/QFramework使用目的说明.md)

***

### 设计 2：敌人行为 - 数据驱动设计 ✅

采用 `ShootingEnemy` + `LaserEnemy` 分层架构，将攻击逻辑集中到基类，不同敌人通过 Inspector 参数实现变体。

> 详细设计见：[敌人系统设计文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/敌人系统设计文档%20-%20美术表现与打击感.md)

### 设计 3：程序化地图生成 ✅

基于 BFS 算法生成程序化地图，支持动态门布局，通过预测权重机制优化路径选择。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第2章「地图生成系统」

***

### 设计 4：敌人 AI - 状态机模式 ✅

采用状态机模式管理敌人行为（追击→游走→攻击），使逻辑清晰且易于扩展。

> 详细设计见：[敌人系统设计文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/敌人系统设计文档%20-%20美术表现与打击感.md) 第8节「状态机实现」

***

### 设计 5：UI 数据绑定 - 响应式架构 ✅

使用 QFramework 的 `BindableProperty` 实现响应式数据绑定，简化 UI 更新逻辑。

> 详见：QFramework 的 BindableProperty 机制

***

### 设计 6：全局状态管理 ✅

采用 `PlayerPrefs` + `BindableProperty` 实现跨场景数据持久化和响应式更新。

> 详见：Global 单例类实现

***

### 设计 7：敌人系统美术表现与打击感 ✅

实现血液飞溅、血迹残留、尸体飞散、屏幕震动等打击感效果。

> 详细设计见：[敌人系统设计文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/敌人系统设计文档%20-%20美术表现与打击感.md)

***

### 设计 8：武器系统架构 ✅

采用双层继承架构（Automatic/SemiAutomatic）分离全自动/半自动逻辑，通过 `LifestealFeature` 实现武器吸血等级成长。

> 详细设计见：[武器系统设计文档 v1.0](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/武器系统设计文档%20v1.0%20\(2026-05-26\).md)

***

### 设计 9：小地图系统 ✅

`MapController` 管理房间网格，`UIMap` 实时渲染玩家周围房间，支持渐进式发现机制。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第1章「小地图系统」

***

### 设计 10：护盾系统 ✅

`ShieldState` 管理护盾激活、保护期（5秒）和格挡次数（5次），保护期内受击不消耗格挡。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第4章「护盾系统」

***

### 设计 11：血库系统 ✅

所有武器共享"脏血"资源，换弹时消耗血库强化武器伤害，耗尽仍可射击但伤害降低。

> 详细设计见：[核心决策文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/核心决策.md) 第4章「血库系统设计」

***

### 设计 12：纯血道具系统 ✅

吸血效果以可拾取道具呈现，先飞射再追踪玩家，增强视觉反馈和操作手感。

> 详细设计见：[核心决策文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/核心决策.md) 第5章「纯血道具系统设计」

***

### 设计 13：商店系统 ✅

商品池随机出货，商品定义复用 `DropItem.price` 字段，支持售罄机制。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第8章「商店系统」

***

### 设计 14：敌人工厂系统（EnemyFactory）✅

单例工厂管理敌人预制体，通过 `EnemyByScore(int score)` 按难度映射生成敌人。

> 详细设计见：[敌人系统设计文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/敌人系统设计文档%20-%20美术表现与打击感.md) 第6.3节

***

### 设计 15：关卡配置系统（LevelsConfig）✅

通过 `RoomNode` 树形结构 + 链式调用预设关卡布局，支持难度分级和随机布局选择。

> 详细设计见：[游戏流程设计文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/游戏流程设计.md) 第3.1节

***

### 设计 16：升级与成长系统 📋

**背景**：如何设计武器升级和玩家成长系统？

**计划方案**：

- 使用 `ScriptableObject` 配置升级数据
- 策略模式实现不同攻击模式
- 装饰器模式组合元素效果

**状态**：尚未实施，详见《武器系统设计文档 v1.0》第6节

***

## 4. 关键设计模式总结

| 设计模式          | 应用场景                                       | 实现效果      |
| ------------- | ------------------------------------------ | --------- |
| **单例模式**      | Global、DropManager、FxManager、MapController | 确保全局唯一访问点 |
| **状态机模式**     | 敌人 AI 行为管理、纯血道具（Flying/Chasing）            | 清晰的状态转换逻辑 |
| **策略模式**      | 武器系统（IWeapon 接口）                           | 统一接口，灵活替换 |
| **观察者模式**     | BindableProperty 数据绑定                      | 自动响应状态变化  |
| **组件化模式**     | 武器特性系统（LifestealFeature、GunClip）           | 能力模块化组合   |
| **模板方法模式**    | Enemy 基类定义受伤/死亡流程，子类可重写具体实现                | 代码复用，扩展性强 |
| **工厂模式**      | FxManager 统一创建特效实例、DropManager 生成掉落物       | 集中管理，便于维护 |
| **组合模式**      | BloodBank 与 WeaponBase 的集成                 | 资源管理解耦    |
| **迭代器模式**     | DynaGrid 遍历房间网格                            | 简化网格访问    |
| **生产者-消费者模式** | BFS房间生成算法                                  | 高效的层级遍历   |

***

## 5. 技术栈

| 类别        | 技术             | 说明                             |
| --------- | -------------- | ------------------------------ |
| **引擎**    | Unity 2D       | 游戏核心开发平台                       |
| **语言**    | C#             | 主要开发语言                         |
| **框架**    | QFramework     | 轻量级游戏框架（AudioKit/ResKit/UIKit） |
| **音频**    | FMOD           | 专业音频引擎集成                       |
| **数据持久化** | PlayerPrefs    | 本地数据存储                         |
| **设计模式**  | 状态机、观察者模式、单例模式 | 架构设计模式                         |
| **美术工具**  | LibreSprite    | 像素画创作                          |
| **音频工具**  | Reaper         | 音频编辑与音效设计                      |

***

*文档版本：v1.3*\
*最后更新：2026-06-23*
