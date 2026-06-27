# Blood 游戏架构设计文档

## 1. 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         游戏主架构 (Global)                                  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    全局状态层 (Global)                               │  │
│  │  静态类管理：HP、金币、当前关卡难度、游戏暂停状态                     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                              ▲                                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                  QFramework 框架层                                    │  │
│  │  AudioKit │ ResKit │ UIKit │ BindableProperty (响应式数据绑定)         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                              ▲                                            │
│  ┌───────────────────┐  ┌───────────────────────────────┐  ┌───────────┐   │
│  │      UI 层         │  │           业务逻辑层            │  │   数据层    │   │
│  │                   │  │                               │  │           │   │
│  │ UIGamePanel       │  │ Player                        │  │ RoomConfig│   │
│  │ UIGameOverPanel   │  │ Enemy 系统                    │  │ Waves     │   │
│  │ UIGamePassPanel   │  │   ├── Enemy（基类）            │  │ DynaGrid  │   │
│  │ UIGameStartPanel  │  │   └── EnemyFactory            │  │ LevelsConfig│  │
│  │ UIMap             │  │ Weapon 系统                    │  │           │   │
│  └───────────────────┘  │   ├── WeaponBase              │  └───────────┘   │
│                         │   ├── LifestealFeature        │                  │
│                         │   ├── GunClip / BloodBank     │                  │
│                         │   └── IWeapon 接口             │                  │
│                         │ MapController                 │                  │
│                         │   ├── RoomGrid                │                  │
│                         │   └── DynamicDoorLayout       │                  │
│                         │ WavesSystem                   │                  │
│                         │ DropManager                   │                  │
│                         │   ├── DropItem                │                  │
│                         │   ├── Shield / DirtyBlood     │                  │
│                         │   └── PureBlood (纯血)        │                  │
│                         │ ShieldState                   │                  │
│                         │ FxManager                     │                  │
│                         │ ShopItem（商店道具）            │                  │
│                         └───────────────────────────────┘                  │
│                              │                                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                     Unity 引擎层                                      │  │
│  │  Physics2D │ Tilemap │ Coroutine │ PlayerPrefs                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

***

## 2. 模块划分说明

| 模块                   | 职责说明                                    | 划分理由                                                               |
| -------------------- | --------------------------------------- | ------------------------------------------------------------------ |
| **Global**           | 管理全局游戏状态（HP、金币、当前关卡难度等），提供全局数据访问和操作接口 | 静态类集中管理游戏核心数据，使用 BindableProperty 实现响应式更新，确保 UI 与数据实时同步 |
| **Player**           | 负责玩家输入处理、移动、自动瞄准、武器切换和战斗逻辑              | 将玩家相关逻辑聚合在一个类中，便于维护和扩展，武器通过接口 (IWeapon) 实现解耦 |
| **Enemy 系统**          | 管理敌人 AI 行为（追击、攻击、死亡），包含多种敌人类型           | 由 Enemy 基类和 EnemyFactory 工厂组成，通过 IDamageable 接口统一伤害系统                 |
| **EnemyFactory**     | 单例工厂类，集中管理敌人预制体，按难度分数生成对应敌人             | 解耦敌人生成与配置，便于扩展新敌人类型，支持难度曲线控制                                       |
| **Weapon 系统**         | 管理武器切换、射击、换弹逻辑，支持多种武器类型                 | 由 WeaponBase 基类和多个武器特性组件组成，通过 IWeapon 接口抽象武器行为，提高可扩展性        |
| **MapController**    | 基于 BFS 算法生成程序化地图，管理房间布局和门的动态开关         | 将地图生成逻辑独立，便于调整地图算法和房间配置，支持动态门布局                                    |
| **LevelsConfig**     | 关卡配置系统，通过树形结构预设多种关卡布局，支持难度分级            | 替换硬编码地图布局，支持随机布局选择，增加重玩价值，便于调整关卡难度曲线                               |
| **WavesSystem**      | 管理敌人波次生成和难度递增                           | 实现动态敌人池机制，根据波次自动解锁更强敌人，控制战斗节奏                                      |
| **DropManager**      | 管理敌人掉落物生成（金币、脏血、护盾、纯血）                   | 集中处理掉落逻辑，便于调整掉落概率和物品类型，支持自动飞向玩家机制                                  |
| **PureBlood**        | 纯血道具的飞射、追踪和拾取治疗逻辑                       | 将吸血效果转化为可拾取道具，增加操作手感和视觉反馈，由 DropManager 管理生成                           |
| **ShopItem**         | 商店商品随机出货和购买逻辑                             | 提供金币消耗途径，增加游戏经济系统完整性                                               |
| **UIMap（小地图）**      | 小地图渲染、房间状态显示、玩家位置追踪                       | 实时显示玩家周围房间，渐进式发现机制，增强探索体验                                          |
| **ShieldState**      | 护盾激活、保护期、格挡次数和碎裂逻辑管理                      | 提供临时伤害抵挡能力，保护期机制增加策略深度                                             |
| **LifestealFeature** | 武器吸血特性、等级升级、吸血量计算                         | 以武器特性组件形式存在，支持武器成长，增加战斗续航能力                                    |
| **FxManager**        | 集中管理血迹、尸体、粒子等特效对象的生成与清理                 | 统一管理视觉特效对象，提供 ClearAllEffects 接口便于场景切换时批量清理                           |
| **UI 层**             | 管理游戏界面（主菜单、HUD、结算界面等）                   | 遵循 MVVM 模式，通过 BindableProperty 实现数据驱动 UI 更新                        |

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

> 详见：Global 静态类实现

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

### 设计 16：加载界面系统 ✅

使用 `SceneManager.LoadSceneAsync()` 实现异步场景加载，配合 `GameUI.ShowLoadingPage()` 显示过渡界面，支持 `allowSceneActivation=false` 暂停激活机制。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第12章「加载界面系统」

***

### 设计 17：关卡切换系统 ✅

采用场景重载方式实现关卡切换，通过 `Global.currentDifficulty` 静态变量传递进度，支持通关判定（`currentDifficulty >= LevelConfigs.Count`）。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第13章「关卡切换系统」

***

### 设计 18：全局暂停状态管理 ✅

通过 `Global.IsGamePaused` 静态变量统一管理游戏暂停状态，控制玩家操作权限，在 `MapController.Start()` 中重置确保新场景状态正确。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第14章「全局暂停状态管理」

***

### 设计 19：关卡名称显示系统 ✅

`GameUI.ShowLevelText()` 使用协程实现淡入→保持→淡出动画，自动去除"Level "前缀显示关卡编号。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第15章「关卡名称显示系统」

***

### 设计 20：特效管理系统（FxManager）✅

集中管理血迹、尸体、粒子效果等特效对象，提供 `ClearAllEffects()` 接口，场景重载时自动清理。

> 详细设计见：[功能说明文档](file:///e:/unity/project/ProjectBlood/Technical%20Documentation/功能说明文档.md) 第16章「特效管理系统」

***

### 设计 21：升级与成长系统 📋

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
| **单例模式**      | EnemyFactory、DropManager、FxManager、MapController | 确保全局唯一访问点 |
| **状态机模式**     | 敌人 AI 行为管理、纯血道具（Flying/Chasing）            | 清晰的状态转换逻辑 |
| **策略模式**      | 武器系统（IWeapon 接口）                           | 统一接口，灵活替换 |
| **观察者模式**     | BindableProperty 数据绑定                      | 自动响应状态变化  |
| **组件化模式**     | 武器特性系统（LifestealFeature、GunClip、BloodBank）   | 能力模块化组合   |
| **模板方法模式**    | Enemy 基类定义受伤/死亡流程，子类可重写具体实现                | 代码复用，扩展性强 |
| **工厂模式**      | EnemyFactory 按难度分数生成对应敌人类型               | 解耦敌人生成与配置，便于扩展新敌人类型 |

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

*文档版本：v1.5*\
*最后更新：2026-06-27*
