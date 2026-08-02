# 吧台灰盒细节修订 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除后吧台门型、客侧尺度、抽屉穿模和水槽空白四项实机问题，并为正式建模提供无歧义合同。

**Architecture:** `BarLayoutDefinition` 继续作为唯一尺度源；布局、视觉、碰撞和测试都从新增的门型、柜架与管线数据生成。推拉门的权威交互根保持静止，只移动子门片；前吧台从实心棱柱改为空心柜架，确保抽屉与静态结构真正分离。

**Tech Stack:** Godot 4.7.1 .NET、C#、PowerShell 验证脚本、Forward+ 截图。

## Global Constraints

- 保留 Z3/H3 16×10×4.5 m 房间、9.10 m 前吧、1.55 m 工作通道及所有稳定玩法 ID。
- 后吧台下柜为推拉门；上柜仍为铰链门。
- 客侧台面只向 +Z 外挑 0.30 m。
- 水槽下方无柜体和玩法碰撞，仅有裸露管线视觉。
- 不修改导入 GLB；`export_presets.cfg` 始终排除提交。

---

### Task 1: 锁定修订合同

**Files:**
- Modify: `tests/godot/BarProductionLayoutContractTests.cs`
- Modify: `tests/godot/BarStorageIntegrationTests.cs`
- Modify: `tests/godot/BarRuntimeGeometryTests.cs`

**Interfaces:**
- Consumes: `BarLayoutDefinition.Prototype` 与当前运行时场景树。
- Produces: `SlidingDoor`、`FrontCarcassParts`、`SinkPlumbingParts` 和真实运动路径的失败断言。

- [x] **Step 1: 写布局合同失败测试**

断言客侧顶面最大 Z 从 `-0.85` 变为 `-0.55`、立板仍为 `-0.85`；五个 `rear_lower_cabinet_*` 为 `SlidingDoor`、上柜为 `Door`；柜架不与抽屉腔体相交；管线均位于水槽净空内。

- [x] **Step 2: 写储物与运行时失败测试**

打开后下柜后断言交互根位置不变且 `MovingLeaf` 沿本地 X 移动；采样所有前抽屉并与 `FrontStaticCarcass` 实际网格边界比较；断言 `ExposedSinkPlumbing` 存在且水槽下无存储节点。

- [x] **Step 3: 运行三个测试并确认 RED**

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe' --headless --path . --scene tests/godot/BarProductionLayoutContractTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe' --headless --path . --scene tests/godot/BarStorageIntegrationTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe' --headless --path . --scene tests/godot/BarRuntimeGeometryTests.tscn
```

预期：新增合同因缺少枚举、数据或运行时节点失败，且失败原因与四项反馈一致。

### Task 2: 实现权威布局和推拉门

**Files:**
- Modify: `scripts/world/BarLayoutDefinition.cs`
- Modify: `scripts/gameplay/CabinetInteractable.cs`
- Modify: `scripts/world/CabinetBuilder.cs`

**Interfaces:**
- Consumes: Task 1 的失败断言。
- Produces: `CabinetPartKind.SlidingDoor`、名为 `FixedLeaf`/`MovingLeaf` 的门片、`FrontCarcassParts` 与 `SinkPlumbingParts`。

- [x] **Step 1: 实现布局数据**

新增 `GuestCounterOutwardExtension = 0.30f`；只扩展客侧顶面；将前体降为薄底座并生成避开四组抽屉的柜架；生成四段裸露管线；把五个后下柜切换为推拉门并把柜内物品移到打开侧。

- [x] **Step 2: 实现推拉门运动**

`Configure` 为 `SlidingDoor` 创建双门片，保留交互根和宿主不动；`SetOpen`/Tween 只驱动 `MovingLeaf.Position.X`，关闭时归零偏移。

- [x] **Step 3: 运行布局和储物测试至 GREEN**

运行 Task 1 的前两个场景，预期分别打印 `BAR_PRODUCTION_LAYOUT_CONTRACT_PASS` 与 `BAR_STORAGE_INTEGRATION_PASS`。

### Task 3: 生成空心柜架、碰撞与管线

**Files:**
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `tests/godot/BarRuntimeGeometryTests.cs`

**Interfaces:**
- Consumes: `BarLayoutDefinition.FrontCarcassParts` 与 `SinkPlumbingParts`。
- Produces: 两个表现世界中的 `FrontStaticCarcass`/`ExposedSinkPlumbing` 以及 NeutralGameplay 中匹配的柜架碰撞。

- [x] **Step 1: 生成视觉和静态碰撞**

保留连续薄底座多边形；从权威方盒列表生成柜架视觉和碰撞；管线只生成视觉，不创建碰撞。

- [x] **Step 2: 运行运行时几何测试至 GREEN**

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe' --headless --path . --scene tests/godot/BarRuntimeGeometryTests.tscn
```

预期：打印 `BAR_RUNTIME_GEOMETRY_PASS`。

- [x] **Step 3: 运行完整回归**

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

预期：资产、领域、Debug/Release、Godot 导入及所有集成场景全部通过。

### Task 4: Forward+ 视觉验证与交接

**Files:**
- Modify: `tests/godot/BarLayoutVisualCapture.cs`
- Modify: `docs/assets/BAR_GRAYBOX_Z3_H3_CAPTURE_20260803.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `progress.md`

**Interfaces:**
- Consumes: 修订后的运行时几何。
- Produces: 固定相机截图、哈希与可继续执行的项目交接。

- [x] **Step 1: 添加四个修订特写并运行 Forward+**

捕获客侧 0.30 m 外挑、抽屉全开净空、后下柜推拉开门及水槽裸露管线，保留房间全景与玩家眼高对照。

- [x] **Step 2: 人工逐图检查**

确认无重叠、无过时柜体、无房外溢出、门片与相邻柜净空正确；发现问题则回到 Task 2/3 修复并重新截图。

- [x] **Step 3: 完整回归后更新文档并提交**

文档明确区分已完成、下一步和仍需用户判断的生产建模检查点；只暂存本轮文件，排除 `export_presets.cfg`，提交信息使用 `fix: refine approved bar graybox details`。

### Task 5: 进入既定生产建模下一阶段

**Files:**
- Follow: `docs/superpowers/plans/2026-08-02-bar-layout-production-model.md` Tasks 6–7
- Follow: `.agents/skills/modeling-glasses-bar-assets/references/workflow.md`

**Interfaces:**
- Consumes: 已修订并验证的 Z3/H3 权威合同。
- Produces: 模块化 GLB 交付合同、验证器自测、Blender 中性建筑轮廓证据。

- [ ] **Step 1: 完成 Task 6 模块化 GLB 合同**

按既定计划先写失败自测，再扩展资产清单和验证器，完成完整回归与独立提交。

- [ ] **Step 2: 执行 Task 7 至中性轮廓审阅门槛**

使用确定性 Blender 脚本建立修订后的 16×10×4.5 m 建筑模块，导出并验证 `bar_architecture.glb`，渲染中性材质五视图。

- [ ] **Step 3: 在 Skill 强制审阅点停止**

向用户提交中性建筑轮廓证据；未获批准前不继续正式吧台/后吧台候选和材质抛光。
