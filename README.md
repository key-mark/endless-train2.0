# Endless Train 2.0

Godot 4.x + C# 竖屏双轨道防守 Demo。

这个项目当前已经实现了一条可游玩的灰盒闭环：

```text
开场影片 -> 开始菜单 -> 战斗关卡 -> Boss 战 -> Endmenu
				  \-> 失败时返回列车主界面升级后再挑战
```

项目核心不是完整 SLG，而是验证《无限列车》前期的最小可玩循环：列车是基地，玩家在战斗中左右拖动火力位置，自动射击敌人和强化目标，局内成长后击败 Boss。

## 1. 当前实现概览

当前工程已经具备以下内容：

- 开场影片播放：`resource/video/*.ogv` / `.webm` / `.mp4`
- 开始菜单：`scenes/StartMenu.tscn`
- 战斗主场景：`scenes/BattleScreen.tscn`
- 失败后返回列车主界面：`scenes/TrainScreen.tscn`
- 击败 Boss 后进入结尾画面：`scenes/Endmenu.tscn`
- 全局 BGM：点击“开始游戏”后开始播放，直到击败 Boss 才停止
- 双轨道战斗：敌人、Pickup、Boss 冲击波都沿左右轨道自上而下推进
- 英雄和炮塔共享横向位置，自动向上射击
- Pickup 二选一成长：必须打爆才生效
- Boss 双形态冲击波攻击
- 炮台永久升级
- `bullet_add` 会生成围绕英雄的小兵作为视觉反馈

## 2. 运行方式

### 环境要求

- Godot 4.x .NET
- C#

### 启动

主场景：

```text
res://scenes/Main.tscn
```

工程设置位于 [project.godot](project.godot)，当前关键参数：

- 内部玩法分辨率：`540 x 960`
- 桌面测试窗口：`810 x 1440`
- Stretch 模式：`canvas_items`
- Autoload：`GameManager="*res://scripts/GameManager.cs"`

## 3. 当前场景流

当前实际流程如下：

```text
Main.tscn
-> OpeningVideo.tscn
-> StartMenu.tscn
-> BattleScreen.tscn
   -> 胜利：Endmenu.tscn
   -> 失败：TrainScreen.tscn
```

说明：

- `StartMenu` 点击开始后直接进入战斗，不经过列车主界面。
- 失败后回 `TrainScreen`，可以升级炮台，再次挑战。
- 胜利后直接进 `Endmenu`。
- 当前 `Endmenu` 还只是结尾画面，没有后续按钮逻辑。

## 4. 项目结构

```text
scenes/                  场景
  Main.tscn              主入口
  OpeningVideo.tscn      开场影片
  StartMenu.tscn         开始菜单
  BattleScreen.tscn      战斗场景
  TrainScreen.tscn       列车主界面
  Endmenu.tscn           通关结尾画面
  objects/               战斗对象预制体

scripts/                 核心逻辑（全部为 C#）
  GameManager.cs         全局状态 / 永久成长 / BGM
  LevelConfig.cs         关卡 JSON 读取与默认值
  BattleScreen.cs        战斗主逻辑
  Boss.cs                Boss 本体
  Shockwave.cs           冲击波
  Enemy.cs               普通敌人
  Pickup.cs              强化目标
  Bullet.cs              子弹
  StartMenu.cs           开始菜单逻辑
  OpeningVideo.cs        开场影片逻辑
  TrainScreen.cs         列车主界面逻辑

data/
  level_select.json      站点到关卡文件的映射
  levels/demo_001.json   当前 Demo 关卡数值

resource/
  figure/                图片资源
  music/bg.mp3           战斗 BGM
  video/                 开场影片
```

## 5. 策划最常改的文件

如果后续主要是调数值，优先关注这三个入口：

1. [data/levels/demo_001.json](data/levels/demo_001.json)
2. [data/level_select.json](data/level_select.json)
3. [scripts/GameManager.cs](scripts/GameManager.cs)

其中：

- `demo_001.json` 控制本关所有战斗参数
- `level_select.json` 控制站点加载哪一关
- `GameManager.cs` 控制局外资源、永久升级、BGM 和站点推进

如果要新增一个可配字段，则需要同时修改：

1. `data/levels/*.json`
2. `scripts/LevelConfig.cs`
3. 读取和使用这个字段的具体逻辑脚本（通常是 `BattleScreen.cs`）

## 6. 局外参数在哪里改

局外的永久成长和资源在 [scripts/GameManager.cs](scripts/GameManager.cs)。

### 当前默认初始值

| 字段 | 当前值 | 说明 |
|---|---:|---|
| `Scrap` | `100` | 炮台升级货币 |
| `Fuel` | `50` | 当前仅展示和奖励 |
| `Food` | `30` | 当前仅展示和奖励 |
| `Parts` | `0` | Boss 奖励资源 |
| `CannonLevel` | `1` | 永久炮台等级 |
| `TrainLevel` | `1` | 当前仅展示 |
| `CurrentStation` | `1` | 当前站点编号 |
| `TrainMaxHp` | `100` | 列车最大生命 |
| `TrainCurrentHp` | `100` | 当前列车生命 |

### 当前永久升级规则

| 方法 | 当前规则 | 修改位置 |
|---|---|---|
| `GetCannonUpgradeCost()` | `CannonLevel * 50` | `scripts/GameManager.cs` |
| `GetCannonDamageBonus()` | 每级 `+5` 伤害 | `scripts/GameManager.cs` |
| `GetCannonFireInterval()` | 每级缩短 `0.08s`，最低 `0.18s` | `scripts/GameManager.cs` |

### 场景切换与状态推进

| 行为 | 当前逻辑 | 修改位置 |
|---|---|---|
| 失败后重试 | 列车 HP 恢复满血 | `RecoverTrainForNextAttempt()` |
| 胜利后结算 | 发奖励并 `CurrentStation + 1` | `CompleteStation()` |
| BGM 开始 | 开始菜单点击开始后播放 | `PlayGameBgm()` + `StartMenu.cs` |
| BGM 停止 | 击败 Boss 后停止 | `StopGameBgm()` + `BattleScreen.cs` |

## 7. 战斗关卡参数在哪里改

战斗调参的主文件是：

```text
res://data/levels/demo_001.json
```

后续策划日常调参，基本都在这里完成。

### 7.1 根节点：战斗空间与流程

| 字段 | 作用 |
|---|---|
| `id` | 关卡 ID |
| `name` | 关卡名称 |
| `duration` | 总时长（当前主要用于概念定义） |
| `boss_start_time` | 普通波次结束、进入 Boss 的时间 |
| `lanes` / `tracks` | 双轨道 x 坐标 |
| `hero_line_y` | 英雄拦截判定线 |
| `train_line_y` | 列车受击线 |
| `player_min_x` / `player_max_x` | 玩家横向拖动范围 |
| `player_start_x` | 玩家初始位置 |
| `hero_block_radius` | 英雄线拦截半径 |

最常见的空间手感调整：

- 觉得玩家太难兼顾左右轨：调大 `hero_block_radius`
- 觉得人物太靠上或太靠下：调 `hero_line_y`
- 觉得轨道太宽或太窄：调 `tracks.left` / `tracks.right`
- 觉得开局站位别扭：调 `player_start_x`

### 7.2 `player`：基础火力和成长下限

| 字段 | 作用 |
|---|---|
| `hero_damage` | 英雄单发伤害 |
| `turret_damage` | 炮台单发伤害 |
| `hero_fire_rate` | 英雄基础射速（每秒发射次数） |
| `bullet_count` | 炮台基础子弹数 |
| `min_fire_interval` | 最低射击间隔，用于限制堆叠后的最小值 |
| `explosive_radius` | 爆炸弹半径 |

注意：

- 当前项目里 `hero_damage` 现在是 `100`，这是一个非常高的测试值。  
  如果要恢复更正常的 Demo 体验，优先下调这里。
- 最终实际射击间隔还会叠加 `GameManager.GetCannonFireInterval()` 和局内 `fire_rate` Pickup。

### 7.3 `objects`：战斗对象尺寸与位置

| 字段 | 作用 |
|---|---|
| `hero_muzzle_y` | 英雄子弹发射高度 |
| `hero_muzzle_offset_x` | 英雄枪口 x 偏移 |
| `turret_muzzle_y` | 炮台子弹发射高度 |
| `turret_muzzle_offset_x` | 炮台枪口 x 偏移 |
| `turret_bullet_spacing` | 多子弹横向间距 |
| `enemy_size` | 敌人显示尺寸 |
| `pickup_spawn_y` | Pickup 出生高度 |
| `pickup_size` | Pickup 尺寸 |
| `pickup_miss_y` | Pickup 漏过后判定移除的 y |
| `bullet_speed` | 子弹速度 |
| `bullet_width` / `bullet_height` | 子弹尺寸 |
| `bullet_destroy_distance_above_top` | 子弹飞出屏幕上边界多少后销毁 |
| `solider_follow_distance` | 小兵围绕英雄的半径 |

常见调整：

- 英雄整体视觉太高/太低：优先看 `hero_line_y`、`hero_muzzle_y`
- 子弹太容易占屏：调小 `bullet_speed` 或改销毁线
- 小兵太分散/太贴脸：调 `solider_follow_distance`
- Pickup 太难点选：调大 `pickup_size`

### 7.4 `rewards`：关卡结算奖励

| 字段 | 作用 |
|---|---|
| `scrap_base` | 通关基础 Scrap |
| `scrap_per_kill` | 每击杀额外 Scrap |
| `fuel` | 通关 Fuel |
| `food` | 通关 Food |
| `parts` | 通关 Parts |

最终 Scrap 结算公式在 [scripts/BattleScreen.cs](scripts/BattleScreen.cs)：

```text
scrap_base + scrap_per_kill * kill_count + enemy_scrap_reward + bonus_scrap_reward
```

### 7.5 `enemy_types`：普通敌人类型

每个敌人类型支持这些字段：

| 字段 | 作用 |
|---|---|
| `name` | 展示名 |
| `hp` | 生命值 |
| `speed` | 沿轨道移动速度 |
| `damage` | 到达英雄线时造成的伤害 |
| `train_damage` | 穿到列车线时造成的伤害 |
| `reward` | 击杀给的 Scrap |
| `spawn_count` | 单次生成数量 |
| `group_spacing` | 编队沿轨道前后间距 |
| `group_spread` | 编队横向散开距离 |
| `texture` | 敌人贴图 |
| `color` | 兜底颜色 |

当前有三类敌人：

- `grunt`
- `runner`
- `shield`

### 7.6 `pickups`：强化类型本身

这里定义“奖励效果是什么”，而不是“什么时候刷出来”。

| 字段 | 作用 |
|---|---|
| `name` | Pickup 上显示的文字 |
| `kind` | 强化逻辑类型 |
| `value` | 强化数值 |
| `hp` | 单类型默认 HP（当前双选项逻辑下主要参考 `pickup_pairs.hp`） |
| `speed` | 单类型默认速度（当前双选项逻辑下主要参考 `pickup_pairs.speed`） |
| `color` | 默认颜色 |

当前支持的 `kind`：

| kind | 效果 |
|---|---|
| `attack_add` | 固定攻击力增加 |
| `fire_rate` | 射速倍率提升 |
| `heal` | 恢复列车 HP |
| `coins` | 增加结算 Scrap |
| `attack_mult` | 总攻击倍率提升 |
| `turret_mult` | 炮台倍率提升，并推进炮塔贴图等级 |
| `bullet_add` | 增加炮台子弹数，并生成小兵 |
| `explosive` | 赋予子弹爆炸范围伤害 |

文案注意：

- Pickup 现在直接显示 `name`
- 建议使用 UTF-8 保存 JSON，否则中文可能出现乱码
- 当前项目里这些名字已经是中文两行文案格式，例如：

```text
攻击
+3
```

### 7.7 `pickup_pairs`：二选一节奏

这里定义“什么时候在左右轨同时刷出一对 Pickup”。

| 字段 | 作用 |
|---|---|
| `start_time` | 开始刷双选项的时间 |
| `interval` | 每次双选项之间的间隔 |
| `end_time` | 结束刷新的时间 |
| `hp` | 当前双选项统一 HP |
| `speed` | 当前双选项统一速度 |
| `color` | 当前双选项统一颜色 |
| `options` | 左右组合池 |

`options` 的格式：

```json
["attack_add", "fire_rate"]
```

表示左轨刷 `attack_add`，右轨刷 `fire_rate`。

### 7.8 `timeline`：普通波次编排

普通敌人按时间轴生成。

单条配置示例：

```json
{ "time": 31.0, "type": "enemy", "enemy": "runner", "track": "right" }
```

支持字段：

| 字段 | 作用 |
|---|---|
| `time` | 生成时间 |
| `type` | 当前主要使用 `enemy` |
| `enemy` | 敌人类型 ID |
| `track` | 左右轨标识，支持 `left` / `right` / `lefttrack` / `righttrack` 等 |
| `lane` | 轨道兜底索引 |
| `count` | 覆盖默认生成数量 |

如果后续想做更复杂的节奏，策划主要改这里。

### 7.9 `boss`：Boss 与冲击波参数

Boss 采用“本体 + 冲击波”的设计，玩家命中 Boss 本体是固定伤害，真正压迫来自冲击波。

| 字段 | 作用 |
|---|---|
| `name` | Boss 名称 |
| `bars` | 多段血量 |
| `boss_damage_per_hit` | 玩家每次命中 Boss 本体的固定伤害 |
| `spawn_x` / `spawn_y` | Boss 出生位置 |
| `width` / `height` | Boss 尺寸 |
| `attack_interval` | 旧字段，当前主要攻击节奏已被新冲击波逻辑接管 |
| `pattern_start_delay` | Boss 出现到第一次攻击前等待 |
| `between_attack_delay` | 两轮攻击之间的等待 |
| `shockwave_spawn_y` | 冲击波出生高度 |
| `shockwave_impact_y` | 冲击波撞线位置 |
| `staggered_count` | 一轮小冲击波总数 |
| `staggered_spawn_interval` | 小冲击波生成间隔 |
| `staggered_hp` / `speed` / `damage` / `width` / `height` | 小冲击波参数 |
| `wide_hp` / `speed` / `damage` / `width` / `height` | 大冲击波参数 |

当前 Boss 攻击规律：

```text
交错小冲击波 -> 大冲击波 -> 交错小冲击波 -> ...
```

下一轮攻击必须等上一轮冲击波全部消失后才会继续。

## 8. 新增关卡的方法

如果要让策划新增第二关，最小步骤如下：

1. 复制一份 `data/levels/demo_001.json`
2. 改成新的文件名，例如 `demo_002.json`
3. 在 `data/level_select.json` 中追加：

```json
{
  "id": "demo_002",
  "station": 2,
  "path": "res://data/levels/demo_002.json"
}
```

4. 胜利一次后，`GameManager.CurrentStation` 会自动加一
5. 下次进入战斗时，`LevelConfig.LoadForStation(State.CurrentStation)` 会自动按站点编号加载

如果当前站点没有找到对应关卡，会回退到 `default_level`。

## 9. 视觉资源和表现入口

策划和美术常用资源路径：

| 内容 | 路径 |
|---|---|
| 开场背景 | `resource/start_bg.png` |
| 结尾背景 | `resource/figure/end_bg.png` |
| 战斗背景 | `resource/figure/new_bg.png` |
| 列车 | `resource/figure/train.png` |
| 英雄 | `resource/figure/hero.png` |
| 炮台等级贴图 | `resource/figure/gun/gun_lv1.png` ~ `gun_lv4.png` |
| 子弹 | `resource/figure/bullet.png` |
| 小兵 | `resource/figure/solider.png` |
| Boss | `resource/figure/boss.png` |
| 敌人 | `resource/figure/enemy/*.png` |
| 小/大冲击波 | `resource/figure/wave/wave_small.png`, `wave_big.png` |
| BGM | `resource/music/bg.mp3` |
| 开场影片 | `resource/video/` |

如果只是换图，不一定需要改代码；先检查对应 `.tscn` 或 JSON 的贴图路径是否引用到了正确资源。

## 10. 如果要改“位置”，优先改哪里

这个项目里“位置”分两类：

### A. 数值驱动的位置

优先改：

- `data/levels/demo_001.json`

典型字段：

- `hero_line_y`
- `train_line_y`
- `player_start_x`
- `player_min_x`
- `player_max_x`
- `objects.hero_muzzle_y`
- `objects.turret_muzzle_y`
- `objects.pickup_spawn_y`
- `boss.spawn_x`
- `boss.spawn_y`

### B. 场景驱动的位置

如果是 UI、背景、轨道角度、控件排版，改：

- `scenes/BattleScreen.tscn`
- `scenes/StartMenu.tscn`
- `scenes/TrainScreen.tscn`
- `scenes/Endmenu.tscn`
- `scenes/objects/*.tscn`

例如：

- 轨道的视觉旋转和长度：`scenes/BattleScreen.tscn` 里的 `LeftTrack` / `RightTrack`
- 开始按钮位置：`scenes/StartMenu.tscn`
- 小兵缩放：`scenes/objects/Solider.tscn`

## 11. 当前 Demo 的关键现状

为方便接手，这里列出当前工程里最值得注意的几个现状：

- 当前只有 1 个正式关卡：`demo_001`
- 站点推进已生效，但第二站会回退到默认关卡
- 开始菜单直接进战斗，不先进列车主界面
- 失败后回列车主界面，胜利后去 Endmenu
- BGM 会从点击开始游戏后持续到击败 Boss
- 当前基础火力已经收敛回 Demo 可玩档，后续优先从 `player.hero_damage` 和 `player.turret_damage` 微调手感
- `bullet_add` 会生成围绕英雄的小兵
- `turret_mult` 不只是倍率提升，还会切炮塔贴图等级
- 项目当前没有存档，所有状态只保存在本次运行的 `GameManager` 中

## 12. 开发与调参注意事项

- 所有关卡配置建议使用 UTF-8 保存
- 如果新增 JSON 字段，必须同步更新 `scripts/LevelConfig.cs`
- 如果只是调数值，优先不要改 `BattleScreen.cs`
- 如果战斗突然报空引用，先检查对应场景节点名是否和代码一致
- 当前 README 介绍的是“工作区此刻的工程状态”，不是最初设计稿

## 13. 手动测试清单

1. 用 Godot 4.x .NET 打开项目
2. 运行 `res://scenes/Main.tscn`
3. 确认开场影片正常播放，或能点击跳过
4. 进入开始菜单，点击开始游戏
5. 确认 BGM 开始播放
6. 进入战斗，确认玩家可左右拖动
7. 确认敌人和 Pickup 沿左右轨道下落
8. 确认 Pickup 需要被打爆才生效
9. 确认 `bullet_add` 后会出现围绕英雄的小兵
10. 存活到 Boss 阶段，观察小冲击波和大冲击波交替
11. 击败 Boss，确认 BGM 停止并进入 Endmenu
12. 再测试一次失败，确认回到 TrainScreen 且可以升级炮台后再次挑战

## 14. 对策划的快速建议

如果你只是想先把手感调顺，建议按这个顺序改：

1. `player.hero_damage`
2. `player.turret_damage`
3. `pickup_pairs.hp`
4. `pickup_pairs.speed`
5. `enemy_types.*.hp`
6. `enemy_types.*.speed`
7. `boss.staggered_*`
8. `boss.wide_*`
9. `rewards.*`

其中：

- 前期卡手，先改 `hero_damage` / `turret_damage`
- 二选一太难，先改 `pickup_pairs.hp` / `pickup_pairs.speed`
- Boss 太脆或太硬，先改 `boss_damage_per_hit`、`bars`、冲击波 HP
- 奖励太少或升级节奏太慢，改 `scrap_base` 和 `GameManager.GetCannonUpgradeCost()`

---

如需继续扩展本项目，建议优先保持“配置驱动调参”，尽量把新数值也收敛到 `data/levels/*.json`，这样策划后续接手成本最低。
