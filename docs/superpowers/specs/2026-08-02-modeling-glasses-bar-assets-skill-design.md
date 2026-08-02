# 眼镜酒馆资产批次建模 Skill 设计

## 目标

在仓库 `.agents/skills/` 中新增 `modeling-glasses-bar-assets`，把阶段 1／2 已验证的 Blender→GLB→Godot 批次工作流固化为可重复执行的项目 Skill 与完整脚手架。Skill 只服务本仓库资产批次；阶段 3 本轮保持暂停。

## 核心边界

- 新 Skill 必须声明依赖 `develop-glasses-bar-godot`，不重复玩法、双世界或资产交接总约束。
- 自动化只生成合同、生成器、审核、测试、捕获和批次记录骨架；不得自行决定正式造型、材质、容量、配方、平衡或顾客内容。
- GLB 始终是手写包装下的视觉子场景；稳定 ID、玩法碰撞、状态、左右手与眼镜世界只观察语义仍由项目实现持有。
- 具体美术值由生成后 GLB 校验与真实截图审核，不用逐项常量断言替代视觉判断。
- Skill 不自动推送、合并、发布或启动下一资产阶段。

## 文件结构

```text
.agents/skills/modeling-glasses-bar-assets/
├── SKILL.md
├── agents/openai.yaml
├── scripts/
│   ├── init_asset_batch.py
│   └── validate_asset_batch.py
├── references/
│   ├── workflow.md
│   ├── framework-contract.md
│   └── review-checkpoints.md
└── assets/templates/
    ├── asset_contract.py.tmpl
    ├── blender_generator.py.tmpl
    ├── blender_review_renderer.py.tmpl
    ├── contract_test.py.tmpl
    ├── godot_integration_test.cs.tmpl
    ├── godot_visual_capture.cs.tmpl
    └── asset_batch_record.md.tmpl
```

`SKILL.md` 保持精简，只负责触发条件、必读资料、状态机和资源路由。详细接口、检查点和模板说明按需读取 references；可执行逻辑留在 scripts。

## 批次配置与生成接口

初始化脚本接收一个 JSON 配置，至少包含：

- `batch_id`：小写字母、数字、连字符组成的稳定批次 ID；
- `stage`：项目阶段编号或描述；
- `assets`：每项的 `asset_id`、`runtime_id`、`required_anchors`、`interaction_kind`；
- `paths`：候选输出、正式模型、Godot 包装、批次记录路径；
- `checkpoints`：两个检查点初始均为 `pending`。

脚本根据配置生成项目文件骨架，不向模板注入未批准包络、材质或姿态值。生成器统一支持：

```text
--mode silhouette --output <ignored-candidate-root>
--mode final --output <formal-model-root>
```

每个资产构建函数以稳定 `asset_id` 注册，并按合同生成锚点。审核渲染器统一生成正面、三分之四和按批次需要声明的组合／尺度视图。Godot 模板只实例化手写包装、行为集成测试和确定性 Forward+ 捕获入口。

## 状态机与门禁

```text
范围与设计
→ 行为合同 RED/GREEN
→ 中性轮廓候选
→ Blender 检查点 1
→ 用户批准
→ 正式材质／GLB／手写包装
→ Godot 行为集成
→ Forward+ 检查点 2
→ 用户批准
→ 清单切换／全量验证／归档
```

- 检查点 1 前，候选 GLB、临时清单和截图只能位于被忽略的产物目录。
- 检查点 1 未批准时，校验器拒绝正式模型、包装或正式清单状态切换。
- 检查点 1 通过后允许生成正式 GLB／包装，但正式清单仍须保持灰盒。
- 检查点 2 必须引用实际 Godot Forward+ 图片；仅有参数、headless 测试或 Blender 图不能批准。
- 检查点 2 未批准时，校验器拒绝把该批资产改为非占位。
- 视觉返修必须循环执行生成、验证、导入、截图与复核；报告文本不能代替实际产物。
- 最终归档必须更新批次记录、项目状态、路线图、变更、交接和根 `progress.md`，并运行项目全量验证。

## 校验器职责

`validate_asset_batch.py` 读取批次配置、正式资产清单和 Git 状态，输出稳定的错误列表与非零退出码。它检查：

1. 批次 ID、资产 ID、运行时 ID 和锚点完整性；
2. 所需骨架文件和批次记录是否存在；
3. 当前阶段是否允许候选／正式文件和清单状态；
4. 正式清单只切换批次内资产，未批准资产仍为灰盒；
5. `artifacts/`、截图、`.blend` 和手改导入产物未被跟踪；
6. 检查点批准记录是否带有实际证据路径；
7. 完成状态是否包含验证摘要、已完成事项、关键决策和未完成待办。

脚本不替代现有 `tools/validate_assets.py`、Godot 行为测试或 `tools/run_verification.ps1`，而是编排并补充阶段门禁。

## Skill 测试策略

Skill 按 RED／GREEN／REFACTOR 验证：

1. 在没有新 Skill 的新鲜代理上下文中运行基线场景，记录是否会跳过轮廓批准、用常量测试代替视觉、提前切换清单或遗漏归档。
2. 编写最小 Skill 和脚本以修正已观察到的失败，不为假设问题堆叠规则。
3. 运行脚本单元测试：合法批次、未批准正式路径、提前清单切换、被跟踪截图、缺失证据和完整归档。
4. 使用同类但不同资产的场景重新前向测试 Skill，确认代理能停在正确检查点并调用模板／校验器。
5. 运行 Skill `quick_validate.py`、脚本帮助与代表性初始化／校验命令，检查 `agents/openai.yaml` 与 `SKILL.md` 一致。

测试产物写入临时目录，不污染正式资产、清单或阶段状态。

## 错误处理

- 初始化脚本遇到已存在目标文件时默认拒绝覆盖，并列出冲突路径。
- 配置错误必须在写文件前全部报告；不得生成半套骨架。
- 校验器按确定性顺序列出所有错误，便于一次修复；任何错误返回非零状态。
- Blender、Godot 或项目依赖缺失时，报告为未验证阻塞，不得把阶段标记完成。
- 工作树存在无关用户改动时，只报告冲突，不暂存、覆盖或清理用户文件。

## 非目标

- 本轮不启动阶段 3，不创建其资产合同、模型或包装。
- 不重构现有阶段 1／2 生成器到新框架；它们作为已验证参考保留。
- 不建立跨项目通用插件，不安装到个人 Skills 目录。
- 不实现最终美术生成、自动审美评分、纹理烘焙或自动批准检查点。

## 完成标准

- Skill 文件夹结构、元数据、脚本、references 和模板均通过验证；
- 脚本测试覆盖门禁与失败路径，并有实际 RED/GREEN 证据；
- 新鲜代理使用 Skill 时能生成完整批次骨架、停在用户检查点且不提前切换清单；
- 项目现有完整验证继续通过；
- `docs/CONTEXT_HANDOFF.md` 与 `progress.md` 记录 Skill 已可用、阶段 3 仍暂停及下一安全动作。
