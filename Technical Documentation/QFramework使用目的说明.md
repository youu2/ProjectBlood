# QFramework 使用说明

## 使用初衷

在项目立项阶段，我了解到了 QFramework 的存在，这是我第一次接触游戏开发框架。认识到框架能带来的好处，以及行业内框架开发的使用情况后，我决定尝试给项目引入QFramework。

选择 QFramework 的主要考量：

1. **中文文档**：提升了查阅的效率
2. **功能实用**：提供 UI 管理、音频管理、数据绑定等实用功能
3. **学习曲线平缓**：有完善的教程，可以逐步学习使用，无需掌握所有功能

初期对 `BindableProperty`、`ViewController` 等概念并不了解，只是抱着"能简化开发"的想法开始尝试。

## 使用情况

虽然项目与 QFramework 深度耦合，但并非所有模块都使用了 QF：

- **使用的模块**：`BindableProperty`（数据绑定）、`AudioKit`（音频管理）、`ViewController`（组件生命周期）
- **尝试部分使用的模块**：`UIKit`（UI面板管理）、`ResKit`（资源管理）等

## 解决的问题

在开发过程中，主要遇到的问题是音频管理：

- 每个武器挂载独立的 `AudioSource` 播放音效
- 切换武器时音效会突然中断，影响玩家体验

通过 `AudioKit` 配合自定义的 `AudioKitManager` 单例封装，解决了音频管理问题：

- 切枪时音效平滑过渡，不会突然中断
- 音量统一管理，便于调整
- 武器系统与音频系统解耦

***

## 已使用 API

### 1. BindableProperty

**功能**：响应式数据绑定，值变化时自动通知监听者

**使用场景**：管理全局游戏状态（HP、金币、等级等）

```csharp
// 定义可绑定属性
public static BindableProperty<float> currentHP = new BindableProperty<float>(30.0f);
public static BindableProperty<int> Coin = new BindableProperty<int>(0);

// 注册监听
Global.currentHP.Register(hp => {
    HPText.text = "HP: " + hp;
});

// 修改值自动触发更新
Global.Coin.Value += 100;
```

### 2. AudioKit

**功能**：简化音效和音乐播放

**使用场景**：播放攻击音效、格挡音效、背景音乐

```csharp
// 播放音效
AudioKit.PlaySound("ShieldBlock");

// 播放背景音乐
AudioKit.PlayMusic("bgm_battle");

// 停止音乐
AudioKit.StopMusic();
```

### 3. ViewController

**功能**：UI 组件生命周期管理

**使用场景**：武器组件、UI 面板等继承 ViewController

```csharp
public abstract class WeaponBase : ViewController, IWeapon
{
    // 自动获得 ViewController 生命周期
}
```

### 4. 链式调用扩展

**功能**：简化对象创建和属性设置

**使用场景**：实例化游戏对象、设置位置和状态

```csharp
prefab.InstantiateWithParent(parent)
    .LocalPosition(x, y)
    .LocalScale(1)
    .Name("Enemy_1")
    .Show();
```

***

## AudioKit封装说明

### AudioKitManager

封装 `AudioKit` 为单例，提供统一调用接口：

```csharp
public class AudioKitManager : IAudioManager
{
    public AudioPlayer PlayOneShot(AudioClip clip, float volume = 1f)
    {
        return AudioKit.PlaySound(clip, volume: volume);
    }
}
```

