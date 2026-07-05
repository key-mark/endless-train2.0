# Codex Handoff

更新时间：2026-07-05

## 项目目标

本项目是 Godot 4 C# 开发的《无限列车 SLG》1 天可演示 Demo。目标是做出最小但完整的前期闭环：

```text
列车主界面 -> 查看资源和等级 -> 升级炮台 -> 前往废弃站点
-> 竖屏双轨道战斗 -> Boss 战 -> 胜利/失败结算
-> 返回列车 -> 资源和站点推进 -> 下一局更强
```

当前不要扩展成完整商业 SLG。所有新增功能都应服务于“列车升级、废弃站点战斗、资源结算、Demo 串联”。

## 技术栈

- 引擎：Godot 4.x
- 脚本：C#
- .NET：`net8.0`
- 项目文件：`Endless-Train2.0.csproj`
- 主场景：`res://scenes/Main.tscn`
- 全局状态：`GameManager` autoload，脚本为 `res://scripts/GameManager.cs`

`project.godot` 已配置：

- 内部玩法分辨率：`540 x 960`
- 竖屏比例：`9:16`
- Stretch Mode：`canvas_items`
- Stretch Aspect：`keep`

## 玩法范围

硬性规则：

- 战斗坐标以 `540 x 960` 为准。
- 战斗方向为自上而下：敌人和升级目标从屏幕上方出现，向下压向底部列车。
- 左轨中心 x = `175`，右轨中心 x = `365`。
- 底部横向列车固定不动。
- 玩家左右拖动英雄和炮台，二者共享同一个 x 坐标。
- 英雄和炮台自动向上射击。
- 炮台升级要影响下一次战斗火力。

不要做：

- 存档、联网、抽卡、背包、装备、复杂英雄养成。
- 科技树、建筑队列、真实 SLG 大地图、国战、联盟。
- 正式美术接入或复杂物理系统。

## 已完成内容

### Step 1：项目分辨率和基础场景

- 项目内部分辨率配置为 `540 x 960`。
- 主入口场景为 `scenes/Main.tscn`。
- 创建了 `TrainScreen` 和 `BattleScreen` 场景。
- TrainScreen 有标题、资源栏占位、列车预览、升级炮台按钮、前往废弃站点按钮。
- BattleScreen 有 540x960 战斗区域、两条纵向轨道、底部列车占位。

### Step 2：GameManager 和资源显示

- 创建 `scripts/GameManager.cs` 作为 autoload。
- 管理当前全局数据：
  - `Scrap`
  - `Fuel`
  - `Food`
  - `Parts`
  - `CannonLevel`
  - `TrainLevel`
  - `CurrentStation`
  - `TrainMaxHp`
  - `TrainCurrentHp`
- `TrainScreen` 显示资源、等级、站点、列车 HP。
- `Upgrade Cannon` 按钮会消耗 Scrap 并提升 `CannonLevel`。
- Scrap 不足时显示提示文本。
- `Go to Abandoned Station` 按钮进入 `BattleScreen`。

### Step 3：BattleScreen 布局和拖动

- BattleScreen 使用 `540 x 960` 玩法坐标。
- 两条轨道已可见：
  - 左轨约为 x = `175`
  - 右轨约为 x = `365`
- 底部列车基地固定。
- 创建了 Hero 和 Turret 灰盒视觉。
- 鼠标/触摸左右拖动时，Hero 和 Turret 通过 `PlayerRig` 共享同一个 x 坐标。
- 玩家横向移动范围配置在 `BattleScreen.cs`：
  - `PlayerMinX = 52`
  - `PlayerMaxX = 488`
- Hero 位于英雄判定线附近。
- Turret 安装在底部列车区域上。

### Step 4：自动射击和子弹

- Hero 和 Turret 会自动向上射击。
- 子弹从 Hero/Turret 附近生成。
- 子弹向屏幕上方飞行，离开屏幕后自动销毁。
- 炮台伤害和射击间隔受 `CannonLevel` 影响。
- BattleScreen 显示当前炮台伤害和射击间隔。

当前公式位于 `GameManager.cs`：

```csharp
UpgradeCost = CannonLevel * 50
Damage = 10 + (CannonLevel - 1) * 5
FireInterval = Max(0.18, 0.75 - (CannonLevel - 1) * 0.08)
```

## 关键文件

- `project.godot`：主场景、autoload、分辨率配置。
- `Endless-Train2.0.csproj`：Godot .NET 项目配置。
- `scenes/Main.tscn`：入口场景，当前实例化 TrainScreen。
- `scenes/TrainScreen.tscn`：列车主界面。
- `scenes/BattleScreen.tscn`：战斗界面。
- `scripts/GameManager.cs`：全局资源、等级、炮台数值。
- `scripts/TrainScreen.cs`：列车界面资源显示、炮台升级、进入战斗。
- `scripts/BattleScreen.cs`：战斗布局交互、拖动、自动射击。
- `scripts/Bullet.cs`：子弹向上移动和离屏销毁。

## 未完成内容

- 基础敌人：
  - 从上方沿左右轨下落。
  - 被子弹命中扣 HP。
  - HP 归零死亡。
  - 到达英雄判定线或列车受击线造成伤害。
- 可射击升级目标：
  - 从上方下落。
  - 与敌人视觉区分。
  - 被子弹打爆后才生效。
  - 战斗结束后清空局内临时升级。
- Boss：
  - 普通波次后出现。
  - 固定在上方。
  - 多段 HP。
  - 左/右轨预警攻击。
- 胜利/失败结算：
  - 胜利发放奖励、推进站点。
  - 失败不发奖励、不推进站点。
  - 返回列车主界面。
- 列车升级/修理列车：
  - Demo 可选，但建议至少保留列车 HP 的反馈和修理入口。
- 燃料消耗：
  - 进入废弃站点是否消耗 Fuel 还未实现。

## 当前验证情况

此前 C# 改动后已运行：

```bash
dotnet build
```

结果：编译成功，0 warning，0 error。

注意：当前环境未找到可直接调用的 Godot 命令行程序，因此编辑器内运行需要手动打开 Godot 项目测试。

## 当前测试方法

1. 用 Godot 4 .NET 打开项目目录。
2. 运行主场景 `scenes/Main.tscn`。
3. 在 TrainScreen 检查资源、等级、站点、HP 是否显示。
4. 点击 `Upgrade Cannon`：
   - Scrap 足够时，CannonLevel 增加。
   - 按钮成本刷新。
   - Scrap 不足时显示 `Not enough Scrap.`。
5. 点击 `Go to Abandoned Station` 进入 BattleScreen。
6. 在 BattleScreen 拖动鼠标或触摸：
   - Hero 和 Turret 应同步左右移动。
   - 不应移出 540 宽战斗区域。
7. 等待自动射击：
   - 子弹应向上飞行。
   - 子弹离开屏幕后销毁。
   - 升级炮台后再次进入战斗，伤害和射击间隔 UI 应变化。

## 给下一个 Codex 的提醒

- 新功能从 `docs/TASKS.md` 的下一项开始。
- 不要跳到 Boss、结算或复杂 SLG。
- 保持灰盒实现即可，优先可玩、可测、可串联。
- 每一步完成后运行 `dotnet build`，并说明 Godot 编辑器内如何验收。
