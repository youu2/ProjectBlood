# 关卡进度存档系统设计方案

适用项目：ProjectBlood（Unity 2D Roguelike，QFramework）
文档性质：实施前设计方案，对接现有代码结构

---

## 一、关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 地图复现方式 | **序列化布局数据**，不用种子 | 布局 BFS、敌人生成、掉落全走 `UnityEngine.Random`（无种子），改种子制牵一发动全身；直接把每个房间的 `tileRows` 字符串数组存进档（23×23 房间约 530B，全图 ~15 房间 ≈ 8KB），还原时跳过 BFS 直接按档建图 |
| 存储介质 | **文件**（`Application.persistentDataPath/run_save.json`），不用 PlayerPrefs | 数据量超 PlayerPrefs 合理范围，且文件才能做原子写与备份 |
| 触发点 | `Room` 新增 `OnTriggerExit2D` | 与现有 `OnTriggerEnter2D` 对称；离开时 `roomState != Battle` 才写盘 |
| 防重复 | **快照哈希对比**，而非房间 id 标记 | 同一房间反复进出但状态有变化（捡了金币）时仍应保存；序列化后比哈希，一致则跳过 |
| 战斗退出回滚 | 机制天然达成 | 战斗房间只在 `Battle` 期间存在未保存状态，退出游戏时磁盘上仍是"进入战斗前"的快照，读档即回滚 |
| 血迹/尸体/弹壳 | **不入存档，读档时随机生成装饰痕迹** | 存档只需 `roomState == Finished` 一个标记；玩家看到的视觉效果一样，存档体积与序列化成本大幅下降（详见第四节） |

---

## 二、自动存档机制

### 触发链

```
玩家离开房间触发区（Room.OnTriggerExit2D）
  → RunSaveService.TryAutoSave(leftRoom)
  → 守卫：leftRoom.roomState != Battle && Global.currentRoom != null
  → 采集快照 → 序列化 → 与上次快照哈希对比
  → 一致：跳过（防重复）  不一致：原子写盘
```

### 额外存档点

- `MapController.InitializeLevel()` 完成后：新关卡开局即存档，形成关卡检查点
- `BtnQuit` 点击时：若当前非战斗，补一次存档（详见第五节）

### 采集时机注意

`Room.cs` 房间完成后掉落物会飞向玩家自动拾取（`autoCollectOnRoomFinish`）。存档延迟到"离开房间 + 0.5s"或等飞行结束后再采集，避免把飞行中的掉落物同时记为"地面存在"和"已入包"。

---

## 三、存档数据内容

新建 `Assets/Scripts/Game/Save/RunSaveData.cs`，纯 DTO + JsonUtility，单槽位 JSON。

头部：`version` · `savedAtUnix` · `crc32`

| 层 | 频率 | 内容 | 对接点 |
|---|---|---|---|
| 关卡标识 | 读档路由 | `difficultyIndex`（如 3 → 2-1）、`levelName` | `Global.currentDifficulty`、`Global.LevelConfigs` |
| 地图与房间 | 每关一次 | 房间列表：`gridPos`·门方向·`roomType`·`tileRows`；每房间 `roomState`、宝箱 `isCollected`、商品 `isCollected`、已发现房间 | `MapController` 布局结构、`Chest.cs`、`ShopItem.cs` |
| 玩家状态 | 频繁 | grid 坐标、`currentHP / INGAME_MAX_HP`、`Coin`、`Level / Exp / MAX_EXP`、武器列表（name·ammo·maxAmmo）、当前武器索引 | `Global.cs`、`WeaponDataSystem.weaponDataList`（`WeaponData` 已是纯数据）、`Player.currentWeapon` |
| 局内成长 | 中频 | `upgrades[]`（upgradeId·count）、武器伤害等级、血印（sigilId·模块计数/已用标记）、`RunElapsedSeconds`、`RemainingTime`、`BlazingCircleDamage`、`BCAttackInterval` | `PlayerUpgradeState`（需新增 Export/Import）、`BloodSigilState`（同上） |
| 地面实体 | 可限量 | 掉落物：type·gridPos（血印掉落附带 sigilId） | `DropManager` 需加"类型枚举 ↔ prefab 字段"映射表 |

### 需要补的稳定 id

- `UpgradeSO` 加显式 `id` 字段（用资产名重命名即炸档）
- `BloodSigilSO` 加显式 `id` 字段

### Boss 处理

Boss 在地图生成时 spawn。读档时 Boss 房 `roomState != Finished` → 重生成满血 Boss；已击杀 → 跳过。战斗中退出天然回滚为满血重打。

### 房间 id

用 `"{gridX},{gridY}"` 字符串，稳定且天然唯一。

---

## 四、装饰性血迹/尸体（替代痕迹序列化）

**目的**：让玩家直观看出一个房间是否已完成战斗，而不把痕迹数据塞进存档。

**做法**：读档还原路径中，遍历房间列表，对每个 `roomState == Finished` 的房间调用一次 `FxManager.SpawnBattleTraces(room)`：

- 血迹 3~6 处：随机位置 + 随机旋转 + 随机透明度（0.5~0.8），模拟干涸感
- 尸体 1~3 具：从该房间模板 `tileRows` 中 `'e'` 格对应的敌人类型里随机挑选，走 `FxManager.SpawnEnemyBody` 现有路径（生成的本来就是无 AI、无碰撞体的纯视觉尸体）

**位置规则**：在房间可达空格内随机（读 `tileRows` 排除 `'#'` 墙和 `'3'` 掩体格），避开门口 2 格范围。

**注意点**：

1. 装饰尸体统一降低透明度或调暗色调，与活敌人一眼区分（Boss 房未完成时里面是真 Boss，尤其需要）
2. 排序层级：血迹 < 尸体 < 玩家/掉落物；全部挂 FxManager 管理，场景重载时 `ClearAllEffects` 现有语义不受影响
3. 同一局内正常战斗留下的真实尸体/血迹不动，只有读档还原时才生成装饰痕迹

**对存档结构的影响**：删掉"地面实体"层中的尸体/血迹条目，其余不变。

---

## 五、按钮接线

### BtnQuit（暂停面板，引用已绑定）

```csharp
BtnQuit.onClick.AddListener(() =>
{
    // 战斗中不覆盖存档（保留进房前快照）；非战斗补一次存档
    if (Global.currentRoom == null || Global.currentRoom.roomState != RoomState.Battle)
        RunSaveService.SaveNow();
    this.CloseSelf();
    GameUI.ShowLoadingPage("GameStart");  // 场景重载即释放本关全部动态对象（现有机制）
});
```

### BtnContinueGame（主菜单，引用已存在于 UIGameStartPanel.Designer）

1. `RunSaveManager.TryLoad(out data)`：文件存在 + 版本匹配 + CRC32 校验通过，否则按钮隐藏/置灰
2. 通过后存入静态载荷 `PendingRestore.Data`，`ShowLoadingPage("InGame")`
3. `MapController.Start()` 检测载荷：走**还原路径**（按 tileRows 建图、恢复房间状态/宝箱/商品、重生成存活 Boss、铺地面掉落物、对每个 Finished 房间生成装饰痕迹、按 grid 坐标放玩家并设 `Global.currentRoom`）
4. 还原 `Global` 字段 → `PlayerUpgradeState.Import()` → `BloodSigilState.Import()` → 重建 `weaponDataList` 并切换到存档时的武器

### 生命周期规则

| 事件 | 存档动作 |
|---|---|
| BtnStartGame（新游戏） | 先 `DeleteSave()` |
| 玩家死亡 / 通关最终层 | `DeleteSave()`（Roguelike 永久死亡语义） |
| 过传送门进下一关 | `InitializeLevel` 完成后自动存新档 |

---

## 六、可靠性、加密与性能

**崩溃安全**：写 `run_save.tmp` → `File.Move` 原子替换正式档；写前把旧档复制为 `.bak`。读档校验失败回退 `.bak`，再失败视为无存档。写一半的截断文件永远不会被当作有效存档。

**校验与加密**：头部 `crc32`（对 payload 计算）做完整性校验（必须）。防篡改只做 XOR + 固定密钥混淆（约 10 行代码）；AES 对本地单机存档没有真实安全收益，不引入。

**版本控制**：头部 `version` 字段，与 `LegacySaveManager` 同一模式。开发期版本不匹配直接删档；发布后再加迁移分支。

**性能**：总量 <50KB，`JsonUtility.ToJson` + 写文件实测 <10ms，主线程同步写稳达 100ms 目标，无需异步线程和增量序列化。"增量保存"由快照哈希对比承担（无变化不写盘）。

---

## 七、实施顺序

1. `RunSaveData` DTO + `RunSaveManager`（读写/校验/版本/原子写/bak）
2. 各系统 `Export/Import` 接口（Global、Player/WeaponDataSystem、PlayerUpgradeState、BloodSigilState；两个 SO 加 id 字段）
3. `MapController` 还原路径（布局序列化 + 按档建图）
4. 房间/宝箱/商店/掉落物还原 **+ 装饰痕迹生成**：
   - 恢复各房间 `roomState`、宝箱/商品 `isCollected`、地面掉落物
   - `FxManager.SpawnBattleTraces(room)`：读 tileRows 找可达地面格，生成血迹 3~6 处 + 尸体 1~3 具（敌人取自该房间模板 `'e'` 格类型），避开门口 2 格
   - `MapController` 还原路径对每个 Finished 房间调用一次
5. `Room.OnTriggerExit2D` 触发器 + 按钮接线
6. 按第八节测试

---

## 八、测试清单

1. **触发准确性**：进出非战斗房 → 落档；宝箱房/商店房反复进出验证（战斗房门锁闭，战斗中离开不可达）
2. **战斗退出还原**：进战斗房打掉敌人半血 → 强杀进程 → 继续游戏 → 敌人满血、房间未开始、玩家状态 = 进房前
3. **防重复**：同一房间进出 5 次无操作 → 文件 mtime 只变 1 次；捡 1 金币再进出 → 再写 1 次
4. **跨平台**：`persistentDataPath` 各平台路径不同但 API 一致，JSON 无字节序问题；编辑器与打包后互拷存档文件验证
5. **断电恢复**：写档过程中强杀（可在 `Save` 里临时插 `Debug.Break` 模拟）→ 下次读档回退 `.bak`
6. **装饰痕迹**：读档后已完成房间存在血迹/尸体装饰，未完成房间没有；装饰尸体与活敌人视觉可区分
