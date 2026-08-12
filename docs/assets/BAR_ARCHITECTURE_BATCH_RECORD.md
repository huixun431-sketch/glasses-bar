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

## 2026-08-12 Task 11 production integration and lighting candidate

- 六模块已在默认 `Main` 中事务式加载；建筑／家具／灯具外壳按现实与眼镜世界各一份，吧台／背柜／磨损保持共享表现，柜门和抽屉视觉绑定到权威玩法节点。任一模块缺失会原子式回退完整灰盒。
- 项目继续拥有碰撞、稳定 ID、开合状态和真实灯光；每个世界为 14 盏逻辑灯。灯光候选降低直下灯峰值，并用两项既有不可见填光照亮深棕立面，未添加额外灯具或修改 GLB。
- Capture scene：`tests/godot/BarProductionVisualCapture.tscn`；Renderer：Godot `4.7.1.stable.mono` Vulkan Forward+／RTX 5070 Laptop GPU；现行证据为 `artifacts/bar-interior-production-integration/` 下 24 张 `1920×1080` PNG。
- 24 张全部已打开检查；`14_reality_lighting`／`15_glasses_lighting` 与 `23_reality_player_eye_lighting`／`24_glasses_player_eye_lighting` 不使用捕获专用技术补光。当前候选的吧台、背柜、地板和客区可读，暖／冷世界差异明确，未见高光白片、死黑立面、柜门行程或动线回归。
- 自动化：主清单 `21/0`、领域 `28/28`、Debug/Release `0/0`、`BAR_PRODUCTION_ASSET_INTEGRATION_PASS` 及完整 Godot 回归通过；批次 `forward-plus-review errors=0`。
- 用户检查点 2 仍为 pending，六项环境主清单保持 `placeholder: true`；本节记录真实证据，不代替用户批准。

## Checkpoint 2: actual integrated Godot Forward+

- Status：approved by user on 2026-08-12。
- 批准记录：用户在检查重新配平后的现实／眼镜 Forward+ 宽景后明确回复“批准”。
- 证据：Task 11 正式包装接入、共享玩法状态、强制缺模块全灰盒回退、24 张实际生产 Forward+ 图以及完整项目验证均已归档。

## Completion

### Completed items

- 完成检查点 1、六模块正式 GLB／sidecar／手写包装、Task 8–10 返修、Task 11 生产接入与检查点 2 批准。
- 默认游戏使用完整正式酒吧视觉；事务式加载器在任一模块失败时仍整体回退到可玩的完整灰盒。
- 六项环境资产已在 JSON 与 Godot Resource 清单中统一切换为 `placeholder: false`；其他未制作资产状态不变。

### Key decisions

- GLB 只负责视觉；碰撞、灯光、稳定 ID、柜门状态与玩法状态继续由 Godot 权威层拥有。
- 现实／眼镜灯光以用户批准的暖／冷候选为准；捕获专用技术补光不进入游戏运行时。
- 正式酒吧完成不扩展正式配方、平衡、顾客内容、手册动画或后台区域。

### Remaining TODOs

- 手册拿取／阅读动画、顾客 AI 与后台区域继续留给后续批准的里程碑。
- `ice_bucket`、`bar_sink`、`cutting_board`、`water_kettle`、`coffee_beans`、`waste_bin` 仍是明确的灰盒资产，不属于本批完成范围。
- 公开仓库 README 仍待后续补充；许可证已按用户决定设置为 MIT。

## Verification summary

- Blender geometry/material contract：`BAR_MODEL_CONTRACT_PASS`。
- Godot Forward+ capture：`BAR_PRODUCTION_VISUAL_CAPTURE_PASS`，24 张 `1920×1080`，全部已人工检查。
- 六份 sidecar 各 `assets=1 errors=0`；主清单 `assets=21 errors=0`，本批六项环境均为正式资产。
- 项目一键回归：领域 `28/28`；Debug/Release `0` 警告／`0` 错误；生产／回退、布局、储物、几何、冒烟、Stage 1/2、输入和流程全部 PASS。
- Status: PASS
