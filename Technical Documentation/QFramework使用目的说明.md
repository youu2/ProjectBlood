# QFramework 使用目的说明

## 一、什么是 QFramework？

QFramework 是一套 Unity 游戏开发框架，提供了一系列工具和设计模式，帮助开发者更高效地组织代码、管理资源和构建游戏架构。

---

## 二、为什么选择 QFramework？

### 1. 解决 Unity 原生开发的痛点

| 痛点 | Unity 原生方式 | QFramework 解决方案 |
|------|---------------|-------------------|
| UI 管理 | 手动查找引用、手动显示隐藏 | UIKit 统一管理，命名规范 |
| 资源加载 | 直接引用或 Resources.Load | ResKit 资源池管理，自动释放 |
| 音频播放 | AudioSource 手动管理 | AudioKit 一行代码播放音效 |
| 代码组织 | 脚本散乱、耦合严重 | 架构分层，职责清晰 |
| 数据绑定 | 手动更新 UI | BindableProperty 自动响应 |

### 2. 降低初学者门槛

```csharp
// Unity 原生：需要理解很多概念
AudioSource source = gameObject.AddComponent<AudioSource>();
source.clip = Resources.Load<AudioClip>("Sound/hit");
source.Play();
Destroy(source, clip.length);

// QFramework：一行代码
AudioKit.PlaySound("hit");
```

---

## 三、QFramework 核心模块

### 1. UIKit - UI 管理系统

**目的**：统一管理所有 UI 面板的创建、显示、隐藏和销毁。

```csharp
// 打开 UI 面板
UIKit.OpenPanel<UIGamePanel>();

// 关闭 UI 面板
UIKit.ClosePanel<UIGamePanel>();

// 获取 UI 面板
var panel = UIKit.GetPanel<UIGamePanel>();
```

**优势**：
- UI 面板自动管理生命周期
- 支持层级管理（背景层、UI层、顶层）
- 避免手动查找 GameObject 引用

---

### 2. ResKit - 资源管理系统

**目的**：统一管理资源加载和释放，避免内存泄漏。

```csharp
// 初始化（游戏启动时调用一次）
ResKit.Init();

// 加载资源
var prefab = ResKit.LoadAsset<GameObject>("Prefabs/Enemy");

// 使用对象池实例化
var enemy = ResKit.Allocate<GameObject>("Enemy");
```

**优势**：
- 资源引用计数管理
- 自动释放不再使用的资源
- 支持异步加载

---

### 3. AudioKit - 音频管理系统

**目的**：简化音效和背景音乐的播放控制。

```csharp
// 播放背景音乐
AudioKit.PlayMusic("bgm_battle");

// 播放音效
AudioKit.PlaySound("hit");

// 停止音乐
AudioKit.StopMusic();

// 设置音量
AudioKit.MusicVolume.Value = 0.5f;
```

**优势**：
- 自动管理 AudioSource 池
- 支持同时播放多个音效
- 音量控制统一管理

---

### 4. BindableProperty - 数据绑定

**目的**：当数据变化时自动通知 UI 更新，无需手动刷新。

```csharp
// 定义可绑定属性
public static BindableProperty<int> Coin = new BindableProperty<int>(0);

// 注册变化监听
Coin.Register(newValue => {
    coinText.text = newValue.ToString();
});

// 修改值时自动触发更新
Coin.Value += 100;  // UI 自动更新
```

**优势**：
- 数据与 UI 自动同步
- 减少手动更新代码
- 支持链式注册多个监听

---

### 5. 链式调用扩展方法

**目的**：简化 GameObject 的常用操作，代码更简洁易读。

```csharp
// Unity 原生写法
var enemy = Instantiate(prefab, parent);
enemy.transform.localPosition = new Vector3(x, y, 0);
enemy.transform.localScale = Vector3.one;
enemy.name = "Enemy_1";
enemy.SetActive(true);

// QFramework 链式写法
prefab.InstantiateWithParent(parent)
    .LocalPosition(x, y)
    .LocalScale(1)
    .Name("Enemy_1")
    .Show();
```

**优势**：
- 代码简洁，一行完成多个操作
- 可读性好，从上到下阅读
- 减少临时变量

---

### 6. Architecture - 架构分层

**目的**：将游戏逻辑分层，降低代码耦合度。

```
┌─────────────────────────────────────┐
│           Display Layer             │  ← UI、动画、特效
├─────────────────────────────────────┤
│           Logic Layer               │  ← 游戏逻辑、状态机
├─────────────────────────────────────┤
│           Data Layer                │  ← 数据存储、配置
├─────────────────────────────────────┤
│           System Layer              │  ← 输入、网络、存档
└─────────────────────────────────────┘
```

**优势**：
- 职责分离，便于维护
- 方便单元测试
- 团队协作更清晰

---

## 四、本项目中的实际应用

### 1. Global.cs - 全局状态管理

```csharp
public class Global : Architecture<Global>
{
    // 使用 BindableProperty 管理游戏状态
    public static BindableProperty<float> currentHP = new BindableProperty<float>(30.0f);
    public static BindableProperty<int> Coin = new BindableProperty<int>(0);
    public static BindableProperty<int> Level = new BindableProperty<int>(1);
}
```

### 2. UIGamePanel.cs - UI 数据绑定

```csharp
// 自动响应 HP 变化更新 UI
Global.currentHP.RegisterWithInitValue(hp => {
    HPText.text = "HP: " + hp;
});
```

### 3. Room.cs - 链式对象创建

```csharp
// 创建房间实例
var roomObj = Room.InstantiateWithParent(this)
    .WithRoomConfig(roomConfig)
    .Position(roomCenter)
    .Show();
```

---

## 五、学习路径建议

| 阶段 | 学习内容 | 时间 |
|------|----------|------|
| **入门** | UIKit、AudioKit 基础使用 | 1-2 天 |
| **进阶** | BindableProperty、链式调用 | 2-3 天 |
| **深入** | Architecture 架构设计 | 1 周 |
| **精通** | 自定义模块扩展、源码阅读 | 长期 |

---

## 六、总结

| 使用 QFramework 的理由 | 说明 |
|----------------------|------|
| **简化开发** | 减少重复代码，提高开发效率 |
| **规范架构** | 统一代码风格，降低耦合 |
| **易于维护** | 模块化设计，便于修改和扩展 |
| **降低门槛** | 封装复杂操作，初学者快速上手 |
| **社区支持** | 中文文档完善，问题容易解决 |

---

## 七、参考资源

- [QFramework 官方文档](https://qframework.cn/)
- [QFramework GitHub](https://github.com/liangxiegame/QFramework)
- [QFramework 视频教程](https://space.bilibili.com/3678386)
