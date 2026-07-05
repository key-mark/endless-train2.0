# 《无限列车 SLG》当前实现状态

更新日期：2026-07-05  
对应工程：Godot 4.x + C# 灰盒 Demo  
依据范围：当前工作区中的 `project.godot`、`scenes/`、`scripts/`、`data/` 与 `resource/figure/`

---

## 1. 当前项目结论

本项目已经实现了一个可跑通的竖屏双轨道防守 Demo 闭环：

```text
列车主界面 -> 前往废弃站点 -> 82 秒普通波次
-> Boss 战 -> 胜利/失败结算 -> 返回列车 -> 奖励入账 -> 炮台升级影响下一局
```

当前状态属于“灰盒可玩 + 部分贴图资源接入”。核心玩法已经从早期 Step 推进到接近 Step 9，Step 10 的反馈、UI、调参仍处于粗调阶段。

---

## 2. 工程入口与基础设置

### 2.1 Godot 工程设置

- 主场景：`res://scenes/Main.tscn`
- Autoload：`GameManager="*res://scripts/GameManager.cs"`
- 内部玩法分辨率：`540 x 960`
- 桌面窗口覆盖尺寸：`810 x 1440`
- Stretch 模式：`canvas_items`
- C# 工程：`Endless-Train2.0.csproj`

### 2.2 场景入口

`Main.tscn` 直接实例化 `TrainScreen.tscn`，玩家启动后进入列车主界面。

---

## 3. 已实现的外层列车主界面

实现文件：

- `scenes/TrainScreen.tscn`
- `scripts/TrainScreen.cs`
- `scripts/GameManager.cs`

已实现内容：

- 显示 `Scrap`、`Fuel`、`Food`、`Parts`
- 显示 `Train Lv`、`Cannon Lv`、`Station`
- 显示 `Train HP`
- 提供 `Upgrade Cannon` 按钮
- 提供 `Go to Abandoned Station` 按钮
- 炮台升级会消耗 Scrap，并提升下一次战斗火力

当前永久状态由 `GameManager` 保存：

| 字段 | 初始值 | 用途 |
|---|---:|---|
| `Scrap` | 100 | 炮台升级与奖励回流 |
| `Fuel` | 50 | 当前仅展示和奖励回流，尚未作为出战消耗 |
| `Food` | 30 | 当前仅展示和奖励回流 |
| `Parts` | 0 | Boss 奖励回流，后续高级升级预留 |
| `CannonLevel` | 1 | 影响永久伤害加成与射击间隔 |
| `TrainLevel` | 1 | 当前仅展示 |
| `CurrentStation` | 1 | 胜利返回后递增 |
| `TrainMaxHp` | 100 | 列车最大 HP |
| `TrainCurrentHp` | 100 | 战斗中受伤/治疗 |

炮台升级规则：

| 方法 | 当前规则 |
|---|---|
| `GetCannonUpgradeCost()` | `CannonLevel * 50` Scrap |
| `GetCannonDamageBonus()` | 每级 +5 伤害加成 |
| `GetCannonFireInterval()` | 每级缩短 0.08 秒，最短 0.18 秒 |

---

## 4. 已实现的战斗玩法

实现文件：

- `scenes/BattleScreen.tscn`
- `scripts/BattleScreen.cs`
- `scripts/Bullet.cs`
- `scripts/Enemy.cs`
- `scripts/Pickup.cs`
- `scripts/Boss.cs`

### 4.1 战斗空间

- 战斗容器为 `BattleArea540x960`
- 双轨道默认位于 `x = 175` 和 `x = 365`
- 英雄判定线：`y = 735`
- 列车线：`y = 845`
- 玩家横向移动范围：`x = 52` 到 `x = 488`
- 玩家初始位置：`x = 270`

### 4.2 玩家控制

已支持鼠标和触屏拖动：

- 鼠标左键按下/移动控制横向位置
- 触屏点击/拖动控制横向位置
- 英雄和炮台挂在同一个 `PlayerRig` 下，共享 x 坐标

### 4.3 自动射击

已实现自动向上射击：

- 英雄发射 1 发子弹
- 炮台按当前子弹数量发射
- 子弹向上移动并使用矩形命中检测
- 命中敌人、升级目标或 Boss 后造成伤害并销毁
- 获得爆炸弹强化后，会对半径内其它目标追加伤害

### 4.4 敌人

敌人来自 `data/levels/demo_001.json` 的时间轴配置，沿场景轨道线从上方向下移动。

当前配置了 3 类敌人：

| 敌人 | 体验定位 | 当前特点 |
|---|---|---|
| `grunt` | 基础敌人 | 血量低、速度慢、成组出现 |
| `runner` | 快速敌人 | 血量低、速度快、数量较多 |
| `shield` | 重甲敌人 | 血量高、伤害高、数量压力大 |

伤害规则：

- 敌人到达英雄判定线时，如果与玩家 x 位置足够接近，会造成完整英雄线伤害并移除。
- 敌人继续到达列车线时，会造成较低列车线伤害并移除。
- 列车 HP 归零后进入失败结算。

### 4.5 可射击升级目标

升级目标由 `Pickup` 实现，不是拾取物，必须被子弹打爆才生效。

当前实现了 8 类本局强化：

| 配置 ID | 效果 |
|---|---|
| `attack_add` | 本局攻击力固定增加 |
| `fire_rate` | 本局射速倍率提升 |
| `heal` | 恢复列车 HP |
| `coins` | 本局额外 Scrap 奖励 |
| `attack_mult` | 本局攻击倍率提升 |
| `turret_mult` | 本局炮台伤害倍率提升 |
| `bullet_add` | 本局炮台子弹数量增加 |
| `explosive` | 本局子弹获得范围伤害 |

本局强化会在退出战斗或返回列车时清空。

---

## 5. 波次与关卡配置

实现文件：

- `scripts/LevelConfig.cs`
- `data/level_select.json`
- `data/levels/demo_001.json`

当前通过 `level_select.json` 选择默认关卡：

```text
demo_001 -> res://data/levels/demo_001.json
```

`demo_001` 当前包含：

- 82 秒普通波次时间轴
- 左右双轨道刷怪与强化目标
- 3 类敌人
- 8 类升级目标
- Boss 战参数
- 通关奖励参数

普通波次结束条件：

```text
battle_time >= boss_start_time
```

当前 `boss_start_time = 82.0`。

---

## 6. Boss 战实现状态

Boss 实现文件：

- `scripts/Boss.cs`
- `scenes/objects/Boss.tscn`

当前 Boss：

- 固定出现在屏幕上方
- 总血量由三段血条相加得到：`160 + 180 + 220 = 560`
- 有三阶段 HP 显示和阶段变化提示
- 阶段变化会改变 Boss 颜色
- 攻击前会显示轨道预警矩形
- 攻击左右轨道交替释放
- 玩家位于危险轨道附近时，列车受到伤害
- 击败 Boss 后进入胜利结算

当前尚未实现：

- Boss 阶段改变攻击频率
- Boss 召唤普通敌人
- 随机单轨攻击
- 复杂 Boss 部件或弱点机制

---

## 7. 结算与奖励回流

胜利结算：

- 击败 Boss 后显示 `VICTORY REPORT`
- 显示 Scrap、Fuel、Food、Parts 和击杀数
- 点击 `Return to Train` 后调用 `GameManager.CompleteStation`
- 奖励入账后 `CurrentStation + 1`
- 返回列车主界面后可以继续升级炮台

奖励计算：

```text
Scrap = scrap_base + scrap_per_kill * kill_count + enemy_scrap_reward + bonus_scrap_reward
Fuel  = level rewards.fuel
Food  = level rewards.food
Parts = level rewards.parts
```

失败结算：

- 列车 HP 归零后显示 `DEFEAT REPORT`
- 当前失败不发放奖励
- 当前失败不推进站点

---

## 8. 当前资源与视觉状态

当前已接入部分位图资源：

- 背景：`resource/figure/background.png`
- 列车：`resource/figure/train.png`
- 英雄：`resource/figure/hero.png`
- 炮台：`resource/figure/gun/gun_lv1.png` 到 `gun_lv4.png`
- 子弹：`resource/figure/bullet.png`
- Boss：`resource/figure/boss.png`
- 敌人：`resource/figure/enemy/*.png`
- 强化目标：`resource/figure/pickups/*.png`

仍属于灰盒状态的部分：

- 主界面列车预览仍是占位块
- 战斗 UI 仍以文字 Label 为主
- Hero/Turret/血量等调试标签仍可见
- 命中、爆炸、受击反馈为简单闪烁、缩放、文字和 ColorRect burst

---

## 9. 与 GDD 的差异和未完成项

| 项目 | GDD 目标 | 当前实现 |
|---|---|---|
| 炮台升级 | 必做 | 已实现 |
| 列车升级 | 可作为永久成长 | 仅展示 `TrainLevel`，未实现按钮和效果 |
| 修理列车 | 主界面推荐功能 | 未实现 |
| 出战燃料消耗 | 资源设计预留 | 未实现，Fuel 目前只展示/奖励 |
| 普通波次 | 必做 | 已实现时间轴波次 |
| 动态压力 | 可选 | 未实现自适应压力，只使用静态时间轴 |
| 升级目标 | 必做 | 已实现，必须射爆 |
| Boss 三阶段 | 推荐 | 已实现三段血量/提示/颜色变化 |
| Boss 召唤敌人 | 推荐 | 未实现 |
| Boss 攻击频率随阶段提升 | 推荐 | 未实现 |
| 胜利结算 | 必做 | 已实现 |
| 失败结算 | 必做 | 已实现，失败无奖励 |
| 第二次战斗永久变强 | 必做 | 已通过 CannonLevel 影响伤害和射速实现 |
| 存档 | 当前不做 | 未实现，符合范围 |

---

## 10. Godot 内测试方法

1. 使用 Godot 4.x .NET 打开项目根目录。
2. 确认主场景为 `res://scenes/Main.tscn`。
3. 运行项目，进入列车主界面。
4. 检查资源、等级、站点和列车 HP 是否显示。
5. 点击 `Upgrade Cannon`，确认 Scrap 减少、Cannon Lv 增加。
6. 点击 `Go to Abandoned Station` 进入战斗。
7. 用鼠标或触屏左右拖动，确认英雄和炮台共享 x 坐标。
8. 等待自动射击，确认子弹向上命中敌人和升级目标。
9. 射爆不同升级目标，确认 Buff 文本和火力表现变化。
10. 存活到约 82 秒，确认进入 Boss 阶段。
11. 避开红色轨道预警，击败 Boss。
12. 点击 `Return to Train`，确认奖励入账、站点 +1。
13. 再次进入战斗，确认炮台永久升级带来的火力提升可感知。

---

## 11. 后续推荐优先级

建议继续按 GDD Step 10 做小范围收尾，不扩展复杂系统：

1. 清理灰盒调试 Label，保留必要战斗信息。
2. 让主界面列车预览使用现有 `train.png`。
3. 给炮台等级切换 `gun_lv1` 到 `gun_lv4` 的视觉反馈。
4. 增加修理列车或战斗前回满 HP 的最小规则，避免多局后 HP 状态不清。
5. 调整 Boss 阶段，使阶段 2/3 的攻击间隔更短。
6. 根据需要再实现燃料出战消耗。

