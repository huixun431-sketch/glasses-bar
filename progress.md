# 《眼镜酒馆》开发进展交接

更新时间：2026-08-11

用途：新对话先读本文件，再读 `docs/CONTEXT_HANDOFF.md`。正式配方、平衡值、顾客内容和最终美术仍未批准；下述数值与灰盒均为开发占位。

## 当前进展｜2026-08-11 Task 10 预集成样式待用户审查

### 已完成的事项

- 六个环境候选已按最新反馈重建：连续 C 形深棕吧台、内侧倒角、东侧门洞、无抽屉水槽基座、空心无异物水槽、顺时针旋转 `90°` 并收紧为 `0.62 × 0.28 m` 的西侧手册架、空心后柜／规整瓶架、后移推拉门、朝桌椅子与六盏磨砂吊灯。
- 新增客桌真实向下 SpotLight3D；27 张 Godot 4.7.1 Vulkan Forward+ `1920×1080` 现行图已逐张检查，证据在 `artifacts/bar-interior-formal-preintegration/`。
- Blender 合同、六份 sidecar、主清单 `21/0`、批次 `formal-candidate errors=0`、领域 `28/28`、Debug/Release 和全部 Godot 集成测试均通过。

### 关键决策

- 当前只到 Task 10 预集成审查，不是建模 Skill 检查点 2。六项环境继续 `placeholder: true`，游戏仍使用完整灰盒。
- 等待用户批准当前样式后才执行 Task 11–13 的生产加载、可动绑定、双世界／回退与最终 QA。
- `export_presets.cfg` 是用户改动，不纳入本轮提交；不推送、不发布。

### 未完成的待办

1. 用户检查当前 27 张 Forward+ 样式证据并批准或提出返修。
2. 获批后执行 Task 11–13；Task 11 行为集成和用户检查点 2 通过前不得取消六项 placeholder。

## 历史｜2026-08-03 首轮正式环境样式候选（已被 2026-08-11 重建取代）

### 已完成的事项

- 南门异物已定位为 Blender AREA 审查灯穿过门洞产生的矩形投影；更换环境光与无阴影 SUN 灯、重拍并逐张检查后异物消失。用户确认其余建筑轮廓符合预期，检查点 1 已归档批准。
- 灰盒玩家工作台与客侧台面新增两张 Forward+ 连续性近景；运行时几何测试新增顶面三角形朝向和覆盖面积断言，确认“似乎渲染失败”不是缺面。
- 已生成建筑、前吧、后吧、家具、灯具外壳、磨损叠加六个正式 GLB 候选、sidecar、手写 Godot 视觉包装和整套预览场景。
- 修复 Blender master 已为 Y-up 却在导出时再次转换的问题；Godot 集成测试锁定房间导入 AABB 为 `16 × 4.5 × 10 m`，并验证视觉包装不拥有碰撞或真实灯光。
- 已用 Godot 4.7.1 Vulkan Forward+／RTX 5070 生成并逐张检查六张 `1920×1080` 正式候选图；建模批次 `forward-plus-review errors=0`。
- 验证全绿：Blender 合同 PASS；六份 sidecar 各 `1/0`；主清单 `21/0`；领域 `28/28`；Debug/Release `0` 警告／`0` 错误；布局、储物、运行时几何、冒烟、Stage 1/2、输入和流程全部 PASS。

### 关键决策

- 正式候选严格沿用当前 Z3/H3 `16 × 10 × 4.5 m` 与 `9.10 m` 前吧，不恢复被打回的 12×9 旧方案。
- 西侧只保留手册，东侧保留弃物区和员工门，水槽下保持开放管线；瓶架为五湾双层规整空架，供后续瓶装原料使用且不残留旧连续架。
- 六个环境模块仍为 `placeholder: true`，当前游戏继续使用完整灰盒。GLB 只负责视觉，碰撞、灯光、稳定 ID 和可动状态继续由 Godot 权威层拥有。

### 未完成的待办

1. 等待用户检查 `artifacts/bar-interior-formal-forward-plus/` 的正式几何／材质候选并批准或指出返修。
2. 获批后执行生产视觉加载器、可动件视觉绑定、现实／眼镜材质与灯光切换，以及任一模块失败时的整套灰盒回退。
3. 完成最终生产／眼镜／强制回退 Forward+ QA 后，才可取消六个环境 placeholder；正式配方、平衡、顾客内容与最终美术继续不扩展。

## 2026-08-03｜建筑中性轮廓检查点 1

### 已完成的事项

- 新增确定性 Blender master、公共建模帮助器、候选导出器、审查渲染器和建筑合同测试。
- 生成 ignored `bar_architecture` 候选并通过真实 GLB 验证；合同测试、候选 1/0、正式占位清单 21/0 均通过。
- 首轮图因构图、曝光和 Y-up 相机翻滚被人工否决；修正后生成六张 1600×1000 中性图并逐张检查。
- 审查图证明开口与墙体分段一致、护墙板绕开门窗、地板沿南北铺设、房间为 16:10、1.83 m 人体参照竖直落地。

### 关键决策

- 本轮只到 `silhouette-review`。候选 GLB、`.blend` 和 PNG 保持 ignored；正式资产清单不切换。
- 建筑轮廓批准前不制作正式建筑 GLB、Godot 包装、前后吧正式候选或材质抛光。

### 未完成的待办

1. 等待用户查看 `artifacts/blender_review/architecture/` 六张图并明确批准或提出返修。
2. 获批后记录检查点 1，再进入 formal-candidate；未获批继续只返修建筑轮廓。
3. 正式配方、容量、平衡、顾客内容与最终美术仍未批准。

## 2026-08-03｜环境 GLB 合同与下一建模检查点

### 已完成的事项

- 六个环境模块已加入资产清单且继续保持占位；模块合同记录稳定根、必需节点、枢轴、视觉/玩法所有权和灰盒回退边界。
- 验证器已覆盖必需根、必需节点、场景根单位变换；四类自测全部通过。
- 已清理正式建模计划中会引入旧版 12×9×3.5 m、三湾/六门后吧的过时文字。
- 完整验证通过：资产 21/0、领域 28/28、Debug/Release 0 警告/0 错误，全部 Godot 场景测试 PASS。

### 关键决策

- 当前权威合同沿用 Z3/H3：16×10×4.5 m、五湾后吧、五组下柜推拉门、十扇上柜门、空心前吧和水槽下裸露管线。
- 中性轮廓候选只能放在 ignored `artifacts/`；检查点 1 批准前不得创建正式 GLB、包装或取消 placeholder。

### 未完成的待办

1. 建立确定性 Blender 主场景与 `bar_architecture` 中性轮廓候选。
2. 输出建筑内立面、俯视和玩家比例视图并人工检查，然后停在用户检查点 1。
3. 未获批准前不开始前后吧正式候选、材质抛光或 Godot 正式包装。

## 2026-08-03｜Z3/H3 吧台细节修订与下一阶段授权

### 已完成的事项

- 后吧五个下柜已改为双片推拉门；活动门片横移，柜体根、物品宿主和稳定 ID 不动，上柜仍为铰链门。
- 前吧客侧台面只向客区外挑 `0.30 m`；前吧实心主体已替换为空心柜架，抽屉与静态结构在五档行程测试中无重叠。
- 水槽下继续无柜体、储物和玩法碰撞，新增裸露排水、P 弯、墙向回水与冷热供水管线视觉。
- 完整回归通过：资产 16/0、领域 28/28、Debug/Release 0 警告/0 错误，全部 Godot 合同与集成场景 PASS。
- 已生成并逐张人工检查 20 张 Vulkan Forward+ `1920×1080` PNG；证据与哈希见 `docs/assets/BAR_GRAYBOX_Z3_H3_DETAIL_FIX_CAPTURE_20260803.md`。

### 关键决策

- 后吧“推拉门”只指五个下柜；十个上柜继续保持铰链门。
- 水槽下“补足内容”解释为裸露管线，不恢复任何已取消的下柜设计。
- 不以关闭碰撞、缩短行程或移入通道掩盖抽屉重叠；前吧必须保持真实空心结构。
- 用户已授权本轮修订完成后自行执行下一步计划，不再等待灰盒二次复核。

### 未完成的待办

1. 执行生产模型计划 Task 6：模块化环境 GLB 合同、资产清单和验证器自测。
2. 依建模 Skill 执行 Task 7 至中性建筑轮廓审阅点；未获用户视觉批准前不继续正式吧台候选与材质抛光。
3. 正式配方、容量、平衡、顾客内容与最终美术仍未批准，不得扩展。

## 2026-08-03｜完整酒吧灰盒 Z3/H3 打回重建

### 已完成的事项

- 已将旧灰盒正式标记为被打回，不再沿用 12 m × 9 m × 3.5 m、5.60 m 前吧及旧 Task 1–5 的完成结论。
- 已完成并获批返工设计 `docs/superpowers/specs/2026-08-03-bar-graybox-rework-design.md`，覆盖新尺度、功能收敛、连续多边形台体、封闭收纳、空瓶架、运行时包络测试和 16 视角实机验收。
- 用户已明确不需要再次复核书面规格，授权直接制作灰盒样式后再检查实际运行截图。
- 已完成连续多边形 9.10 m 前吧、Z3 两侧功能收敛、H3 高度、五湾两层空瓶架、23 个封闭互锁储物正面和柜内物品宿主；旧倒角叠片、重叠台面、连续瓶架、占位酒瓶与东侧旧湿区结构均已硬删除。
- 已完成自动回归：资产 16/0、领域测试 28/28、Debug/Release 0 警告/0 错误；布局、储物、运行时几何、冒烟、Stage 1/2、输入与流程测试全部 PASS。
- 已生成并人工检查 `artifacts/visual_review_bar_graybox_z3_h3/` 的 16 张 1920×1080 Vulkan Forward+ 审批图；结构图有捕获专用中性补光，现实／眼镜灯光图不加补光，逐图 SHA-256 见 `docs/assets/BAR_GRAYBOX_Z3_H3_CAPTURE_20260803.md`。

### 关键决策

- 房间为 `16 × 10 × 4.5 m`；前吧东西长 `9.10 m`；玩家／客侧台面高 `1.12 / 1.38 m`；玩家眼高保持 `1.83 m`；南北操作通道 `1.55 m`。
- 西侧只设置操作手册；东侧只设置弃物区和员工门；前吧东端水槽下方完全开放，不得残留柜体、抽屉、门板、碰撞或旧湿区节点。
- 工具、杯具、咖啡原料、水壶和冰桶每日初始全部位于关闭的抽屉／柜内；瓶架保留供后续瓶装原料使用，但本轮仅建五湾两层规整空架。
- 前吧视觉与碰撞从同一连续多边形轮廓派生，旧独立倒角片、重叠台面和旧连续瓶架必须硬删除；不得通过隐藏、缩放或移出房间掩盖失败。
- `export_presets.cfg` 是用户改动，继续保留且不纳入提交；不推送、不发布。

### 未完成的待办

1. 等待用户检查本轮 16 张新灰盒截图；若需修改，只回到当前灰盒修正、回归和重拍。
2. 用户明确批准新灰盒后，才可开始正式环境 GLB 合同与 Blender 模块化几何。
3. 正式配方、容量、平衡、顾客内容与最终美术仍未批准，不得因灰盒通过而自行扩展。

## 2026-08-02｜已被打回的完整酒吧灰盒审批历史

### 已完成的事项

- 已按批准规格完成计划 Task 1–4，并完成 Task 5 的截图制作与人工检查：权威布局现为 12 m × 9 m × 3.5 m、1.20/0.96 m 错层台面、1.83 m 眼高、1.40 m U 内通道、0.38 m 抽屉行程。
- 已重建分段建筑墙体、南侧 1.40 m 双扇入口、东侧唯一 3.20 m × 1.55 m 景观窗、北东约 0.90 m 侧门、U 形吧台、东湿区/员工门、西手册架、三湾后吧、八抽屉/六上柜门、六吧椅、三桌十二椅和 3/2/4/2 专用灯组。
- 已完成第二轮合同审计并补齐五项缺口：水槽下方开放净空、拉出客椅 1.20/0.90 m 路线及入口／南窗维护区、两处 45° U 台内倒角、吊顶与绕开开口的 1.05 m 墙裙、东西向后吧线性灯条。
- 已将建筑碰撞与灰盒视觉拆分；可动抽屉/柜门/员工门/手册保持 `NeutralGameplay` 唯一权威节点。手册已具备 `Grip`、`Placement`、封面和左右页枢轴，阅读/翻页/放回动画仍按规格延期。
- 已新增 `BarProductionLayoutContractTests` 并接入一键验证；修正相机射线/探针可能命中玩家自身，以及抽屉行程由储物深度隐式推导的问题。
- 已运行完整回归，退出码 0：资产 16 项/0 错误、领域测试 28/28、Debug/Release 0 警告/0 错误，布局合同、冒烟、Stage 1/2、输入和流程测试全部 PASS。
- 已用实际 Forward+ Movie Maker 重新生成 11 张 1280×720 审批图，并逐张检查方位、玩家视角、U 内部、湿区/员工门、水槽下净空、手册、开上柜门、开冰桶抽屉、拉出客椅、南门窗、吊顶／墙裙、现实/眼镜灯光。最终路径与 SHA-256 见 `docs/CONTEXT_HANDOFF.md`。

### 关键决策

- 当前灰盒是正式 Blender 环境建模前的尺度/方位验收基线；截图通过只批准继续 Task 6–10，不等于批准最终几何、材质或美术定稿。
- 北侧后吧、南侧前吧、玩家位于 U 内并面向世界南 `+Z` 的方位不再从旧截图或旧灰盒推断。
- `export_presets.cfg` 仍是用户未提交改动，未纳入本阶段；本轮不推送、不发布。

### 未完成的待办

1. 等待用户明确批准或指出需调整的灰盒尺度与方位；若需修改，只回到 Task 1–5 修正、回归和重拍。
2. 获批后执行 Task 6–10：环境 GLB 合同、可复现 Blender 主文件、六个模块中的建筑/吧台/后吧/家具/灯具几何与首轮材质检查，并在第二审批门暂停。
3. 第二审批门通过后再执行 Task 11–13：Godot 包装、双世界材质/照明、可动件视觉绑定、严格回退验证和最终截图。

## 2026-08-02｜建模工作流与 Skill 融合归档

### 已完成的事项

- 已定位并融合隔离分支 `codex/stage1-asset-modeling`，来源归档点为 `bbc4011`。该分支包含 Stage 1/2 共九项已批准 M1 技术／材质候选、确定性 Blender 生成器、手写 Godot 包装、双世界／双手表现、集成测试和批次记录。
- 已融合仓库本地 `.agents/skills/modeling-glasses-bar-assets/`：含 Skill、UI 元数据、批次初始化／阶段校验脚本、三个阶段 reference、七个骨架模板和三类压力场景。
- 已恢复此前暂停的 Skill 验证：官方静态校验通过、两个 Python 脚本编译通过、框架测试 7/7、工具测试合计 9/9，四个 fresh-context GREEN 场景均守住配置、轮廓、Forward+、完成归档与无关改动门禁。
- 已在完整融合结果上运行回归：资产 16 项 0 错误、领域测试 28/28、Debug/Release 0 警告/0 错误，Godot 导入、冒烟、Stage 1、Stage 2、输入与流程测试全部 PASS。
- 融合时修复 Skill 校验器与既有仓库实践的冲突：Godot 自动生成且已跟踪的正式 `.glb.import` 不再使任何新批次永久失败；候选 `artifacts/`、PNG 和 `.blend` 仍由校验器拒绝，手工 `.glb.import` 改动由 Git diff 审查。

### 关键决策

- Stage 1/2 的九项 GLB 是用户批准进入原型的 M1 候选，不是最终美术定稿；稳定 ID、碰撞、玩法状态、现实可交互／眼镜只观察和灰盒回退保持权威。
- `modeling-glasses-bar-assets` 负责可复用的批次流程门禁，不替代项目 Skill `develop-glasses-bar-godot`，也不从旧批次推断新批次批准。
- 独立 Stage 3 固定站点批次继续暂停。当前 Goal 只执行已批准的完整酒吧环境计划；环境计划中的 `bar_counter` 模块不自动授权五个固定站点独立正式资产。
- 原隔离分支作为 Git 历史来源保留到融合后回归完成；用户的 `export_presets.cfg` 修改始终排除在提交之外。

### 未完成的待办

1. 等待用户明确批准或要求调整完整酒吧灰盒的尺度与方位；若需调整，只回到 Task 1–5 修正、回归和重拍。
2. 在用户明确批准灰盒尺度与方位前，不开始 Task 6 或任何正式环境 GLB。

## 2026-08-02｜完整酒吧生产模型实施计划

### 已完成的事项

- 已完成并逐项锁定完整酒吧的建筑、U 形吧台、前吧、东西侧功能、后吧、门窗、顾客桌椅、吧椅、吧台内外照明、材质、双视觉状态和 GLB/Godot 交付规格。
- 已完成权威设计规格：`docs/superpowers/specs/2026-08-02-bar-layout-production-model-design.md`。
- 已完成测试优先实施计划：`docs/superpowers/plans/2026-08-02-bar-layout-production-model.md`。计划含 13 个任务，覆盖完整灰盒校准、两次用户验收关口、六个模块化 GLB、Godot 包装、双世界材质/照明、灰盒回退、自动回归和 Forward+ 截图复核。
- 已更新 `docs/CONTEXT_HANDOFF.md`，记录计划路径、执行顺序和验收边界。
- 已完成计划文档自审：无未决占位符；已核对方位、尺度、门窗、湿东干西、手册架、高频工具、抽屉行程、顾客桌椅、四壁灯/两填光、三吊灯/两线灯、取消吧台固定脚踏结构、双世界共享状态和灰盒回退。

### 关键决策

- 正确方位固定为北侧后吧、南侧横向前吧、两侧台向北延伸；玩家在 U 形内部并向南面向顾客。
- 旧灰盒的 14 m × 13.3 m 房间、1.42 m 前吧、2.00 m 眼高、2.31 m 通道和约 0.62 m 抽屉行程均被新规格取代。
- 必须先完成 12 m × 9 m 建筑和 1.20/0.96 m 错层吧台、1.78–1.88 m 眼高、1.40 m 操作通道、0.38 m 抽屉行程的可玩灰盒，并由用户按实机截图批准后，才可开始 Blender 正式模型。
- 正式资产拆为 `bar_architecture`、`bar_counter`、`bar_backbar`、`bar_furniture`、`bar_lighting`、`bar_wear_overlays` 六个模块；GLB 只负责视觉，Godot 包装负责碰撞、灯光、交互、稳定 ID 和状态。
- 静态资源在现实/眼镜世界复用同一 GLB；抽屉、柜门、员工门和手册只保留一套权威玩法状态。任何正式模块加载失败时，整套视觉回退到完整灰盒。
- 《美术设计案》作为低多边形复古 3D、手绘基础色、AO 和简化 PBR 的美术参考，但不得覆盖本轮已批准的布局、尺度、材质分配和交互决定。
- 所有附着于前吧的连续脚踏杆和固定脚踏板保持取消；只保留踢脚槽和每张独立吧椅自身的脚环。
- 本轮按用户提供的仓库指令在 `codex/integrate-modeling-workflow` 提交阶段性成果；不推送或发布。用户已有的 `export_presets.cfg` 修改必须保留且不得纳入任何提交。

### 未完成的待办

1. 验证并融合隔离工作树中的 Stage 1/2 正式候选、Godot 包装、测试和 `modeling-glasses-bar-assets` Skill，归档原分支来源与验证证据。
2. 从实施计划 Task 1 开始，先写失败的酒吧布局契约测试，再修改 `BarLayoutDefinition`。
3. 完成建筑、U 形交互区、客区桌椅和完整灯组灰盒，运行回归并生成 Forward+ 实机截图；等待用户批准尺度和方位。
4. 灰盒批准后再建立 GLB 合同、Blender 主文件和六个正式模块；每个重要几何/材质阶段等待用户复核。
5. 完成 Godot 包装、双世界材质/照明切换、可动件绑定、完整灰盒回退、严格资产验证、自动回归和最终视觉验收。
6. 阶段性提交继续排除 `export_presets.cfg`；不得推送或发布。
## 2026-08-02｜第二阶段手持资产完成

### 已完成的事项

- 用户已明确通过 Blender 轮廓检查点与最终 Godot Forward+ 检查点；阶段 2 的 `traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large` 已完成可复现 GLB、手写包装、世界／双手接入与视觉复核。
- 轮廓返修已落实：滤器握把与主体相连且内部漏斗路径清楚；豆铲缩小为宽口、浅腹、低前缘；冰夹为轻薄张开的双金属片、凹形小勺夹头和可见 U 形弹性连接。
- 材质返修已落实：滤器为暖金属，豆铲／冰夹为深色缎面旧钢，小／大量酒器为冷银主体与亮银杯沿／腰环；现实材质与眼镜世界观察覆盖均已实机检查。
- 两份正式资产清单只将上述五项改为非占位。阶段 1 与阶段 2 共 9 项为批准进入原型的 M1 技术／材质候选；阶段 3／4 的其余 7 项继续保持灰盒。
- 已运行 `powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1`，退出码 `0`：资产 `16` 项／`0` 错误，领域测试 `28/28`，Debug／Release `0` 警告／`0` 错误；Godot 导入、`SMOKE_TESTS_PASS`、`STAGE1_ASSET_INTEGRATION_PASS`、`STAGE2_ASSET_INTEGRATION_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS` 全部通过。
- 两次批准、最终关键帧、返修过程和验证细节已归档到 `docs/assets/STAGE2_ASSET_BATCH_20260802.md`。

### 关键决策

- 本批批准的是 M1 技术／材质候选，不是最终美术定稿；后续仍可在稳定 ID 和手写包装合同下替换视觉子场景。
- 稳定 ID、碰撞、权威玩法状态、现实可交互／眼镜只观察、左右手与每日复位行为保持不变；保持行为测试，不用精确美术常量断言冻结模型。
- 正式容量、配方、平衡、顾客内容均未修改。眼镜世界相邻量酒器标签略有重叠，但不遮挡模型，作为非阻塞原型问题保留。
- 阶段 3 未开始；阶段 2 的通过不自动授权固定站点建模、推送或合并。

### 未完成的待办

1. 等待用户明确指定是否开始阶段 3 的固定交互站点；在此之前不制作 `water_kettle`、`ice_bucket`、`bar_sink`、`coffee_beans`、`waste_bin`。
2. `cutting_board` 与 `bar_counter` 仍属于更后的阶段 4；以上 7 项全部继续保持灰盒回退。
3. 等待正式容量／冰美式配方／容差／评价经济、顾客内容和最终美术；真实键鼠手感仍需用户在可玩包中持续验收。

## 2026-08-02｜第二阶段手持资产设计批准

### 已完成的事项

- 用户已批准第二阶段五项资产：`traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large`。
- 已完成三轮关键设计选择：传统过滤器采用紧凑金属滴滤杯轮廓；材质采用 B×C“混合 C”；生产审核采用先轮廓成组、再统一精修的方案 B。
- 已建立书面规格 `docs/superpowers/specs/2026-08-02-stage2-handheld-assets-design.md`，整理资产轮廓、材质映射、锚点、包装合同、两个用户审核门、技术边界与完成条件。
- 用户已复核通过书面规格；已建立详细实施计划 `docs/superpowers/plans/2026-08-02-stage2-handheld-assets.md`，把正式清单切换严格放在 Godot 检查点 2 通过之后。
- 当前尚未开始第二阶段建模、GLB 导出、清单切换或 Godot 集成；现有灰盒状态保持不变。

### 关键决策

- `traditional_filter` 为低矮暖金属滴滤杯，带短侧握把、可读网面与底部出口；不采用布袋支架或长柄滤篮。
- `bean_scoop`／`ice_tongs` 使用深色旧钢缎面，`jigger_small`／`jigger_large` 继承已批准中号母版的亮银家族。
- 检查点 1：五件低细节模型在 Blender 中同屏审核轮廓、尺度、操作方向和锚点；用户通过前不做材质精修。
- 检查点 2：实际 Godot Forward+ 世界、手持和组合截图审核；用户通过前不报告阶段完成。
- 稳定 ID、手写包装、灰盒回退、左右手、液体、配方和双世界玩法合同保持不变。

### 未完成的待办

1. 用户选择逐任务代理执行或当前对话内联执行。
2. 按检查点 1 完成五件轮廓阻塞与 Blender 队列，等待用户批准后再进入材质、GLB 和 Godot 集成。
3. Godot 检查点 2 通过前不得切换正式清单为非占位，也不得报告第二阶段完成。

## 2026-08-02｜第一轮资产建模完成

### 已完成的事项

- 已完成高球杯、中号量酒器、研钵和研杵四项米制低多边形 GLB、确定性 Blender 生成器、中性棚拍脚本与手写 Godot 包装；稳定根 ID、必需锚点、网格、PBR 材质、变换和包络均通过校验。
- 已新增 `ToolVisualLibrary`，集中四项稳定 ID 到包装场景映射、现实/眼镜材质切换和手持姿态；世界工具与左右手会优先使用包装，缺失时恢复原灰盒。
- 已用失败→通过的阶段 1 资产集成测试锁定包装、锚点、接触面、双手替换、世界切换和回退合同；输入测试同步验证批准包装。
- 已完成多轮 Godot Forward+ 校准：修正世界模型悬空、研钵手持遮挡、研钵暗场可读性，并根据用户反馈强化量酒器拉丝银与高球杯透明杯壁/亮杯沿/厚底。
- 用户已通过最终材质审核；四项清单状态已改为非占位，其余 12 项继续保持灰盒。
- 完整验证已通过：资产 16 项 0 错误、领域测试 28/28、Debug/Release 0 警告/0 错误，Godot 导入、`SMOKE_TESTS_PASS`、`STAGE1_ASSET_INTEGRATION_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 最终棚拍位于 `artifacts/visual_review_20260802_stage1_material_round4/`，最终游戏画面位于 `artifacts/visual_review_20260802_stage1_godot_round4/`；详细哈希与返修记录见 `docs/assets/STAGE1_ASSET_BATCH_20260802.md`。

### 关键决策

- 四项是用户批准进入原型的 M1 技术/材质样件，不等于最终美术定稿；后续仍可通过稳定包装替换 GLB，而不改玩法状态和碰撞合同。
- 导入 GLB 只作包装的视觉子场景；稳定 ID、Gameplay 状态、碰撞和眼镜世界切换仍由手写代码/场景持有。
- 量酒器采用暗场可读的游戏化拉丝银，不追求缺少环境反射时的纯金属物理结果；高球杯以透明杯壁、亮杯沿和厚底优先保证第一人称辨识。

### 未完成的待办

1. 第一轮无遗留实现待办；保持阶段 1 已验收状态。
2. 等待用户决定是否开始阶段 2 或继续返修本批；在收到新指令前不制作其余 12 项资产。
3. 正式配方、平衡、顾客内容和最终美术仍未批准，不得把本批技术样件误报为最终资产。
## 2026-08-02｜核心交互物品建模计划

### 已完成的事项

- 已依据《美术设计案》与现有 16 项资产清单建立 `docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md`，覆盖 9 件手持工具、5 个固定交互站点和 2 项承载结构。
- 计划采用风险优先混合排期：阶段 1 先以高球杯、中号量酒器、研钵和研杵锁定玻璃/金属/复合材质、握持、摆放、容积与双手组合，再批量制作其余资产。
- 单人总排期锁定为 28–39 个工作日，包含阶段级返修缓冲、独立美术评审和较高交互精度的 Godot 校准。
- 已逐项整理稳定 ID、负责人、初始状态、依赖、必需锚点、灰盒包络参考、交互重点、六道验收门和批次记录模板。
- 本轮只完成建模准备与计划文档，尚未制作、导出或接入任何正式 GLB；现有 16 项仍是灰盒占位。
- 计划文档完成后已运行完整回归：资产 16 项 0 错误、领域测试 28/28、Debug/Release 0 警告/0 错误，Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS` 均通过；本轮无视觉改动，不新增截图。

### 关键决策

- 建模基线保持 Blender 4.5.5 LTS、米制、`+Y` 向上、`-Z` 向前；正式候选必须应用变换并通过稳定 ID、锚点、PBR 材质和 GLB 校验。
- 优先提高交互精度而非盲目增加面数：每件资产需校准握持、摆放、倾倒、液面、碰撞或交互焦点，并经过独立美术评审。
- `bar_sink` 资产对应运行时 `hand_wash_sink`，`water_kettle` 资产对应运行时 `kettle`；两侧稳定 ID 均不得互相改名。
- 当前双头量酒器先制作金属候选；设计案中的透明量杯示例不直接覆盖现有量酒器定义，最终材质在阶段 1 美术评审锁定。
- GLB 继续作为手写玩法包装场景的视觉子场景；碰撞、玩法脚本、锚点修正和眼镜材质覆盖不直接挂在导入节点上。
- 灰盒只有在对应资产通过技术、美术、交互、接入和实际截图复核后才允许替换。本计划不批准人物、环境扩展、正式配方或最终美术细节。

### 未完成的待办

1. 执行阶段 0：建立 Blender 制作模板，为 16 项资产补齐尺寸卡、接触面、原点、锚点和碰撞预期。
2. 执行阶段 1：制作高球杯、中号量酒器、研钵和研杵样件，完成两轮 Godot 校准与两轮美术评审。
3. 样件组通过后，按计划依次完成其余手持工具、固定站点、承载结构和全集成回归；每批更新状态与验收证据。
4. 正式冰美式配方、平衡值、最终传统滤具造型与最终量酒器材质仍需用户/策划/美术批准，不得在建模过程中自行固化。

## 2026-07-29｜第七批职责迁移：共用设置状态与服务

### 已完成的事项

- 新增无 Godot 依赖的 `SettingsState`，统一保存并归一化主音量 `0–100%` 与鼠标灵敏度 `0.001–0.006`，同时提供既有 `1.0–6.0` 滑杆映射。
- 在 `Main.tscn` 新增单一场景级 `SettingsService`，从现有 Master bus/`PlayerController` 初始化，并成为运行时应用音量和灵敏度的唯一入口。
- 新增表现侧 `SettingsPanelBinding`，统一同步滑杆、`0%`/`0.0` 标签与 service change event；主菜单和暂停菜单不再直接读写 AudioServer 或玩家灵敏度。
- `OpeningMenuController` 从 254 行降为 237 行，`PauseMenuController` 从 114 行降为 99 行；两者只保留各自页面、焦点、暂停、重开和返回行为，原节点路径与 UI 结构未改。
- 新增 1 项领域测试覆盖设置上下界归一化；输入集成新增主菜单 → 暂停菜单、暂停菜单 → 主菜单的双向实时同步，以及 Master bus 和玩家实际灵敏度断言。
- 新增可复用 `SettingsVisualCapture` 与主/暂停两个捕获场景，便于后续设置 UI 回归。
- 完整验证通过：资产 16 项 0 错误、领域测试 28/28、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 已人工检查两张 Forward+ 最终帧：主菜单设置页 `opening_settings00000044.png` SHA-256 `1CD5A778FB75C78D667A4AEFA111DD757BD0E82563875D626B9AD0D23FD31D8F`，暂停设置页 `pause_settings00000044.png` SHA-256 `8A819249AD8E7C58B056F723BF4C984C1312FE4E8FE995870A00D01D91785B7C`；默认 `100% / 2.2`、滑杆、按钮和暂停遮罩均正常。

### 关键决策

- `SettingsState` 是纯值对象，`SettingsService` 是当前运行时设置的唯一 owner；菜单与 binding 只展示/发起修改，不各自持有第二份设置值。
- 当前只统一运行时状态，不自行新增磁盘设置持久化；退出重开后的保存仍与正式存档产品层一起规划。
- 音量线性下限 `0.001`、灵敏度范围/格式、默认值和暂停时可调语义均为原行为迁移，不新增选项或平衡。
- 动画、IK、骨骼等表现隔离约束继续有效；设置服务不扩大 Gameplay 对表现实现的依赖。

### 未完成的待办

1. P2：按架构审查继续处理可组合配方条件/效果，需先保持当前原型配方与评价完全等价。
2. P2：为 schema version 增加显式迁移器和损坏存档回退；磁盘槽/继续游戏仍属于后续产品层。
3. P2/M3：集中工具/站点眼镜材质与标签到 presenter，并等待正式 GLB；正式配方/平衡和真实键鼠手感验收仍是外部阻塞。

## 2026-07-29｜第六批职责迁移：站点定义与动作 handler

### 已完成的事项

- 新增 `StationDefinition`/`StationCatalogDefinition` Resource 与 `prototype_station_catalog.tres`，集中 6 类站点的稳定 ID、Kind、显示名、handler ID、原料/数量、动态提示模板、眼镜世界提示可见性和交付距离。
- 新增 `StationDefinitionCatalog`，加载并校验站点 ID/Kind 唯一性、必填显示名/handler、原料源配置与顾客正交互距离；布局 ID/Kind 与定义不一致时构建立即失败。
- 新增 `StationActionHandlerRegistry`，注册顾客、原料源、每日洗手、水壶和弃物桶 5 类 handler；提示、动作定义、许可、不可用原因和执行结果均从 `StationInteractable` 的 Kind `switch` 迁出。
- `StationInteractable` 从 140 行收敛为 94 行通用会话/世界/柜体门控、definition/handler 解析和 `IInteractable` facade；公开 `Kind`/`EntityId`、动态建站方式和直接测试调用保持兼容。
- `GameplaySceneComposer` 在创建站点时绑定已校验 Resource；冰桶随原抽屉创建/开合，不改变节点路径、碰撞或表现构建。
- 流程集成新增 6 个定义、6 个唯一 Kind、全部 handler 已注册、顾客 `3.15 m`、冰 `1` 块、咖啡豆 `0.25` 份以及 6 个运行时节点/定义一致性断言；既有流程继续实际覆盖接单、交付、洗手、量水、取冰/豆和清废。
- 完整验证通过：资产 16 项 0 错误、领域测试 27/27、Debug/Release 0 警告/0 错误、Godot Resource 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 已运行 Forward+ `InteractionFeedbackVisualCapture` 并人工检查，最终帧为 `artifacts/visual_review_20260729_station_refactor/feedback00000044.png`，SHA-256 `5C95D1B6B5A1489226F07F5D1319C6F6AA503B99C05DAFAC412198A2E7403173`；接单反馈、阶段切换和交付不可用提示显示正常。

### 关键决策

- Resource 只描述站点配置，不保存运行时玩法状态；handler 只通过 `GameSession`/`DrinkWorkstation` 的既有 API 评估并执行动作，站点 Node 不再按 Kind 决定规则。
- 顾客交付距离、原料份量和全部中文提示是原值迁移，不属于新平衡或正式内容；咖啡豆 `0.25` 仍显式标记开发占位。
- `StationKind` 暂时保留为布局/表现和旧调用兼容字段，但新增玩法站点必须先添加定义与已注册 handler，不能重新扩大节点 `switch`。
- 后续动画、IK、骨骼等表现仍只能单向消费事件、signal 或动作结果；站点 handler 不得引用或控制表现实现。

### 未完成的待办

1. P1：下一批抽出主菜单与暂停菜单共用的 `SettingsState/SettingsService`，保持音量、鼠标灵敏度和 UI 交互不变。
2. P2：继续按审查处理可组合配方条件/效果、显式存档迁移器，以及正式资产到 presenter 的集中映射。
3. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜第五批职责迁移：玩家控制器

### 已完成的事项

- 新增 `PlayerMotor`，从 `PlayerController` 接管移动输入、重力、视角旋转、跨天姿态复位以及 schema version 1 玩家姿态快照往返。
- 新增 `InteractionSensor`，按原有顺序使用 `InteractionRay` 优先、`InteractionProbe` 兜底，只返回交互目标和命中点，不评估或执行玩法规则。
- 新增 `PlayerActionInput`，把既有输入映射转换为统一动作管线请求，驱动连续动作、提示检查、提交/取消和反馈；原 action ID、phase、目标 ID 与提交时机保持不变。
- 新增 `HeldToolPresenter`，只消费 `DrinkWorkstation` 的 `HandsChanged`/`HandToolIdsChanged` signal 并更新现有手持 Mesh、可见性和隐藏标签；Gameplay 不引用该 presenter。
- `PlayerController` 从 382 行收敛为 98 行 Godot 生命周期、公开 API、signal 和协作者组合 facade；`Main.tscn` 挂载、节点路径、导出参数、HUD/菜单/存档调用均未改变。
- 输入集成回归新增玩家姿态快照/零速度恢复，以及研钵/研杵 hand signal → Mesh/可见性同步与每日重置断言。
- 完整验证通过：资产 16 项 0 错误、领域测试 27/27、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 已运行 Forward+ `ToolHandsVisualCapture` 并人工检查，最终帧为 `artifacts/visual_review_20260729_player_refactor/hands00000044.png`，SHA-256 `F03D29EDFB8BC6C69B2D8053F72B762E749437882E26EFD04A63238474612921`；左右手灰盒、HUD 手位和交互反馈显示正常。

### 关键决策

- 玩家姿态仍是唯一权威状态，`PlayerMotor` 只负责读写该姿态；动作过程继续由 `GameplayActionPipeline` 持有，交互探测不拥有玩法状态。
- `HeldToolPresenter` 是单向表现消费者。后续动画、IK、骨骼只能替换或扩展表现层，并通过事件、signal 或动作结果接收通知；Gameplay 不得依赖、查询或控制其实现，表现层不得决定玩法结果。
- 第五批保留 `PlayerController` 作为稳定 Godot facade，避免场景、HUD、菜单、交互上下文和存档系统在职责迁移时同时改接口。
- 第五批完成不代表整体架构重构完成。

### 未完成的待办

1. P1：下一批拆分 `StationInteractable`，引入站点定义并以动作 handler 注册替代 Kind `switch`，保持所有提示、许可、反馈和玩法结果不变。
2. P1：随后抽出主菜单/暂停菜单共用的 `SettingsState/SettingsService`。
3. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜第四批职责迁移：灰盒场景组合

### 已完成的事项

- 新增 `scripts/world/BarLayoutDefinition.cs`，集中当前灰盒的尺寸、坐标、稳定节点 ID、站点、工具、砧板、柜体和冰桶布局；构建前校验 ID 唯一性、正尺寸、三槽砧板、单一冰桶抽屉与水槽下方净空。
- 新增 `GrayboxArchitectureBuilder`，只根据布局数据创建现实/眼镜建筑、材质、标签和静态碰撞，不读取或修改任何玩法状态。
- 新增 `CabinetBuilder`，只组装现有 `CabinetInteractable`、抽屉空腔与内置冰桶节点；柜体开合状态继续由各柜体实例持有。
- 新增 `GameplaySceneComposer`，创建工作台、台面、站点、砧板和工具，并绑定玩家、HUD、主菜单、暂停菜单与会话重置。
- `GrayboxLevelBuilder` 从 470 行收敛为 52 行 Godot 组合根；原有公开布局常量继续转发，所有节点名/路径、坐标、尺寸、颜色、碰撞层、创建顺序、signal 和重置语义不变。
- 流程测试新增布局定义不变量断言，锁定 5 个站点、9 件工具、8 个抽屉、6 扇柜门、三槽砧板及 `front_drawer_2_upper` 冰桶位置。
- 完整验证通过：资产 16 项 0 错误、领域测试 27/27、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 已运行 Forward+ `BarLayoutVisualCapture` 并人工检查；当前最终帧与 2026-07-23 已验收基线大小均为 162,717 字节，SHA-256 均为 `5E610186D5C7FD7FDC6265325CCC2A5521AFD37ECD573BF8120C37F5B916BE51`，视觉输出逐像素一致。

### 关键决策

- `BarLayoutDefinition` 是当前灰盒空间数据的唯一来源，但它不属于 Gameplay 状态，也不进入存档；建筑/材质构建器只能消费布局数据，不能反向控制玩法结果。
- `GameplaySceneComposer` 只创建 adapter 并连接现有 owner，不获得工具、饮品、会话或柜体状态所有权。
- 后续动画、IK、骨骼仍是可替换表现实现。Gameplay 只能通过事件、signal 或动作结果通知表现层，不得直接依赖、查询或控制这些对象。
- 第四批只完成灰盒场景组合职责拆分，整体架构重构仍继续。

### 未完成的待办

1. P1：下一批拆分 `PlayerController`，分离 `PlayerMotor`、`InteractionSensor`、`PlayerActionInput` 与 `HeldToolPresenter`，保持玩家姿态/输入/动作提交行为不变。
2. P1：随后拆分 `StationInteractable` 和主/暂停菜单共用设置服务。
3. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜第三批职责迁移：当前饮品组装状态

### 已完成的事项

- 新增纯领域 `src/Domain/DrinkAssemblyState.cs`，统一持有当前杯、液体组成、耗时、浪费、溢出、失败次数、已完成步骤、完成度、丢弃/重做、评价输入、每日重置和工作台快照映射。
- 将 `LiquidContainer` 从 Godot 玩法目录迁入 `src/Domain/LiquidContainer.cs`，删除不再需要的 `ILiquidContainer`/`IProcessLiquidTarget` 临时接口；液体容量、混合比例、溢出与恢复算法未改变。
- `ProcessExecutionService` 改为在构造时依赖稳定的 `DrinkAssemblyState`，不再直接修改 `DrinkSnapshot`，也不再由 facade 在每次工序调用时传入液体目标。
- `DrinkWorkstation` 保留原有 `Glass`、`TotalWaste`、评价、重置和存档公开行为，只负责 Godot signal、表现同步、卫生/水壶环境输入、反馈格式化与跨服务编排。
- 新增 4 项领域测试，覆盖丢弃后重做评价隔离、跨天饮品/当日指标清零、schema version 1 恢复语义和混合液体比例移除；已有工序测试同步改为验证正式饮品 owner。
- 完整验证通过：资产 16 项 0 错误、领域测试 27/27、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。本批无视觉改动，没有新增截图。

### 关键决策

- `DrinkAssemblyState` 是当前饮品与当日制作指标的唯一 owner；`LiquidContainer` 只负责杯中液体本身，`ProcessExecutionService` 只提交工序结果，`DrinkWorkstation` 不再直接改写饮品快照。
- `WorkstationSnapshot` 继续使用 schema version 1，字段、JSON 形状和恢复语义不变；随机数消费时机、配方判定、完成度传播、溢出/浪费累计与跨天重置均保持原行为。
- 后续动画、IK、骨骼等表现系统继续遵守硬边界：Gameplay 只能通过事件、signal 或动作结果通知表现层，不得依赖、查询或控制动画播放器、IK 求解器、Skeleton/Bone；表现层不得反向决定玩法或存档结果。
- 三批 `DrinkWorkstation` 迁移已经完成既定工具、工序、当前饮品 owner 拆分，但不代表整体架构重构结束。

### 未完成的待办

1. P1：`GrayboxLevelBuilder` 已完成；下一步拆分 `PlayerController`。
2. P1：随后按审查顺序拆分 `StationInteractable` 和共用设置服务。
3. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜第二批职责迁移：工序执行服务

### 已完成的事项

- 新增 `src/Domain/ProcessExecutionService.cs`，把工序目录、砧板能力/选择、来源合并、规则鉴定、成功输出、失败废品、重复补救、液体接纳/溢出和工序统计从 `DrinkWorkstation` 迁入纯 C# 服务。
- 新增类型化 `ProcessExecutionOutcome`、`ProcessBlockReason` 与 `ProcessTransitionHint`；`DrinkWorkstation` 保留原有公开 API，只负责把 outcome 格式化为既有中文反馈、发布 signal 和提供卫生/水壶环境输入。
- `LiquidContainer` 通过纯领域 `IProcessLiquidTarget` 窄端口接入；工序服务不引用 Godot Node、材质、动画、IK、骨骼或 UI。
- 新增 4 项领域测试，覆盖精确工序选择与输出提交、液体接纳/溢出、错误材料变废，以及缺水/重复补救的随机数消费边界。
- 完整验证通过：资产 16 项 0 错误、领域测试 23/23、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。本批无视觉改动，没有新增截图。

### 关键决策

- `ProcessExecutionService` 是工序选择与提交的唯一应用服务；Gameplay facade 不再直接合并来源、调用规则或写入工序输出/废品。
- 随机数仍由 facade 提供为延迟调用：缺水、重复补救动作不足和重复补救错误工具路径不会额外消费排队鉴定值，普通工序保持既有取得时机。
- 本批使用的 `IProcessLiquidTarget` 是迁移期临时边界；其职责已在第三批由 `DrinkAssemblyState` 正式接管。

### 未完成的待办

1. P1：`DrinkAssemblyState` 与 `GrayboxLevelBuilder` 已完成；下一步按审查顺序拆分 `PlayerController`、`StationInteractable` 和共用设置服务。
2. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜架构审查后首批职责迁移

### 已完成的事项

- 新增 `src/Domain/ToolInventoryService.cs`，把工具实例集合、左右手槽、拿放、防重叠、砧板槽、原材料装载/板上转移、每日重置和工具快照捕获/恢复从 `DrinkWorkstation` 迁入纯 C# 权威服务。
- `DrinkWorkstation` 的现有公开 API、signal、中文反馈与场景节点同步保持不变；`DrinkWorkstation.Persistence.cs` 把工具快照读写委托给新服务。没有改变任何配方、规则、数值、场景或视觉参数。
- 新增 3 项纯领域测试，覆盖手位/防重叠与载料落台限制、砧板内容双向转移、工具状态捕获/恢复/重置。
- 完整验证通过：资产 16 项 0 错误、领域测试 19/19、Debug/Release 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。本轮无视觉改动，因此没有新增截图。

### 关键决策

- `ToolInventoryService` 是工具集合、左右手 ID、砧板工具 ID、工具位置与内容转移的唯一 owner；`DrinkWorkstation` 是 Godot signal facade、表现同步与跨服务编排入口。
- 后续动画、IK、骨骼等均属于可替换表现实现。Gameplay 只能通过事件、signal 或动作结果通知表现层，不得直接依赖、查询或控制动画播放器、IK 求解器、Skeleton/Bone 等对象；表现层不得反向决定动作、工序、配方或存档结果。
- 本轮只完成 `DrinkWorkstation` 的工具库存首批迁移，不能宣称整个工作台拆分完成。

### 未完成的待办

1. P1：`ProcessExecutionService`、`DrinkAssemblyState` 与 `GrayboxLevelBuilder` 已完成；下一步拆分 `PlayerController`。
2. P1：随后按审查顺序拆分 `StationInteractable` 和共用设置服务。
3. P2/M3 与外部阻塞不变：正式存档产品层、正式配方/平衡、首批 GLB 和真实键鼠手感验收仍待后续。

## 2026-07-29｜完整结构审查、纠错与架构边界

### 已完成的事项

- 完成交互、物品、配方、工序、存档和眼镜系统的代码/数据/场景/测试审查。总体架构、状态归属、风险和拆分顺序已写入 `docs/ARCHITECTURE_REVIEW_20260729.md`；逐脚本职责表写入 `docs/SCRIPT_RESPONSIBILITIES.md`。
- 建立统一 `GameplayActionPipeline` 与稳定动作定义。正式玩家输入路径中的拿取/摆放/站点交互、切镜、简易工序、连续砧板工序、量酒器切换和跨天统一经过“只读检查 → 拒绝或开始 → 提交或取消”，并生成 action trace。
- 将工具权威实例和表现分开：`src/Domain/ToolInstanceState.cs` 保存工具位置、手位、砧板槽、内容物、废品、完成度和量酒器端位；`scripts/gameplay/ToolPresentationBinding.cs` 只绑定 Godot 节点。连续摆放坐标不再从视觉节点反向读取。
- 建立存档基础：`GameSaveSnapshot` schema version 1、JSON 序列化、严格一致性校验、`GameSession`/`DrinkWorkstation` 捕获与恢复。集成测试已覆盖会话、工具内容/位置、玩家姿态和冰桶抽屉开合往返。
- 建立工具/工序目录和配方兼容性校验，错误稳定 ID、分类、工具引用、输入输出、结果容器或配方步骤漂移会在创建实例前失败。
- 修复两个真实状态错误：
  - `TotalWaste` 此前不会跨天清零，现已随当天重置。
  - 评价此前读取历史累计原料/完成度，丢掉旧饮品重做后会污染新杯；现从当前高球杯实例重建。
- 将 `RecipeTargets.IsPrototype` 与 `EnableQuantityScoring` 分离，避免原型批准状态和评分策略互相决定；删除未进入运行逻辑的旧交互枚举、重复容器 DTO 和工具字段。
- 更新 `docs/TECHNICAL_DESIGN.md` 的旧布局描述：当前为约 `2.31 m` 关闭通道、8 个前抽屉、6 扇上方大门和约 `1.69 m` 长抽屉打开通行带。
- 最终自动化通过：资产 16 项 0 错误、领域测试 16/16、Debug/Release 构建 0 警告/0 错误、Godot 导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。

### 关键决策

- 定义只描述类型和规则；实例只描述当前实体状态。Resource → 不可变 Spec → 运行时实例，任何实例创建前先做目录交叉校验。
- 交互只发现目标和给出只读许可/提示；动作才修改状态。新增玩家行为不得绕过 `GameplayActionPipeline`。
- 连续动作过程由 pipeline/`IManualOperation` 持有，取消不写玩法状态，完成提交时才结算材料、废品、完成度与统计。
- `RealityWorld`/`GlassesWorld` 仍只负责表现；`GameSession` 是世界模式唯一 owner，工具、订单和饮品不在两个世界复制。
- 存档只保存权威状态及影响玩法可达性的实例状态，不保存 Node、材质、Tween、HUD、提示或半完成动作。
- 当前完成的是 M0 存档状态边界与往返基础，不是正式磁盘存档产品。主菜单“继续游戏”保持禁用，不自行新增存档槽、云同步或正式迁移策略。
- `DrinkWorkstation.Persistence.cs` 只是代码文件拆分；`DrinkWorkstation` 仍拥有过多编排职责，不得误报为状态所有权拆分完成。

### 未完成的待办

1. P1：`ToolInventoryService`、`ProcessExecutionService`、`DrinkAssemblyState` 与 `GrayboxLevelBuilder` 已完成；下一步拆分 `PlayerController`，保留 Godot facade/signal 桥。
2. P1：把 `GrayboxLevelBuilder` 的组合根、布局数据、建筑/柜体/站点生成分离。
3. P1：把 `PlayerController` 的移动、交互探测、动作输入和手持表现分离。
4. P1：将 `StationInteractable` 的 Kind `switch` 改为站点定义 Resource + 动作 handler 注册。
5. P1：提取主菜单/暂停菜单共用的 `SettingsService`。
6. P2/M3：正式存档产品层补充磁盘槽、原子写入、备份、显式 schema 迁移器、损坏回退、“继续游戏”和设置持久化。
7. 外部阻塞继续不变：等待正式冰美式配方/容差/评价经济和首批 GLB；仍需用户真实键鼠游玩检查手感。

## 一、已完成的事项

### 最终视觉稿拆分主菜单

- 已从误发到“黑血之怒”项目的实现中迁回原视觉稿拆分资产，不再仿制一张相似菜单：`assets/ui/main_menu/main_menu_background.png` 是无字背景，`main_menu_title.png` 是独立标题，`main_menu_selector.png` 是可移动选择器，`main_menu_reference.png` 保留完整视觉稿。
- “继续游戏 / 开始游戏 / 设置 / 制作人员 / 退出游戏”均为 Godot 实时文字和透明可点击控件，覆盖在视觉稿对应位置；页面跳转、焦点与点击由控件负责。
- 进入菜单时“开始游戏”获得键盘导航焦点，但鼠标模式下只在光标实际悬停按钮时显示高亮；光标离开按钮后选择器隐藏，不再永久固定高亮“开始游戏”。键盘/手柄输入会恢复高亮并移动独立选择器。
- 继续游戏因尚无存档保持禁用；设置、制作人员、暂停菜单和返回主菜单功能保留。

### 吧台、柜体和操作空间

- 前吧台顶高由 `1.18 m` 调到 `1.42 m`，玩家眼高由 `1.76 m` 调到 `2.00 m`；玩家胶囊体、全部前台工具、砧板、水槽、侧围挡顶面和吧椅均按同一 `0.24 m` 增量协调抬升，不再只抬相机。
- 低位后吧台已完全撤离操作通道，改为后墙 `1.58 m` 高浅架和酒架上方 3 组大门吊柜（6 扇门）；关闭状态的工作通道净宽约 `2.31 m`。
- 用户在首轮 Forward+ 截图中指出吊柜/酒架穿模后，吊柜中心由 `3.74 m` 上移至 `3.90 m`。最终柜体下沿与酒架顶端保留 `0.14 m`，柜门下沿保留 `0.24 m`，自动化测试已锁定最小净空。
- 每日洗手水槽已移回玩家画面左侧的前吧台并独占整格，水槽下方不生成抽屉或柜门；旧后墙水槽旁的双层窄抽屉一并删除。
- 前吧台其余 4 格保留双层深抽屉（共 8 个）。抽屉拉出行程由最高约 `0.34 m` 增至约 `0.62 m`，完全拉开时至后墙浅架仍约有 `1.69 m` 通行带；柜门/抽屉单开互锁继续保留。
- 眼镜世界操作手册从前台水壶/量酒器旁移到侧面围挡台面；工具、站点、砧板与手册 3D 标签同步缩小，实际画面不再由远处标签遮挡手册。
- 砧板画面右下方的上层抽屉内放置冰桶；抽屉关闭时不可取冰。只有冰夹能夹冰，原料勺在能力和交互判定两层都被禁止取冰。
- 柜门、抽屉、工具的灰盒碰撞体已重新按可见 Box/Cylinder/Sphere 尺寸生成；柜门开向、抽屉行程和每日/重开复位均有集成测试。

### 水、卫生与工序补救

- 水槽只用于每日洗手，不再提供制作用水；每天开始为未洗手状态，未洗手对后续概率工序施加 `4%` 的小幅开发占位惩罚，洗手后清除，当天不会重复计罚。
- 制作用水只能由量酒器从水壶取得。已加入三种双头量酒器：小号 `15/30 ml`、中号 `20/40 ml`、大号 `25/50 ml`；按 `F` 切换当前端，水壶不再作为手持直倒工具。
- 萃取缺水现在不会破坏咖啡粉，并区分“水壶无水”和“滤具尚未加入量取的水”；此前测试失败已按“测试时水壶为空”修正诊断，不再把正常规则误判为实现故障。
- 重复萃取和重复过滤可部分恢复该工序损失的完成度，但上限为 `0.96`，不能把偏差完全抹平；恢复比例、目标量和上限均保持 `IsPrototype=true`。

### 既有玩法继续成立

- 接单前允许自由拿取、取料、组合和制作，需求未知且不能交付；接单后现场保留。错误工具、材料、顺序和用量允许真实发生并进入失败、废品或低完成度结果。
- 每件工具仍是唯一运行时实体；左手至多一种放置类，右手至多一种手持类。载料工具不能落普通台面，废品需手动倒入弃物桶。
- 眼镜仍只负责可选观察与辨认；两个世界共享唯一玩法状态，戴镜不锁移动，也不是开始制作的前置条件。
- 30 天周目、逐段近视曲线、客人限距提交、暂停重开、返回主菜单和第 30 天结束均保留。

### 验证、截图和导出

- `tools/run_verification.ps1` 全部通过：资产验证器自测、16 项资产清单 `0` 错误、领域测试 `11/11`、Godot 编辑器导入、`SMOKE_TESTS_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- .NET Debug/Release 构建均为 `0` 警告、`0` 错误；Godot 流程测试实际覆盖每日洗手、三种双头量酒器、水壶余量、干萃取诊断、重复工序补救、冰桶抽屉、冰夹限制、柜体通行、30 天重置与周目结束。
- Forward+ 实际截图已人工检查：
  - 鼠标模式无固定高亮：`artifacts/visual_review_20260722/menu00000001.png`
  - 键盘选择器移动：`artifacts/visual_review_20260722/menu_keyboard00000001.png`
  - 新吧台全景：`artifacts/visual_review_20260722/layout00000002.png`
  - 前吧台深抽屉/冰桶：`artifacts/visual_review_20260722/storage_v200000001.png`
  - 后柜外开与窄抽屉：`artifacts/visual_review_20260722/storage_v200000004.png`
  - 通道安全修正复核：`artifacts/visual_review_20260722_clearance/final00000030.png`、`final00000075.png`
  - 两人通道最终全景/柜体：`artifacts/visual_review_20260722_two_person/layout00000030.png`、`storage00000030.png`、`storage00000075.png`
  - 2026-07-23 加高吧台/吊柜最终全景：`artifacts/visual_review_20260723_playtest_final/layout00000030.png`
  - 长抽屉与前台水槽下方净空：`artifacts/visual_review_20260723_playtest_final/storage00000030.png`
  - 吊柜上移后的酒架净空：`artifacts/visual_review_20260723_playtest_final/storage00000075.png`
  - `2.00 m` 玩家眼高查看上方吊柜：`artifacts/visual_review_20260723_playtest_final/cabinet_pov00000030.png`
  - 侧围挡手册与收敛后的空间标签：`artifacts/visual_review_20260723_playtest_final_v2/manual00000030.png`
- 2026-07-23 Windows 包已按最新布局重导出并实际无头启动，Debug/Release 退出码均为 `0`：
  - Debug：`builds/windows/debug/GlassesBar.exe`，104,523,416 字节
  - Release：`builds/windows/release/GlassesBar.exe`，110,614,680 字节
- 已基于提交 `fcc6546` 生成可直接交付的 Windows 64 位 Release 测试包 `builds/windows/GlassesBar-Prototype-Test-20260723.zip`。包内包含 `GlassesBar.exe`、完整 .NET 运行数据目录和中文测试说明，共 189 个条目、74,242,112 字节；SHA-256 为 `4F7291BA1420ED83ADEA346BA06F775C2A97E6FACEE3533E7BBE902E6123C1B4`。导出日志无错误匹配，实际无头启动退出码为 `0`。
- 已按用户要求创建公开 GitHub 仓库 `https://github.com/huixun431-sketch/glasses-bar`，完整 `main` 历史已推送，本地分支跟踪 `origin/main`。`builds/` 测试包继续保持忽略，既存未提交的 `export_presets.cfg` 自动补全差异没有进入远程历史。

## 二、关键决策

### P0｜不可回退的设计边界

- 主菜单以用户提供的 1672×941 图片为最终视觉稿，结构必须是“无字背景 + 独立标题 + Godot 实时菜单文字/点击控件 + 可移动选择器”，不得再制作相似替代图。
- 菜单高亮必须区分输入模式：键盘/手柄允许默认焦点；鼠标未悬停任何按钮时不得显示焦点高亮。
- 最新空间基准覆盖上一轮数值：前台顶高 `1.42 m`、玩家眼高 `2.00 m`、关闭通道约 `2.31 m`、长抽屉行程约 `0.62 m`。低位后吧台不得重新占用通道；后柜必须位于酒架上方并与酒架保留可见净空。
- 水槽必须位于玩家画面左侧前吧台，水槽正下方不得生成抽屉/柜门；操作手册必须位于侧面围挡，并保持只在眼镜世界显示。
- 吊柜保留大门形式并向外开；前抽屉完全拉开仍需至少一人舒适通行。柜体单开互锁继续作为防困措施。
- 水槽只用于每日洗手，水壶只作为量酒器水源；制作用水必须经过三种双头量酒器。原料勺不能取冰，冰桶必须位于指定上层抽屉。
- “允许玩家犯错”仍是核心。系统只阻止物理不成立的动作；重复萃取/过滤是有限补救，不是无损刷满完成度。
- 所有新增容量、`4%` 卫生惩罚、`0.96` 恢复上限、吧台尺寸和概率仍是开发占位，不能当作正式平衡或正式配方。
- 用户已明确授权创建公开 GitHub 仓库并推送完整 `main`，该操作已经完成；仍未授权合并、创建 GitHub Release 或历史重写。
- 测试版原型采用 Windows 64 位 Release ZIP 交付，构建产物继续保持 Git 忽略；包内保留中文测试说明，明确灰盒、配方和数值均非最终内容。
- 远程仓库按用户明确要求设为公开；当前以 `main` 作为默认分支和直接同步分支，未额外创建发布分支或 GitHub Release。

### P1｜技术结构

- `OpeningMenuController` 负责输入模式、实时文字、选择器和页面请求；拆分纹理只负责视觉。
- `ToolInventoryService` 是工具集合、双手、砧板槽、工具位置与内容转移的权威 owner；`ProcessExecutionService` 负责工序选择、规则调用、输出/废品与补救；`DrinkAssemblyState` 负责当前杯、液体、评价与当日制作指标；`DrinkWorkstation` 只保留水壶、量酒器端位、洗手、反馈、signal 和跨服务编排。
- `GrayboxLevelBuilder` 负责参数化吧台/工具/柜体布局；`CabinetInteractable` 负责门扇、抽屉、碰撞和单开互锁。
- 正式 GLB 仍须以稳定 ID 和玩法包装场景接入，不直接修改导入节点；当前灰盒只在实际资产通过运行与截图复核后替换。

## 三、未完成的待办

### P1｜需要用户实玩验收

1. 解压 `builds/windows/GlassesBar-Prototype-Test-20260723.zip`，运行 `GlassesBar.exe` 至少完整游玩两天，重点检查：鼠标/键盘菜单切换、`1.42 m` 吧台与 `2.00 m` 眼高、侧面手册、水槽净空、上方吊柜、长抽屉通行、冰桶取用、每日洗手、三种量酒器换端、空水壶诊断、重复萃取/过滤补救。
2. 记录 `2.31 m` 操作通道、`0.62 m` 抽屉行程、吊柜仰视/交互角度、量酒器操作次数和 `4%` 未洗手惩罚在实际手感上是否合适。
3. 继续复核所有灰盒模型与交互判定框；本轮已修正通用形状和新柜体，但实际游玩仍可能发现个别需要针对性调整的对象。

### P1｜外部内容阻塞

- 等待策划批准正式冰美式配方、容量、概率/恢复曲线、评价、奖励与成本。
- 等待正式酒类清单、操作手册、顾客内容和首批 GLB/纹理；当前菜单“继续游戏”因存档未设计而禁用。

### P2｜后续功能

- 柜体后续仍需要正式储物/补货规则和美术资产；当前除冰桶外主要是可交互灰盒储物结构。
- 多顾客、随机订单、经营经济、升级、存档、莫吉托和真实流体物理仍属于后续里程碑。

## 四、新对话接手入口

- 首读摘要：`docs/CONTEXT_HANDOFF.md`
- 当前状态：`docs/PROJECT_STATUS.md`
- 技术设计：`docs/TECHNICAL_DESIGN.md`
- 路线图/变更：`docs/ROADMAP.md`、`docs/CHANGELOG.md`
- 主场景：`scenes/Main.tscn`
- 菜单：`scripts/ui/OpeningMenuController.cs`
- 工作台/库存/工序：`scripts/gameplay/DrinkWorkstation.cs`、`src/Domain/ToolInventoryService.cs`、`src/Domain/ToolProcessModel.cs`
- 场景/柜体：`scripts/world/GrayboxLevelBuilder.cs`、`scripts/gameplay/CabinetInteractable.cs`
- 开发数据：`data/gameplay/prototype_gameplay_catalog.tres`
- 一键验证：`tools/run_verification.ps1`

建议新对话首句：

> 请先阅读 `progress.md` 和 `docs/CONTEXT_HANDOFF.md`，使用项目 Skill `develop-glasses-bar-godot` 接手。先实玩新版 Release 两天，重点检查拆分主菜单、`1.42 m` 前台/`2.00 m` 眼高、`2.31 m` 通道、`0.62 m` 长抽屉、前台水槽净空、酒架上方吊柜、侧围挡手册、冰桶抽屉和量酒器流程，再按实际手感继续迭代。

## 2026-08-02｜阶段 2 手持资产检查点 2 视觉返修

### 已完成的事项

- 已根据审核反馈重做阶段 2 金属表现：小／大量酒器改为可读冷银主体与明亮杯沿／腰环；豆铲和冰夹统一为深色粗糙缎面旧钢。
- 豆铲手持缩放已缩小，并以斜视角稳定呈现宽口、浅腹、低前缘和连接握柄。
- 冰夹夹头已改为更大的闭合双面凹形小勺，握点移到双臂中段；最终手持画面可同时读出两片细长臂、张开状态、夹头和 U 形弹簧连接。
- 已用非 headless Godot 4.7.1 Forward+ 在 RTX 5070 Laptop 上重新捕获 156 帧；关键帧位于 `artifacts/visual_review_20260802_stage2_godot/`。

### 关键决策

- 只修改可复现生成器几何／PBR 和 `ToolVisualLibrary.ApplyHeldPose()`，不改变稳定 ID、玩法状态、碰撞、现实可交互／眼镜观察语义。
- 公共眼镜标签在密集量酒器处的文字重叠是非阻塞原型问题，本轮不扩展修复范围。
- 五项阶段 2 资产继续保持 `placeholder=true`；通过本轮视觉返修不等于授权进入 Task 7。

### 未完成的待办

- 等待用户复核最终关键帧。
- 仅在用户明确批准后执行 Task 7：取消五项阶段 2 占位标记、更新正式状态并做最终收尾验证。

## 2026-08-02 — Asset-modeling Skill Task 1 RED baseline

### 已完成的事项

- 新增三份可复用的资产批次压力场景和两份隔离的 fresh-context 基线记录；原始转录仅保存在被忽略的 `.superpowers/sdd/.../scratch/`。
- 按用户要求缩减测试预算：完成轮廓检查点与 Forward+ 材质快捷场景；完成压力场景仅保留夹具，未派发。
- 已确认 `.agents/skills/modeling-glasses-bar-assets` 不存在。

### 关键决策

- 两个已执行场景均未跳过必要门槛，因此不虚构 RED 纪律失败；现有证据仅将后续工作定位为技术/框架辅助，不能声称完成压力场景已验证。
- 未创建目标 Skill 生产文件，也未进入 Stage 3。

### 未完成的待办

- 后续任务在用户许可的范围内继续 Skill 的设计、实现与验证；如需更完整的基线覆盖，可另行执行 `completion-pressure` 场景。

## 2026-08-02 — Asset-modeling Skill static implementation

### 已完成的事项

- 已静态完成仓库本地 Skill `.agents/skills/modeling-glasses-bar-assets/`：精简 `SKILL.md`、匹配的 `agents/openai.yaml`、两个标准库脚本、三个按阶段路由的 reference，以及七个设计指定模板。
- 框架明确生成合同、Blender 生成／审图、合同检查、Godot 行为集成／Forward+ 捕获和批次记录骨架；不注入未经批准的造型、材质、容量或玩法值。
- 已删除被用户禁止保留的 `tests/tools/test_modeling_skill_framework.py` 与目录占位 `.gitkeep`；保留既有 RED 场景及基线记录，未新增任何运行。
- 已更新 Skill 测试状态文档，并写入 `.superpowers/sdd/2026-08-02-modeling-glasses-bar-assets-skill/static-implementation-report.md`。

### 关键决策

- 用户明确覆盖原计划的测试与验证步骤；本轮未运行单元测试、`py_compile`、`quick_validate`、代理压力／前向测试、Blender、Godot 或完整项目验证，因此交付状态是“已实现但未测试／未验证”。
- Skill 强制依赖 `develop-glasses-bar-godot`，保留两次用户硬检查点、ignored artifacts／正式路径／清单门禁、真实 Forward+ 图片人工审查、稳定 ID、手写包装、项目玩法状态所有权和灰盒回退。
- Skill 与脚本不自动批准、推送、合并、发布、重写历史或启动下一资产阶段。
- 未创建或修改任何 Stage 3 配置、合同、模型、包装、清单、截图或资产状态。

### 未完成的待办

- 仅在用户另行批准后，补做脚本单元测试、语法检查、Skill 静态校验与 fresh-context 前向场景，并根据真实失败做最小修正。
- 任何新资产批次仍需新的范围／设计与显式用户授权；Stage 3 继续暂停。

## 2026-08-11 — 正式环境 Task 8–10 打回重建与预集成审查

### 已完成的事项

- 按用户最新反馈重建六个环境模块：吧台结构基座／柜体／工作台为连续 C 形单网格，仅在东侧 `0.90 m` 员工门处分割；45° 倒角仅位于工作区内边缘。
- 水槽下保留无抽屉的深棕基座并隐藏管线；槽体改为实际空心，槽沿改为水平 XZ 几何，龙头为东侧立管与向槽内下倾短嘴。自动射线从槽中心向下首先命中槽底。
- 西侧手册架按最终反馈从上一候选顺时针旋转 `90°`，并由 `0.80 × 0.32 m` 收紧为 `0.62 × 0.28 m`，长边贴合侧台纵深；后吧上下柜体为空心框，推拉活动门后移，五湾两层瓶架规整无重叠。删除全部墨绿色柜材并统一为 `deep_brown_cabinet`。
- 十二把客座椅全部朝向所属桌心；新增三盏客桌吊灯与实际向下 SpotLight3D。六盏吊灯及四盏壁灯罩统一使用 `frosted_translucent_shade` 半透明磨砂材质。
- 新增同模型 neutral/reality/glasses 表现切换回归；Godot 4.7.1 Vulkan Forward+ 在 RTX 5070 上生成并人工检查 27 张 `1920×1080` 图，路径为 `artifacts/bar-interior-formal-preintegration/`。旧开启状态截图文件名已清理。
- 修复正式批次生成器只导出建筑模块的问题；六模块正式 GLB/sidecar 与 ignored 候选均已刷新，批次配置和资产合同同步为当前设计。
- 验证通过：`BAR_MODEL_CONTRACT_PASS`；六份 sidecar 各 `1/0`；主清单 `21/0`；批次 `formal-candidate errors=0`；领域 `28/28`；Debug/Release `0` 警告／`0` 错误；布局、储物、运行时几何、表现模式、冒烟、Stage 1/2、输入和流程全部 PASS。

### 关键决策

- 当前证据属于 Task 10 预集成几何／材质审查，不是建模 Skill 检查点 2。Task 11 真实生产加载、共享玩法状态、双世界与全灰盒回退完成前，不运行或宣称最终 `forward-plus-review`。
- 六项环境主清单继续 `placeholder: true`，当前游戏继续使用完整灰盒；视觉 GLB 不接管碰撞、灯光、稳定 ID 或玩法状态。
- 西侧只保留顺时针旋转并缩小后的手册架；东侧只保留无抽屉水槽基座、弃物区和员工门。旧开放管线、竖直槽框、横栏式龙头、东侧下柜、墨绿材质和拼接工作台不得恢复。
- 用户现有 `export_presets.cfg` 修改保留且不纳入本轮提交；不推送、不发布。

### 未完成的待办

- 等待用户检查本轮 Task 10 样式与 27 张 Forward+ 证据。
- 用户批准后执行计划 Task 11–13：生产视觉加载器、可动件视觉绑定、现实／眼镜材质与灯光切换、任一模块失败时全灰盒回退，以及生产／眼镜／回退三组最终 Forward+ QA。
- 只有 Task 11 行为集成和用户检查点 2 都通过后，才可将六项环境 placeholder 切换为正式状态。

## 2026-08-11 — 中断恢复后的交付前审计

### 已完成的事项

- 接管 `codex/integrate-modeling-workflow` 工作树并核对 `git status`、未暂存差异、未跟踪文件与现有 HEAD `3028cb3`；确认暂存区原先为空。
- 对本轮交付范围执行静态审计：`git diff --check` 无空白错误；六项环境主清单仍为占位，批次仍停在 Task 10 / `formal-candidate`，未误记为检查点 2 或 Task 11 生产集成。
- 遵照用户要求复用前序验证证据，未重新运行建模、生成、导出、截图或已通过的资产／项目验证。

### 关键决策

- `export_presets.cfg` 是用户本地改动，保留在工作树且明确排除在交付提交之外。
- 本次只提交已完成的 Task 8–10 候选返修、审查支持、合同／记录／交接文档；不推送、不发布、不进入 Task 11。

### 未完成的待办

- 等待用户审查 Task 10 样式；获批后才按现有计划进入 Task 11–13。
- 六项环境资产继续保持 `placeholder: true`，直到行为集成与用户检查点 2 均通过。

## 2026-08-11 — GitHub 发布准备与开发线整合

### 已完成的事项

- 审计 Git worktree、分支与远端：仓库实际只登记一个物理工作树；`main` 是 `codex/integrate-modeling-workflow` 的祖先。
- 将 `main` fast-forward 到 `2e823ce`，两条本地分支现在指向同一提交，差异为 `0/0`，未产生冲突或重写历史。
- 确认 `huixun431-sketch/glasses-bar` 已是 PUBLIC，GitHub CLI 已登录；当前跟踪树的常见敏感文件名／密钥模式扫描无命中。
- 合并后完整运行 `tools/run_verification.ps1`，退出码 `0`：资产 `21/0`、领域测试 `28/28`、Debug/Release 构建与布局、储物、运行时几何、正式表现模式、冒烟、Stage 1/2、输入、流程场景全部通过。

### 关键决策

- 采用可证明无分叉的 fast-forward 整合，不制造多余合并提交。
- `export_presets.cfg` 保持未提交并排除 GitHub 同步；不使用 force push。
- 仓库尚缺 README 与 LICENSE。README 可按当前可验证状态补充；LICENSE 涉及权利授予，等待用户选择，不擅自决定。

### 未完成的待办

- 推送本地 `main` 到公开 GitHub 远端，并核对远端提交。
- 根据用户许可证选择补充 LICENSE，并完善 README 的构建、验证、项目状态与贡献边界。
- 按既有批准规格和 Task 11–13 计划继续生产视觉加载、可动件绑定、双世界表现与全灰盒回退；检查点 2 和 placeholder 切换仍需满足既有门禁。
