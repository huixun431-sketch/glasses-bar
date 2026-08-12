# 《眼镜酒馆》垂直切片技术设计

更新日期：2026-08-12

## 目标与核心原则

第一阶段 9 杯正式配方正文与三种量酒器容量已经批准并进入 Resource 目录。当前可玩链仍只有部分实现的冰美式，其余 8 杯为目录内容；正式容差、概率／恢复曲线、奖励和成本尚未批准。实现继续以“允许玩家犯错”为首要原则：只阻止物理上不可能的行为，不用正确答案提前拦截玩家；错误工具、错误材料和比例偏差必须进入可感知、可恢复的结果。

完整结构审查、状态归属和拆分优先级见 `docs/ARCHITECTURE_REVIEW_20260729.md`；逐脚本职责见 `docs/SCRIPT_RESPONSIBILITIES.md`。

## 架构层级

- 定义层：Godot Resource 只定义工具、工序、配方、原料和资产；加载后转为 `ToolSpec`、`OperationSpec`、`RecipeTargets`。
- 校验层：`GameplayCatalogValidator` 在创建实例前检查稳定 ID、工具分类、工序引用、输入输出、结果容器以及配方/工序漂移。
- 实例层：`ToolInstanceState`、`ToolInventoryService`、`DrinkAssemblyState`、`ProcessExecutionService`、`DayFlow` 和 `GameSession` 保存或提交权威运行时状态，不引用表现 Node。
- 动作层：`GameplayActionPipeline` 统一执行“只读检查 → 拒绝/开始 → 提交/取消”，连续动作只在提交时修改玩法状态。
- 场景组合层：`BarLayoutDefinition` 保存当前灰盒坐标/尺寸/稳定节点 ID；`GrayboxArchitectureBuilder` 创建建筑与碰撞，`CabinetBuilder` 组装柜体节点，`GameplaySceneComposer` 创建玩法 adapter 并绑定现有 owner；`GrayboxLevelBuilder` 只保留组合顺序。
- 表现层：`ToolPresentationBinding`、世界 controller、HUD、材质和标签只呈现权威状态。
- 持久化边界：`GameSaveSnapshot` 按 schema version 保存会话、工作台、工具实例、玩家姿态和柜体开合；不保存 Node、材质、Tween、HUD 或活动动作。

Gameplay 与表现之间的依赖只能单向流动：Gameplay 通过事件、Godot signal 或动作结果通知表现层，表现层读取结果并自行选择动画实现。Gameplay 不得引用、查询或控制 `AnimationPlayer`、AnimationTree、IK 求解器、Skeleton/Bone 或其他动画/骨骼表现对象；表现层也不得反向决定动作是否合法、工序结果、配方状态或存档内容。后续接入动画、IK 和正式角色资产时必须继续遵守此边界。

## 世界结构

- `NeutralGameplay` 保存唯一的工具实体、双手槽位、摆放位置、砧板组合、原材料、饮品和订单状态。
- `RealityWorld` 显示客人和原材料，并应用现实世界近视后处理；现实世界允许制作。
- `GlassesWorld` 隐藏客人与原材料，保留吧台和工具信息表现；戴镜只用于查询与辨认，不是开始制作的前置条件。
- 切镜只改变表现和交互权限，不复制玩法对象；玩家在两个世界都可以移动。

## 双手、工具与摆放

- `ToolCategory.Automatic` 是新增工具的默认分类。能搬运原料或必须手持使用的工具自动归为 `Handheld`，其余归为 `Placement`；也可在 Resource 中显式覆盖。
- 左手至多持有一种放置类工具，右手至多持有一种手持类工具。高球杯、研钵、滤具属于放置类；研杵、原料勺、冰夹和三种双头量酒器属于手持类。水壶是固定供水站，不再作为可拿取直倒工具。
- 每件工具只有一个 `ToolInstanceState` 权威实例；`ToolInventoryService` 持有工具集合、双手槽、砧板槽、连续位置和内容转移。`DrinkWorkstation` 在适配层按稳定 ID 使用 `ToolPresentationBinding` 同步对应 `ToolInteractable` 表现节点。拿起时原位置实体与碰撞消失，并在对应手部显示；放下后同一实例移动到新位置，不生成副本。连续摆放坐标属于实例状态，不从视觉节点反向读取。
- 前后吧台使用连续放置表面。交互点直接作为摆放坐标，`ToolInventoryService` 按工具占地半径检查重叠；`DrinkWorkstation` 只把结果同步到节点并发出 signal。双手都有物品时普通吧台与砧板都优先处理左手放置类工具。
- 手持类工具一次只能携带一种原材料；允许继续累加同一种原料。原材料不能徒手拿取，也不能直接放在空砧板或裸吧台上；装有材料、产物或废品的手持工具不能落普通台面，必须先转移或清废。
- 双手占满且要更换已清空的右手工具时，玩家先在普通台面放下左手工具，再放下右手工具，随后重新拿回左手工具；载料右手工具必须先完成转移，不能用落台绕过物理规则。

## 数据驱动工序

- `data/gameplay/prototype_gameplay_catalog.tres` 是当前工具和工序的权威开发目录，稳定 ID 同步到资产清单。
- `data/ingredients/stage1_ingredients.tres` 与 `data/recipes/stage1_recipe_catalog.tres` 是第一阶段正式内容目录；正式投料要求与当前成品评价目标分离，防止咖啡豆等转化原料被误当作杯中最终成分。
- `OperationComplexity.Automatic` 按能力自动分类：可脱离砧板执行的是简易工序；砧板工序中不要求手持工具的是普通工序；要求手持工具参与的是复杂工序。
- 当前简易工序只有加冰与加水，使用左手高球杯和右手载料工具，按 `R` 执行；直接加水按量酒器实际载量转移，支持 `10/20`、`15/30`、`25/50 ml` 六个端位。
- 砧板有三个放置类工具位。它的可实现工序完全由已放置工具 ID 集合解析，`RequiredPlacementToolIds` 原生支持复数工具组合。
- 工具可通过 `BoardConflictGroup` 声明砧板冲突；当前研钵与传统滤具占用同一准备容器角色，不能同时上板，高球杯可与滤具组成过滤组合。
- 砧板交互顺序为：放置左手工具 → 用右手工具放入原材料 → 在材料存在后尝试当前组合支持的工序。系统不会在入料时验证配方正确性。
- `ProcessExecutionService` 是无 Godot 依赖的工序应用服务，负责目录、能力/选择、来源合并、规则鉴定、输出/废品、重复补救和工序统计。它返回类型化 `ProcessExecutionOutcome`；`DrinkWorkstation` 只将 outcome 格式化为既有反馈并发 signal。
- `DrinkAssemblyState` 是当前饮品唯一 owner，持有纯领域 `LiquidContainer`，统一管理接纳量、溢出、浪费、失败、完成步骤/完成度、丢弃重做、评价输入、每日重置和 schema version 1 快照映射。
- `ProcessExecutionService` 只向稳定的 `DrinkAssemblyState` 提交工序结果，不直接改写 `DrinkSnapshot`，也不接收 Node、材质、动画、IK、骨骼或 UI 对象。

## 错误、概率与恢复

- 错误手持工具：允许开始并完成操作动作，结算时失败，参与材料标记为废品。
- 错误材料种类：如果材料集合不符合该工具组合支持的任何工序输入，结算失败并标记为废品。
- 操作动作不足：不报废材料，玩家可继续尝试。
- 当前开发占位概率为：`完成度 = clamp(1 - 平均相对偏离度, 0, 1)`，本次成功率等于该完成度；相对偏离不超过 `0.0001` 视为浮点数值噪声并保证成功。鉴定通过时完成度沿中间产物一直传递到成品；鉴定失败时材料报废。此公式和目标量均是 `IsPrototype=true` 的技术占位，不是正式平衡。
- 每天开始时 `HandsWashedToday=false`，概率工序额外扣除 `0.04` 原型成功率；与水槽洗手后当天归零。水槽不再产生制作用水。
- 萃取缺水时先区分固定水壶是否为空与滤具是否未加入量取水，失败尝试不破坏咖啡粉。重复萃取和过滤调用有上限的部分恢复函数，当前上限 `0.96`，不能把偏差完全抹平。
- 废品不会自动消失。玩家必须拿起装有废品的放置类工具，或保留载有废品的手持工具，再与弃物桶交互清空；工具本身不会被丢弃。
- 饮品评价只从当前高球杯实例重建原料和成品完成度；失败次数、浪费、溢出和用时是当日历史指标。丢弃旧饮品后，旧杯内容和旧完成度不得污染重做成品。

## 统一动作管线

- `IInteractable` 负责目标发现、提示、可用性和动作定义映射；检查阶段必须只读。
- `GameplayActionDefinition` 是稳定定义，当前覆盖拿取、摆放、砧板转移、工序、站点、交付、柜体、切镜、量酒器和跨天。
- `GameplayActionPipeline` 是玩家命令的唯一运行时入口，输出 `GameplayActionTrace`。
- 瞬时动作在执行后提交；`IManualOperation` 连续动作由管线持有，开始/更新阶段不改材料，完成时结算，取消时不提交。
- 玩家输入不得新增绕过管线的直接状态写入；测试可通过显式 adapter 调用内部方法，但正式输入路径必须经过管线。

## 存档边界

- `GameSaveSnapshot` 当前 schema 为 1，使用稳定配方 ID、世界 ID 和工具 ID。
- 快照包含天数/阶段/世界、工具位置/手位/砧板槽/内容、当前杯、卫生、水壶、当日指标、玩家姿态和柜体开合。
- JSON 反序列化后先校验版本、枚举、容量、非负量、工具唯一性、双手与砧板一致性，再允许恢复。
- 恢复先应用领域状态并发出会话事件，再恢复可能被事件重建/重置的玩家和柜体实例。
- 当前只提供架构与往返恢复能力；尚未启用磁盘槽位、原子写入、备份、迁移器或主菜单“继续游戏”。

## 当前开发流程

1. 主菜单选择“开始游戏”进入第 1 天。接单前即可自由拿取、取料与制作，但需求未知且不能交付；接单只揭示订单并保留现场，不要求先戴镜。
2. 可按 `G` 戴镜查看操作手册和分类信息，再按 `G` 回到现实；也可完全不戴镜开始制作。
3. 左手拿研钵并放上砧板；右手原料勺取豆并放入研钵；换研杵尝试研磨。
4. 每日先在水槽洗手。用原料勺转移咖啡粉，移走研钵，左手把滤具放上砧板并加入咖啡粉；右手选择三种双头量酒器之一，按 `F` 选端并从固定水壶量水后完成浸润萃取。
5. 把高球杯作为第二种放置类工具加入砧板，执行不需要手持工具的普通过滤。
6. 左手取回高球杯；打开砧板画面右下方上层抽屉，用冰夹从冰桶取冰；右手分别以冰夹和量酒器携料，按 `R` 完成简易加冰、加水。
7. 走近客人提交，查看包含成品完成度、失败工序与浪费的评价；日结后进入下一天，第 30 天日结后返回主菜单。

## 菜单、天数与场景

- `OpeningMenuController` 在最终视觉稿的无字背景/独立标题上管理 Godot 实时菜单文字、透明点击区、输入模式和独立选择器；“继续游戏”因尚未启用磁盘存档槽而明确禁用。鼠标未悬停时选择器隐藏，键盘/手柄导航时显示。`PauseMenuController` 以 `ProcessMode.Always` 在暂停状态接收输入。两者的设置页共享 `SettingsPanelBinding`，由场景级 `SettingsService` 单向应用纯 `SettingsState` 到 Master bus 与玩家灵敏度；菜单不再各自持有设置值。
- `MyopiaProgression` 以纯领域函数计算 30 天近视值；`MyopiaEffectController` 在 `DayChanged` 时应用规则值，开发控制台可临时覆盖。
- 前台、后墙浅架和左右回转台均有玩家碰撞，关闭状态形成约 `2.31 m` 工作通道。前吧台水槽独占一格且下方净空，其余四格为 8 个深抽屉；酒架上方 3 组吊柜使用 6 扇成对大门。`CabinetInteractable` 以单开互锁避免同时挤占通道；前抽屉约 `0.62 m` 行程，完全打开仍约保留 `1.69 m` 通行带。冰桶随指定上层抽屉移动，其余柜体当前不保存正式玩法物品。
- 上述灰盒布局值集中于不可变 `BarLayoutDefinition` 并在场景创建前校验；建筑、碰撞、柜体和玩法绑定的构造代码分离后仍保留原节点路径与创建顺序。2026-07-29 Forward+ 对照帧与拆分前基线的 SHA-256 完全一致。

## 状态与验证

- `ToolProcessModel` 是无 Godot 依赖的分类、冲突、材料集合、偏离度与结果规则层。
- `ToolInstanceState` 管理单件工具的权威运行时状态；`ToolInventoryService` 已接管工具集合、双手、连续摆放、砧板槽和内容转移；`ProcessExecutionService` 已接管工序选择与提交；`DrinkAssemblyState` 已接管当前杯、饮品统计、评价和快照映射。`DrinkWorkstation` 保留 Godot signal facade、表现同步、卫生/水壶、反馈与跨服务编排。
- `GrayboxLevelBuilder` 已收敛为场景组合根；布局、建筑/碰撞、柜体节点和玩法 adapter 绑定分别由四个无玩法状态的协作者负责。
- `PlayerController` 已收敛为稳定 Godot facade：`PlayerMotor` 持有玩家姿态读写，`InteractionSensor` 只探测目标，`PlayerActionInput` 把输入路由到统一动作管线，`HeldToolPresenter` 单向消费 hand signal。
- 六类站点由 `prototype_station_catalog.tres` 的 `StationDefinition` 描述，并经目录校验后绑定到运行时节点；`StationActionHandlerRegistry` 按 handler ID 路由提示、许可和执行，`StationInteractable` 只做通用门控，不再按 Kind 分派规则。
- 审查列出的 P1 facade/组合根/站点/设置拆分已完成；下一职责迁移目标为 P2 可组合配方条件/效果，仍须保留当前原型配方和评价结果。
- `GameSession` 管理菜单、天数、世界模式和日流程；接单直接进入 `Preparation`，`RecipeObservation` 只保留为兼容路径，不再是制作必经状态。
- 自动化必须覆盖双手容量、实体移动、防重叠、复数砧板工具、冲突、单一载料、错误工具/材料、比例成功/失败、手动清废、每日洗手、双头量酒器、固定水壶、重复工序恢复、动作开始/提交/取消、存档往返、柜体通行、无戴镜制作、交付与跨天重置。

## 边界

本里程碑已登记 9 杯正式配方正文，但不包含其余 8 杯的可玩工序、正式概率平衡、随机订单、多顾客 AI、升级购买、正式经济、磁盘存档槽/UI/云同步、逐日难度/奖励变化、800 度渐进难度或真实流体物理。当前版本化快照只是长期存档的状态与迁移基础。未通过对应建模批次的工具轮廓、手部表现、动画、声音和材质仍是可替换灰盒。
