# 古代人生 MVP

Godot 4.6 + C# 的竖屏文字人生模拟 Demo。移动端基准视口为 `1080x2400`，桌面调试窗口为同宽高比的 `450x1000`。

## 运行

1. 使用 Godot 4.6.x Mono 版本打开本目录。
2. 等待 C# 编译完成。
3. 运行主场景 `Scenes/Main.tscn`，或直接按 `F6/F5`。

## 目录

```text
Configs/                 JSON 游戏配置
Resources/               背景、图标、主题等资源
Scenes/                  主场景、UI 场景、可复用组件场景
Scripts/
  Configs/               配置读取与校验
  Managers/              游戏会话与 Godot 管理节点
  Models/                角色、历法、行动、结算数据
  Systems/               时间、行动、每月结算规则
  UI/                    界面渲染与组件脚本
Tests/                   不依赖引擎的核心规则检查
```

`Scripts/MainController.cs` 只连接 UI 与 `GameManager`。数值规则位于独立 System 中，UI 不直接修改角色状态。

## 扩展行动

在 `Configs/actions.json` 中增加一项即可生成新按钮。支持以下奖励类型：

- `money`
- `culture`
- `energy`

图标放入 `Resources/Icons/`，并在配置的 `icon_path` 中引用。`daily_limit` 为 `0` 时不限制每月次数。

## 验证

```powershell
dotnet run --project Tests\AncientLife.CoreChecks.csproj --configuration Release
```

核心检查覆盖行动消耗、休息限制、捕鱼范围、每月结算、历法推进、死亡和重新开始。

## 随机事件

事件统一配置在 `Configs/Events.json`。根节点的 `trigger_chance` 控制每月结算后的触发概率，事件通过 `weight` 加权抽取，并支持属性条件、2 至 4 个动态选择、多个奖励和 `nextEventId` 连续事件。

正式流程为：每月结算、检测事件、完成事件选择、进入下个月。事件弹窗只负责显示，条件判断和奖励执行位于独立事件系统中。

## 架空历法

年号从 `Configs/Eras.json` 随机选择。月份、四季和月令配置在 `Configs/Calendar.json`。一个月为一个回合，每 12 个回合进入新年并增加一岁；`EraManager` 已提供改元入口，但当前版本不会自动改元。
