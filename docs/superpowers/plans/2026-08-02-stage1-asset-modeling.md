# 第一轮核心交互资产建模实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `highball_glass`、`jigger_medium`、`mortar`、`pestle` 制作可复现的低多边形 GLB 技术样件，在 Godot 中通过手写包装场景接入，并完成技术、交互和实际 Forward+ 画面复核。

**Architecture:** Blender 4.5.5 LTS 后台运行仓库内的确定性 Python 生成器，输出米制、`+Y` 向上、`-Z` 向前、应用变换且内嵌简单 PBR 材质的 GLB。Godot 只实例化 `scenes/assets/stage1/*.tscn` 手写包装场景；`ToolInteractable` 和 `HeldToolPresenter` 保持玩法状态的单向消费者，加载失败时回退现有灰盒，不改变配方、容量、动作许可或领域状态。

**Tech Stack:** Blender 4.5.5 LTS Python API、glTF 2.0/GLB、Godot 4.7.1 .NET/C#、NUnit/.NET 8、PowerShell 验证脚本。

## Global Constraints

- 稳定 ID 必须保持 `highball_glass`、`jigger_medium`、`mortar`、`pestle`。
- GLB 使用米制、`+Y` 向上、`-Z` 向前；节点缩放为 `[1, 1, 1]`。
- 锚点名称大小写精确匹配计划表：`Grip`、`Placement`、`FillOrigin`、`Spout`、`Interaction`。
- 导入的 GLB 只能位于手写玩法包装场景之下；不得在导入节点上绑定玩法脚本。
- 现实世界使用暖色复古候选；眼镜世界只做材质覆盖，不复制或拥有玩法状态。
- 灰盒回退保留到资产验证、Godot 导入、交互测试和实际截图复核全部通过。
- 不改变正式配方、平衡值、顾客内容、玩法状态归属或未批准的最终美术方案。
- 不纳入用户已有的 `export_presets.cfg` 改动，不提交 `.blend` 源文件和 `artifacts/` 截图。

---

### Task 1: 锁定尺寸卡、生成器合同与资产验证

**Files:**
- Create: `docs/assets/STAGE1_ASSET_BATCH_20260802.md`
- Modify: `tools/validate_assets.py`
- Test: `tools/validate_assets.py`

**Interfaces:**
- Consumes: `GameplaySceneComposer.ToolMesh`、`HeldToolPresenter` 的现有灰盒包络和 `assets/asset_manifest.json` 锚点清单。
- Produces: 四项资产的尺寸、接触面、原点、朝向、锚点位置和碰撞预期；验证器对根节点名、锚点、应用变换、网格和材质进行检查。

- [ ] **Step 1: 添加失败的验证器自测**

  在 `self_test()` 中生成根节点名与资产 ID 不一致、锚点有非单位缩放的 GLB，断言验证结果非零；同时保留现有好/坏样本。

- [ ] **Step 2: 运行自测确认失败**

  Run: `python tools/validate_assets.py --self-test`
  Expected: FAIL，因为验证器尚未检查根节点名和锚点变换。

- [ ] **Step 3: 实现最小验证规则并记录尺寸卡**

  根节点必须命名为资产 ID；所有节点不得包含非单位 `scale`；必需锚点必须存在。尺寸卡采用现有灰盒外包络：高球杯 `0.15 × 0.25 × 0.15 m`、中号量酒器 `0.13 × 0.18 × 0.13 m`、研钵 `0.48 × 0.24 × 0.48 m`、研杵 `0.15 × 0.42 × 0.15 m`，并在批次记录中列出原点、接触面与锚点坐标。

- [ ] **Step 4: 运行验证器自测确认通过**

  Run: `python tools/validate_assets.py --self-test`
  Expected: `SELFTEST ... PASS` 且退出码 0。

### Task 2: 建立 Blender 生成器并输出四项 GLB

**Files:**
- Create: `tools/modeling/generate_stage1_assets.py`
- Create: `assets/models/highball_glass.glb`
- Create: `assets/models/jigger_medium.glb`
- Create: `assets/models/mortar.glb`
- Create: `assets/models/pestle.glb`
- Modify: `assets/asset_manifest.json`

**Interfaces:**
- Consumes: Task 1 尺寸卡和锚点合同。
- Produces: `build_highball_glass()`、`build_jigger_medium()`、`build_mortar()`、`build_pestle()`，每个函数清空场景、创建一个以资产 ID 命名的根节点、模型网格、PBR 材质和所需锚点，再导出单个 GLB。

- [ ] **Step 1: 先运行未存在的生成器确认失败**

  Run: `& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage1_assets.py -- --output assets/models`
  Expected: FAIL，脚本尚不存在。

- [ ] **Step 2: 实现确定性低多边形生成器**

  高球杯使用 16 边外壁、内壁、加厚杯底与独立口沿，整体高 `0.25 m`；中号量酒器使用上下两只 16 边截锥和窄腰，整体高 `0.18 m`；研钵使用 16 边外壳/内腔、厚口沿和底座，整体高 `0.24 m`；研杵使用低边数旋转轮廓，整体高 `0.42 m`。玻璃使用半透明浅青材质，量酒器使用暗银金属候选，研钵/研杵使用深棕复合材质并以少量边缘色带表达使用痕迹。

- [ ] **Step 3: 后台生成 GLB**

  Run: `& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage1_assets.py -- --output assets/models`
  Expected: 四个 `WROTE ...glb`，Blender 退出码 0。

- [ ] **Step 4: 让清单接受四项技术候选**

  仅在 GLB 通过验证后将四项的 `placeholder` 改为 `false`；其余 12 项保持 `true`。

- [ ] **Step 5: 验证资产结构**

  Run: `python tools/validate_assets.py assets/asset_manifest.json --allow-placeholders`
  Expected: 四项显示 `OK`，其余 12 项显示 `INFO ... graybox placeholder`，`SUMMARY assets=16 errors=0`。

### Task 3: 建立手写包装场景和回退加载器

**Files:**
- Create: `scenes/assets/stage1/highball_glass.tscn`
- Create: `scenes/assets/stage1/jigger_medium.tscn`
- Create: `scenes/assets/stage1/mortar.tscn`
- Create: `scenes/assets/stage1/pestle.tscn`
- Create: `scripts/assets/ToolVisualLibrary.cs`
- Create: `scripts/assets/ToolVisualLibrary.cs.uid`（由 Godot 导入生成）
- Modify: `scripts/gameplay/ToolInteractable.cs`
- Modify: `scripts/world/GameplaySceneComposer.cs`

**Interfaces:**
- Consumes: `ToolVisualLibrary.Instantiate(string toolId) -> Node3D?`。
- Produces: 四个包装根节点的 `asset_id` metadata；世界摆放时优先显示包装场景，资源缺失或实例化失败时继续显示现有程序化灰盒。

- [ ] **Step 1: 增加资产接入测试并确认失败**

  在 Task 4 的测试场景中断言四个 `ToolInteractable` 均有名为 `AssetVisual` 的包装子节点和匹配的 `asset_id` metadata。

- [ ] **Step 2: 创建包装场景**

  每个 `.tscn` 以 `Node3D` 为根，`asset_id` metadata 等于稳定 ID，唯一视觉子节点实例化对应 `res://assets/models/<id>.glb`；包装根不附加玩法脚本和碰撞。

- [ ] **Step 3: 实现加载器与世界摆放接入**

  `ToolVisualLibrary` 只维护四项稳定 ID 到包装场景路径的只读映射。`ToolInteractable.Configure` 接受候选包装实例，成功时隐藏程序化 `MeshInstance3D` 并挂载 `AssetVisual`；失败时保留原灰盒和现有碰撞。

- [ ] **Step 4: Godot 导入**

  Run: `& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --editor --quit`
  Expected: 退出码 0，四项 GLB、包装场景与 C# 脚本无导入错误。

### Task 4: 接入第一人称手持表现并添加集成测试

**Files:**
- Modify: `scripts/player/HeldToolPresenter.cs`
- Modify: `scripts/player/PlayerController.cs`
- Modify: `tests/godot/InputIntegrationTests.cs`
- Create: `tests/godot/Stage1AssetIntegrationTests.cs`
- Create: `tests/godot/Stage1AssetIntegrationTests.tscn`
- Modify: `tools/run_verification.ps1`

**Interfaces:**
- Consumes: `ToolVisualLibrary.Instantiate` 和左右 `Node3D` 手部锚点。
- Produces: 手持资产实例节点名 `HeldAssetVisual`；未覆盖的工具仍使用原 `HeldTool` 灰盒，重置时两种表现都隐藏。

- [ ] **Step 1: 写失败的集成断言**

  测试四项世界节点都加载包装场景；拿起研钵/研杵后左右手分别出现匹配 `asset_id` 的 `HeldAssetVisual`；重置后两者隐藏；现实/眼镜切换不改变工具权威 ID 或节点数量。

- [ ] **Step 2: 运行测试确认失败**

  Run: `dotnet build GlassesBar.csproj --configuration Debug --nologo` 后运行 `Godot ... res://tests/godot/Stage1AssetIntegrationTests.tscn`
  Expected: FAIL，因为手持包装实例尚未接入。

- [ ] **Step 3: 实现手持包装实例与灰盒回退**

  `HeldToolPresenter` 接收左右手锚点和既有灰盒节点；工具变化时释放旧 `HeldAssetVisual`，为四项加载新包装实例，按 `Grip` 锚点的逆变换校正到手部原点；其他工具仍更新并显示原灰盒 Mesh。

- [ ] **Step 4: 运行集成测试确认通过**

  Run: `Godot ... --headless --path . --quit-after 300 res://tests/godot/Stage1AssetIntegrationTests.tscn`
  Expected: `STAGE1_ASSET_INTEGRATION_PASS`，退出码 0。

### Task 5: 两轮 Godot 校准和视觉复核

**Files:**
- Create: `tests/godot/Stage1AssetVisualCapture.cs`
- Create: `tests/godot/Stage1AssetVisualCapture.tscn`
- Modify: `docs/assets/STAGE1_ASSET_BATCH_20260802.md`

**Interfaces:**
- Consumes: 四项世界/手持候选、`Grip`/`Placement`/`FillOrigin`/`Spout`/`Interaction` 锚点。
- Produces: `artifacts/visual_review_20260802_stage1/` 中的实际 Forward+ PNG，以及尺寸、遮挡、轮廓、材质、双世界和组合净空检查结论。

- [ ] **Step 1: 首轮世界摆放截图**

  使用 Forward+ 非 headless 模式运行专用截图场景，将相机对准吧台上的四项资产，分别捕获现实和眼镜世界。检查杯底/研钵底/量酒器底是否落台，四件远距轮廓是否互相区分。

- [ ] **Step 2: 首轮返修**

  只调整生成器中的轮廓、材质参数和包装校正；不修改领域数据、玩法包络或站点逻辑。重新生成、导入并运行资产验证。

- [ ] **Step 3: 第二轮手持与组合截图**

  捕获高球杯/研钵左手、量酒器/研杵右手，以及研钵+研杵组合。检查第一人称遮挡、握持轴向、研杵接触端与研钵内腔净空、量酒器 `Spout` 朝向。

- [ ] **Step 4: 第二轮返修并记录结论**

  重复生成、导入和截图，记录每项技术、美术候选、交互和灰盒回退状态；最终美术仍标记为 M1 技术候选而非正式终稿。

### Task 6: 全量验证、状态文档与提交

**Files:**
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `progress.md`

**Interfaces:**
- Consumes: 前五项任务的验证输出和视觉证据。
- Produces: 明确区分“已验证技术候选”“等待用户主观美术/手感复核”“后续阶段 2 未开始”的项目状态。

- [ ] **Step 1: 运行全量验证**

  Run: `powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1`
  Expected: 资产 16 项 0 错误、Debug/Release 0 错误、领域测试全部通过、Godot 导入/冒烟/资产/输入/流程测试全部 PASS。

- [ ] **Step 2: 检查工作树范围**

  Run: `git status --short` 和 `git diff --check`
  Expected: 只包含阶段 1 资产、接入、测试和文档；`export_presets.cfg` 仍为用户改动且不暂存。

- [ ] **Step 3: 更新四份状态文档**

  写入资产状态、关键决策、验证命令、截图路径、仍保留的限制和下一动作；`progress.md` 必须包含已完成事项、关键决策、未完成待办。

- [ ] **Step 4: 提交本轮工作**

  Run: `git add` 显式列出本轮文件，确认不含 `export_presets.cfg` 后提交。
  Commit message: `feat: model first core interaction asset batch`

