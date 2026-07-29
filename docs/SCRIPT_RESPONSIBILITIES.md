# 脚本职责与拆分表

更新日期：2026-07-29

标记：

- 保留：职责单一或当前规模合理。
- 已拆：本轮已完成关键边界拆分。
- 待拆 P1：下一轮应优先拆分。
- 待拆 P2：内容扩展前处理即可。

## 领域与应用层

| 脚本 | 当前职责 | 权威状态 | 拆分结论 |
|---|---|---|---|
| `src/Domain/DayFlow.cs` | 日流程状态机与合法迁移/恢复 | 当前 `DayPhase` | 保留 |
| `src/Domain/LiquidMath.cs` | 液体转移与倾斜流量纯函数 | 无 | 保留 |
| `src/Domain/MyopiaProgression.cs` | 30 天近视曲线纯函数 | 无 | 保留 |
| `src/Domain/RecipeEvaluation.cs` | 配方目标、饮品评价输入与评分 | 评价 DTO；不持有运行会话 | 保留 |
| `src/Domain/ToolProcessModel.cs` | 工具/工序规格、分类、冲突、概率、补救规则 | 定义规格；无场景状态 | 待拆 P2：规格 DTO 与规则可分文件 |
| `src/Domain/ToolInstanceState.cs` | 单件工具的可保存权威实例状态 | 工具位置、位置分类、内容、废品、完成度、端位 | 已拆 |
| `src/Domain/ToolInventoryService.cs` | 工具实例集合、双手槽、拿放、防重叠、砧板槽、内容装载/转移、工具快照往返 | 工具集合、左右手 ID、砧板工具 ID | 已拆；无 Godot/表现依赖 |
| `src/Domain/GameplayActionModel.cs` | 动作稳定定义、模式、阶段与 trace | 无运行时活动动作 | 保留 |
| `src/Domain/GameplayCatalogValidation.cs` | 工具/工序/配方交叉引用校验 | 无 | 保留 |
| `src/Domain/SaveGameSnapshot.cs` | 版本化存档 DTO、JSON、结构一致性校验 | 序列化副本，不是运行时 owner | 待拆 P2：DTO、validator、serializer 可在迁移器加入时分文件 |
| `scripts/actions/GameplayActionPipeline.cs` | 玩家命令统一检查、拒绝、开始、提交、取消与 trace | 当前活动动作 | 保留 |

## 会话、数据定义与接口

| 脚本 | 当前职责 | 权威状态 | 拆分结论 |
|---|---|---|---|
| `scripts/core/GameSession.cs` | 开局、天数、世界模式、阶段、评价/日结、存档聚合恢复 | 会话状态 | 待拆 P1：存档聚合移至 `SaveGameCoordinator` |
| `scripts/core/GameEnums.cs` | `WorldMode`、`IngredientUnit`、`StationKind` | 无 | 待拆 P2：按领域/场景命名空间拆分 |
| `scripts/core/GameTypes.cs` | `OperationResult`、`InteractionContext` | 短生命周期 DTO | 保留 |
| `scripts/data/GameplayCatalogDefinition.cs` | Resource 目录转规格并触发校验 | 定义数据 | 保留 |
| `scripts/data/ToolDefinition.cs` | 工具 Resource 定义 → `ToolSpec` | 定义数据 | 保留 |
| `scripts/data/OperationDefinition.cs` | 工序 Resource 定义 → `OperationSpec` | 定义数据 | 保留 |
| `scripts/data/RecipeDefinition.cs` | 配方 Resource 定义 → `RecipeTargets` | 定义数据 | 保留 |
| `scripts/data/RecipeStep.cs` | 单个配方步骤定义 | 定义数据 | 保留 |
| `scripts/data/ToleranceProfile.cs` | 数量容差/评分策略定义 | 定义数据 | 保留 |
| `scripts/data/IngredientDefinition.cs` | 原料稳定 ID、显示名、单位、原型标记 | 定义数据 | 待拆 P2：正式原料目录接入时纳入统一 catalog |
| `scripts/data/AssetManifest.cs`、`AssetEntry.cs` | 资产清单 Resource | 定义数据 | 保留 |
| `scripts/interfaces/IInteractable.cs` | 交互发现、提示、动作定义映射与执行适配契约 | 无 | 待拆 P1：最终分成 interaction source 与 action handler |
| `scripts/interfaces/IManualOperation.cs` | 连续动作过程契约 | 过程状态由实现持有 | 保留 |
| `scripts/interfaces/ILiquidContainer.cs` | 液体容器只读/修改契约 | 无 | 保留 |
| `scripts/interfaces/IWorldPresenter.cs` | 世界表现切换/实体查询契约 | 无 | 保留 |

## 玩法实例与交互

| 脚本 | 当前职责 | 权威状态 | 拆分结论 |
|---|---|---|---|
| `scripts/gameplay/DrinkWorkstation.cs` | Godot signal facade、工具表现同步、工序选择/结算、饮品、卫生、水壶、评价、反馈 | 工具库存已迁出；仍持有饮品与当日制作编排 | 已完成首批拆分；待拆 P1：`ProcessExecutionService`、`DrinkAssemblyState` |
| `scripts/gameplay/DrinkWorkstation.Persistence.cs` | 聚合工作台快照；工具部分委托 `ToolInventoryService` 往返 | 不新增状态 | 已完成工具状态委托；以后移入 coordinator/mapper |
| `scripts/gameplay/ToolPresentationBinding.cs` | 工具实例到 Godot 节点的表现绑定 | 无玩法状态 | 已拆 |
| `scripts/gameplay/LiquidContainer.cs` | 当前液体组成、容量、溢出、恢复 | 杯中液体 | 保留；后续可移入纯 Domain |
| `scripts/gameplay/ToolInteractable.cs` | 工具交互源、灰盒碰撞/材质/标签、实例表现应用 | 无工具玩法状态 | 待拆 P2：正式资产时分 source/presenter |
| `scripts/gameplay/CounterSurfaceInteractable.cs` | 连续台面交互与摆放点计算 | 无 | 保留 |
| `scripts/gameplay/WorkboardInteractable.cs` | 砧板交互决策、连续手势过程、提交到工作台 | 活动手势过程 | 待拆 P1：交互 adapter 与 manual process 分离 |
| `scripts/gameplay/StationInteractable.cs` | 顾客/冰桶/水槽/水壶/豆源/弃物桶的提示、许可与执行分派 | 无独立长期状态 | 待拆 P1：Resource + handler 注册表 |
| `scripts/gameplay/CabinetInteractable.cs` | 门/抽屉开合、动画、碰撞、互锁、内容说明 | 柜体开合实例状态 | 待拆 P2：状态/动画 presenter 可分离 |

## 玩家、世界与 UI

| 脚本 | 当前职责 | 权威状态 | 拆分结论 |
|---|---|---|---|
| `scripts/player/PlayerController.cs` | 移动、视角、输入映射、目标探测、动作请求、连续动作驱动、手持灰盒表现、玩家快照 | 玩家姿态；动作由 pipeline 持有 | 待拆 P1 |
| `scripts/world/GrayboxLevelBuilder.cs` | 组合根、灰盒建筑、碰撞、柜体、站点、工具、菜单/重置绑定 | 布局常量；不应拥有玩法进度 | 待拆 P1 |
| `scripts/world/WorldLayerController.cs` | 现实/眼镜世界、模糊层和信息层可见性 | 仅表现缓存 | 保留 |
| `scripts/world/MyopiaEffectController.cs` | 度数 → Shader 参数、开发覆盖 | 当前表现参数 | 保留 |
| `scripts/ui/HudController.cs` | 阶段、提示、动作进度、反馈、双手、日结显示 | 仅 UI 状态 | 保留 |
| `scripts/ui/OpeningMenuController.cs` | 主菜单页面、输入模式、选择器、设置控件 | 临时 UI/设置值 | 待拆 P1：设置逻辑复用 |
| `scripts/ui/PauseMenuController.cs` | 暂停页面、重开/返回信号、设置控件 | 暂停 UI 状态 | 待拆 P1：与主菜单共享设置服务 |
| `scripts/ui/DeveloperConsole.cs` | 开发命令输入与近视调节 | 控制台开关/文本 | 保留 |

## 测试与工具

| 脚本/目录 | 职责 | 结论 |
|---|---|---|
| `tests/DomainTests.cs` | 纯 C# 日流程、液体、评价、工具库存、工序、动作定义、目录校验、存档 schema | 保留；当前 19 项 |
| `tests/godot/InputIntegrationTests.cs` | 菜单、输入、移动、交互、切镜和动作管线 | 保留 |
| `tests/godot/FlowIntegrationTests.cs` | 完整制作、错误/补救、柜体、存档往返、评价、跨天 | 待拆 P2：按系统拆成多个场景测试以降低单文件长度 |
| `tests/godot/*VisualCapture.cs` | 指定状态的 Forward+ 视觉捕获 | 保留 |
| `tools/run_verification.ps1` | 一键资产、构建、领域、Godot 冒烟/输入/流程验证 | 保留 |
| `tools/validate_assets.py` | 资产 manifest 与 GLB 结构校验 | 保留 |

## 新脚本放置规则

1. 新定义放 `scripts/data`，运行前转成 `src/Domain` 的不可变规格。
2. 新可保存实例状态放 `src/Domain`，不得引用 Godot Node。
3. 新玩家动作先添加稳定动作定义，再接入 `GameplayActionPipeline`。
4. 新交互节点只负责发现/适配，不直接成为跨系统状态 owner。
5. 新视觉脚本只订阅状态或 signal，不反向决定配方、工序或存档结果。
6. 动画、IK、骨骼等表现实现只能订阅 Gameplay 事件/signal 或读取动作结果；Gameplay 不得直接依赖或控制这些表现对象。
