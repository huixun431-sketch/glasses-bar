# 眼镜酒馆

《眼镜酒馆》是一款使用 Godot 4.7.1 .NET/C# 开发的第一人称调酒原型。玩家需要观察、记忆并亲手完成操作；眼镜世界提供信息与规划辅助，但不会替玩家执行制作步骤。

项目目前处于可验证的垂直切片阶段。正式酒吧环境、双世界表现、柜体与工具交互、参数化液体、数据驱动工序、每日循环和测试管线已经接入；配方、平衡、顾客内容及部分工具/原料美术仍是开发占位，不代表最终内容。

## 当前状态

- Godot `4.7.1`、C#、.NET `8.0`、Forward+。
- 正式酒吧环境的六个模块已经通过两次用户视觉检查点并在运行时启用。
- 正式环境加载采用全有或全无策略：任一模块失败时整体回退到完整可玩灰盒。
- 现实世界与眼镜世界共享唯一玩法状态；导入 GLB 只负责视觉，碰撞、灯光、稳定 ID 和交互由 Godot 包装层拥有。
- 资产清单当前为 21 项、0 错误；其中尚未制作的资产继续明确标记为灰盒占位。
- 项目使用 [MIT License](LICENSE)。

详细状态和后续范围见 [项目状态](docs/PROJECT_STATUS.md)、[路线图](docs/ROADMAP.md) 与 [上下文交接](docs/CONTEXT_HANDOFF.md)。

## 环境要求

- [Godot Engine 4.7.1 .NET](https://godotengine.org/)
- .NET 8 SDK
- Python 3（资产验证脚本使用）
- Windows PowerShell（完整验证入口使用）

Blender 只在重新生成或修改正式模型时需要。正常运行、构建和测试现有项目不要求 Blender。

## 运行项目

1. 克隆仓库并进入项目目录。
2. 使用 Godot 4.7.1 .NET 打开 `project.godot`，等待首次导入和 C# 构建完成。
3. 运行主场景 `scenes/Main.tscn`，或在项目根目录执行：

```powershell
godot --path .
```

如果命令名为 `godot4`，请相应替换。Windows 也可以直接使用 Godot .NET 控制台程序的完整路径。

## 基本操作

| 操作 | 默认输入 |
|---|---|
| 移动 | `W` `A` `S` `D` |
| 交互 | `E` |
| 切换眼镜世界 | `G` |
| 执行连续操作 | 鼠标左键 |
| 操作辅助/确认 | `Space` / `Enter` |
| 取消操作 | `Q` |
| 使用手持工具 | `R` |
| 切换量酒器端位 | `F` |
| 暂停 | `Esc` |

部分调试或原型输入仍可能随开发调整；权威映射位于 `project.godot`。

## 构建与验证

构建 C# 项目：

```powershell
dotnet build GlassesBar.csproj --configuration Debug
dotnet build GlassesBar.csproj --configuration Release
```

运行完整验证：

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

完整入口会依次运行：

- 资产验证器自测与主资产清单验证；
- Debug/Release 构建；
- 纯领域测试；
- Godot 导入；
- 酒吧布局、储物、运行时几何、正式资产与灰盒回退测试；
- 冒烟、Stage 1/2 资产、输入和完整流程集成测试。

验证脚本会优先使用 PATH 中的 `godot`/`godot4`。仓库内记录的开发机便携路径只是本地回退，不是项目的通用安装要求。

## 目录结构

- `scenes/`：Godot 场景和手写资产包装。
- `scripts/`：Godot C# 组合、表现和交互层。
- `src/Domain/`：不依赖 Godot 的权威玩法状态与服务。
- `data/`：配方、站点和资产等数据资源。
- `assets/`：正式 GLB、UI 与资产清单。
- `tests/`：纯领域及 Godot 集成测试。
- `tools/`：资产校验、验证和 Blender 工作流工具。
- `docs/`：设计规格、实施计划、状态和资产交接记录。

## 开发边界

- 不要直接修改 Godot 生成的 GLB 导入节点；为资产保留稳定 ID，并通过手写包装接入。
- 不要把权威玩法状态放入 `RealityWorld` 或 `GlassesWorld`。
- 配方、容量、概率、奖励、顾客故事和最终美术在明确批准前必须标记为原型或占位。
- 视觉改动需要在真实 Forward+ 项目中运行并检查实际画面。
- `artifacts/` 和 `builds/` 是本地生成物，不应提交到仓库。

## 贡献

提交改动前请运行完整验证，并在变更涉及视觉时附上实际运行检查证据。当前项目包含尚未定稿的游戏内容；贡献不应擅自把开发占位升级为正式设计。

## 许可证

本项目按 [MIT License](LICENSE) 开源。
