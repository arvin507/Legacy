# 古代人生 MVP

Godot 4.6 + C# 的竖屏文字人生模拟 Demo。移动端基准视口为 `1080x2400`，桌面调试窗口为同宽高比的 `450x1000`。

游戏以一个月为一个回合。玩家从十六岁开始，通过务农、谋生、读书和随机事件选择人生道路，在生活成本、职业成长与衰老之间维持一生。

## 运行

1. 使用 Godot 4.6.x Mono 版本打开本目录。
2. 等待 C# Debug 构建完成。
3. 运行主场景 `Scenes/Main.tscn`，或直接按 `F6/F5`。

也可以先在命令行构建：

```bash
dotnet build AncientLife.csproj --configuration Debug
```

## 目录

```text
Configs/                 JSON 游戏配置
Resources/               背景、图标、主题等资源
Scenes/                  主场景、UI 场景、可复用组件场景
Scripts/
  Configs/               配置读取与校验
  Managers/              游戏会话与 Godot 管理节点
  Models/                角色、历法、职业、行动和结算数据
  Systems/               时间、行动、职业、衰老、事件与月度结算规则
  UI/                    界面渲染与组件脚本
Tests/                   不依赖引擎的核心规则检查
ROADMAP.md                下一阶段开发路线和范围
```

`Scripts/MainController.cs` 只连接 UI 与 `GameManager`。数值规则位于独立 System 中，UI 不直接修改角色状态。

## 当前人生循环

- 每月拥有有限体力和行动次数。
- 行动会获得资源并提升农事或学识。
- 第一次从事务农类行动会进入农户路线，第一次学习会进入读书路线。
- 达到技能、文化和财富要求后，会在月末自动晋升并支付对应成本。
- 界面会持续显示下一身份及当前晋升进度，便于安排跨月目标。
- 职业每月可能带来金钱或粮食收益。
- 每月需要消耗粮食和日常用度，冬季成本更高。
- 六十岁后会产生逐月衰老损耗，生日时健康和体力上限也会下降。
- 重大职业变化、事件和结局会记录在人生履历中。

## 职业路线

职业配置位于 `Configs/Professions.json`。

### 农户路线

```text
平民 → 帮农 → 佃农 → 自耕农 → 乡绅
```

### 读书路线

```text
平民 → 书生 → 童生 → 秀才 → 举人
```

## 扩展行动

在 `Configs/actions.json` 中增加一项即可生成新按钮。支持以下奖励类型：

- `money`
- `culture`
- `energy`

行动还可以配置：

- `monthly_limit`：每月次数上限，`0` 表示不限制
- `skill_type`：`none`、`farming` 或 `scholarship`
- `skill_gain`：每次成功行动增加的技能值

图标放入 `Resources/Icons/`，并在配置的 `icon_path` 中引用。

## 随机事件

事件统一配置在 `Configs/Events.json`。根节点的 `trigger_chance` 控制每月结算后的触发概率，事件通过 `weight` 加权抽取，并支持：

- 属性、年龄、月份和季节条件
- 职业、农事、学识和声望条件
- 2 至 4 个动态选择
- 多个奖励
- `nextEventId` 连续事件
- `unique` 一生仅触发一次
- `cooldown_months` 冷却月数
- `tags` 内容分类

正式流程为：月度结算、检测事件、完成事件选择、进入下个月。事件弹窗只负责显示，条件判断和奖励执行位于独立事件系统中。

## 架空历法

年号从 `Configs/Eras.json` 随机选择。月份、四季和月令配置在 `Configs/Calendar.json`。一个月为一个回合，每 12 个回合进入新年并增加一岁；`EraManager` 已提供改元入口，但当前版本不会自动改元。

## 验证

```bash
dotnet run --project Tests/AncientLife.CoreChecks.csproj --configuration Release
```

核心检查覆盖行动消耗、月度限制、生活成本、职业路线、晋升、衰老、历法、死亡、人生履历、事件冷却、唯一事件和连续事件。
