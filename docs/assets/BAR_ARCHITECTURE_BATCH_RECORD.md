# Asset batch record: `bar-architecture`

- Stage: `approved checkpoint 1 formal candidate`
- Status: Task 10 PRE-INTEGRATION CANDIDATE READY FOR USER REVIEW；建模 Skill 检查点 2 尚未开始。
- Candidate root: `artifacts/blender_candidates`
- Formal model root: `assets/models`
- Wrapper root: `scenes/environment/modules`
- JSON manifest: `assets/asset_manifest.json`

## Scope and contracts

| Asset ID | Runtime ID | Required anchor | Interaction kind |
|---|---|---|---|
| `bar_architecture` | `bar_architecture` | `Placement` | `environment_visual` |
| `bar_counter` | `bar_counter` | `Placement` | `environment_visual` |
| `bar_backbar` | `bar_backbar` | `rear_lower_cabinet_1_fixed` | `environment_visual` |
| `bar_furniture` | `bar_furniture` | `lounge_table_1` | `environment_visual` |
| `bar_lighting` | `bar_lighting` | `lounge_pendant_1` | `environment_visual` |
| `bar_wear_overlays` | `bar_wear_overlays` | `wear_overlay_root` | `environment_visual` |

当前 Z3/H3 权威建筑尺度为 `16.00 × 10.00 × 4.50 m`。六个模块均已导出至 `assets/models/` 并带独立 sidecar；主清单继续保持 `placeholder: true`，当前游戏仍使用完整灰盒。

## Checkpoint 1: neutral Blender silhouette review

- Status：approved by user。
- 证据：`artifacts/blender_review/architecture/01_north_interior.png` 至 `06_interior_three_quarter.png`。
- 南门矩形异物确认是旧审查 AREA 灯投影；改用世界环境光与无阴影 SUN 后消失，没有删除或隐藏建筑节点。

## 2026-08-11 Task 8–10 rework

- 吧台结构：前吧、西侧连接与后吧的基座、柜体、工作台分别为单一连续 C 形网格；只在东侧 `0.90 m` 员工门处切分。45° 倒角只存在于工作区内侧边缘。
- 功能收敛：西侧只保留手册架；架板相对上一候选顺时针旋转 `90°` 并收紧为 `0.62 × 0.28 m`，长边贴合西侧吧台纵深。东侧保留无抽屉的水槽基座、弃物区和员工门，不恢复已取消的东侧下柜，不显示管线。
- 水槽：水平 XZ 槽沿、真正空心槽体、东侧立管和向槽内下倾的短出水嘴。几何射线从槽中心向下首先命中槽底，旧竖直槽框和横向栏杆式节点被合同拒绝。
- 后吧：五湾两层瓶架规整且无重叠；上下柜体为空心框；下柜活动推拉门后移，闭合时不突出明显边缝。
- 材质：柜体全部为 `deep_brown_cabinet`，合同拒绝任意 `deep_green_cabinet`。灯罩统一标注为 `frosted_translucent_shade`，具半透明、透射和高粗糙度。
- 客座：三桌十二椅逐把朝向所属桌心；三盏客桌吊灯居中，并由 Godot 向下 SpotLight3D 实际照亮台面。加上三盏前吧吊灯，共六盏可见吊灯。

## Task 10 pre-integration Forward+ review

- Capture scene：`tests/godot/BarInteriorFormalVisualCapture.tscn`。
- Renderer：Godot `4.7.1.stable.mono`，Vulkan Forward+，NVIDIA GeForce RTX 5070 Laptop GPU，`1920×1080`。
- 当前证据：`artifacts/bar-interior-formal-preintegration/` 下共 27 张现行 PNG：neutral/reality/glasses 各八个同机位，以及抽屉、五组下推拉柜、十扇上柜三个开启状态。
- 重点视图：
  - `reality/04_west_manual_only.png`：顺时针旋转后的紧凑手册架与连续西侧连接。
  - `reality/05_customer_lounge_and_lights.png`：三桌、朝桌椅子、磨砂吊灯和已照亮台面。
  - `reality/06_overhead_layout.png`：连续 C 形吧台、内侧倒角、东侧门洞与全局通路。
  - `reality/08_sink_bowl_closeup.png`：空心槽底、水平槽沿、下倾短出水嘴与无异物净空。
  - `open-states/09_open_drawer_clearance.png`、`10_all_rear_sliding_doors_open.png`、`11_all_upper_doors_open.png`：可动件不与柜体重叠，柜内为空腔。
- 检查结果：27 张均已在本机打开检查；本轮未发现南门异物、顶棚光斑、墨绿残留、旧水槽横栏或过时开启状态截图。
- 用户决定：pending。本批现停在 Task 10 预集成审查，等待用户检查样式。

## Checkpoint 2: actual integrated Godot Forward+

- Status：pending，未开始。
- 必需证据：Task 11 正式包装接入、共享玩法状态、现实可交互／眼镜只观察、强制缺模块全灰盒回退，以及实际生产现实／眼镜／回退 Forward+ 图。
- 只有上述行为与视觉证据齐全并由用户明确批准后，才可将六个环境项从 `placeholder: true` 切换为正式状态。

## Completion

### Completed items

- 完成检查点 1、六模块正式候选、sidecar、手写预览包装、Task 8–10 最新返修及 27 张 Forward+ 预集成证据。
- 完成连续吧台、空心水槽、深棕空心柜、规整瓶架、推拉门后移、椅子朝桌、客桌吊灯和磨砂透光灯罩的自动合同。
- 保持视觉 GLB 与玩法/碰撞/稳定 ID/双世界状态分离，未替换运行时灰盒。

### Key decisions

- Task 10 预览不是建模 Skill 检查点 2；不得用预览模式替代 Task 11 的实际双世界与回退验证。
- 六项环境主清单保持 `placeholder: true`；旧设计不得通过模型节点、材质名、文档或截图文件名残留。
- 用户批准本轮样式后才执行 Task 11–13；不自行扩展最终配方、平衡、顾客内容或最终美术范围。

### Remaining TODOs

- 请用户检查当前 Task 10 几何／材质预审。
- 获批后实现生产视觉加载器、移动件视觉绑定、现实／眼镜材质和灯光切换，以及任一模块失败时的全灰盒回退。
- 完成生产/眼镜/回退三组 Forward+ QA 与用户检查点 2 批准后，才取消六项 placeholder。

## Verification summary

- Blender geometry/material contract：`BAR_MODEL_CONTRACT_PASS`。
- Godot Forward+ capture：`BAR_INTERIOR_FORMAL_PREINTEGRATION_CAPTURE_PASS`，27 张 `1920×1080`。
- 六份 sidecar：各 `assets=1 errors=0`；主清单：`assets=21 errors=0`，六项环境仍为 placeholder。
- 建模 Skill 批次门禁：`formal-candidate errors=0`。严格 `forward-plus-review` 保持 pending，因为 Task 11 实际集成尚未开始。
- 项目一键回归：领域 `28/28`；Debug/Release `0` 警告／`0` 错误；`BAR_PRODUCTION_LAYOUT_CONTRACT_PASS`、`BAR_STORAGE_INTEGRATION_PASS`、`BAR_RUNTIME_GEOMETRY_PASS`、`BAR_FORMAL_REVIEW_VARIANT_PASS`、冒烟、Stage 1/2、输入和流程测试全部 PASS。
