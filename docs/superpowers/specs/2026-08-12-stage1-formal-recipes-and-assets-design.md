# 第一阶段正式配方与物品目录设计

## 1. 目标

将用户批准的 9 杯核心饮品作为第一阶段正式内容源，替换当前冰美式配方与单一原料 Resource 的开发占位，并把由配方反推的去重物品需求纳入核心交互物品建模计划。本轮不生成模型、不切换尚未验收的资产清单状态，也不把尚未实现的 8 杯饮品表述为可游玩。

## 2. 权威来源与冲突处理

- 九杯配方正文是原料与用量的最高优先级来源。
- 库存汇总缺少但配方正文出现的水、咖啡豆、苏打水、柠檬汁和蛋白必须补入正式原料目录。
- 糖浆瓶与冰夹等重复条目只保留一个稳定资产 ID；配方引用同一原料或工具。
- 威士忌酸不加入橙皮，因为该装饰未出现在对应配方正文。
- `4–5 块`保存为数值范围；“两勺”“少量”和未注明数量保持原始语义，不换算为毫升、克或平衡值。
- 碎冰与大冰块是不同资源形态；青柠片、青柠汁和青柠角共享青柠原料来源，但作为不同配方成分与可视资源变体记录。

## 3. 数据边界

### 3.1 原料目录

新增一个 `IngredientCatalogDefinition`，集中保存所有第一阶段正式原料。每项 `IngredientDefinition` 继续使用稳定 ID、显示名和主要计量单位，并将 `IsPrototype` 设为 `false`。计量单位扩展为 `Drop` 与 `Spoon`，避免虚构一滴或一勺的换算体积。

正式原料目录包含配方正文所需的 24 个唯一成分 ID：

`crushed_ice`、`water`、`coffee_beans`、`gin`、`tonic_water`、`lime_slice`、`ice_cube`、`whiskey`、`aromatic_bitters`、`sugar_cube`、`orange_peel`、`white_rum`、`lime_juice`、`soda_water`、`simple_syrup`、`mint_leaves`、`tequila`、`orange_liqueur`、`salt`、`vodka`、`ginger_beer`、`lime_wedge`、`dry_vermouth`、`olive`、`lemon_juice`、`egg_white`。

其中青柠的片、汁、角是不同工序形态，碎冰与冰块也是不同工序形态。目录实际为 26 个成分 ID；将三种青柠形态合并为一个青柠来源、两种冰形态合并为一个冰来源后，共 23 个采购来源族。

### 3.2 配方目录

新增 `RecipeCatalogDefinition` 和 `RecipeIngredientRequirement`：

- `RecipeDefinition.Ingredients` 保存正式配方正文，不参与当前成品杯评价的内部中间产物判断。
- `RecipeIngredientRequirement` 同时支持单值、范围和纯文本数量，任何未提供数值的材料不得自动补值。
- `RecipeDefinition.ImplementationStatus` 区分 `CatalogOnly`、`Partial`、`Playable`。
- 9 杯均为 `IsPrototype=false` 的正式内容；冰美式为 `Partial`，其余 8 杯为 `CatalogOnly`。
- 当前 `RecipeDefinition.Steps` 继续服务于已存在的冰美式运行时工序。原料正文与运行时评价目标分离，避免错误要求成品杯中仍含未转化的咖啡豆。

### 3.3 冰美式运行时

- 将资源稳定 ID 从 `prototype_iced_americano` 改为 `iced_americano`，显示名改为“冰美式”，运行时加载路径同步更新。
- 正式正文记录 `150g 碎冰 + 100ml 水 + 18g 咖啡豆`。
- 现有研磨、萃取、过滤链保留为暂时实现，并使用正式投料量；萃取液与过滤咖啡液仍是内部中间产物，不进入正式库存统计。
- 三种双头量酒器的小端标称容量正式调整为 `10 ml`、`15 ml`、`25 ml`，大端分别为 `20 ml`、`30 ml`、`50 ml`。这保留现有换端交互，并可通过重复计量精确组成第一阶段配方中的全部整数毫升用量。
- 因碎冰铲、正式咖啡萃取设备、正式容差与完整动画尚未接入，状态保持 `Partial`，不得宣称正式流程已验收。
- 旧存档中的 `prototype_iced_americano` 在载入时迁移为 `iced_americano`，避免仅因稳定 ID 更名使现有快照失效。

## 4. 建模计划扩展

现有 16 项计划保留其历史验收状态，并新增“第一阶段正式配方资产扩展”章节。新章节不预先把资产写进 `asset_manifest.json`；每个后续批次仍需独立通过轮廓和 Forward+ 两道用户检查点。

资产按以下三种口径统计：

1. **稳定模型资产**：需要独立 GLB 或独立包装场景的模型族。
2. **可交互物品实例**：玩家可拿取、放置、倾倒、切割或操作的实体。
3. **资源变体**：可共享模型但需不同材质、标签、内容物或切分状态的变体。

计划必须明确标出：

- 已有正式资产可复用：`highball_glass`、三种 `jigger_*`、`mortar`、`pestle`、`ice_tongs` 等。
- 已有灰盒待替换：`ice_bucket`、`cutting_board`、`coffee_beans`、`water_kettle` 等。
- 新增杯具、酒瓶族、辅助瓶罐、果物/植物、糖盐冰、吧勺、雪克杯、冰铲、调酒刀、咖啡研磨与萃取设备。
- `冰夹`只计一次；`糖浆瓶`只计一次；`青柠`通过完整果、片、角和汁的资源变体管理。
- 动画需求单独列为依赖矩阵：倾倒、切割、捣碎、摇酒、搅拌、磨豆、咖啡萃取。

## 5. 验证与完成标准

- 自动验证 9 个配方 ID、26 个成分 ID、所有引用闭合、数量范围合法、正式标记和实现状态。
- 自动验证 `jigger_small=10/20 ml`、`jigger_medium=15/30 ml`、`jigger_large=25/50 ml`，并移除量酒器界面中的“开发占位容量”措辞。
- 冰美式 Resource 构建后必须保留正式正文，并生成与现有工序兼容的运行时目标。
- 旧配方 ID 存档迁移通过测试。
- 资产清单保持原状态：新计划项不得提前加入 manifest 或去掉 placeholder。
- 运行资产校验、Debug/Release 构建、领域测试和相关 Godot 测试；没有视觉资产变化，不新增截图验收。
- 更新 `PROJECT_STATUS.md`、`ROADMAP.md`、`CHANGELOG.md`、`CONTEXT_HANDOFF.md` 与根目录 `progress.md`。
- `export_presets.cfg` 始终排除在提交之外。
