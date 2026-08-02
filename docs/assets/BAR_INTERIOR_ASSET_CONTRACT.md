# 完整酒吧模块化 GLB 交付合同

更新日期：2026-08-03  
状态：合同已生效；六个环境模块当前仍为 `placeholder: true`。候选 GLB 通过结构验证后仍须完成正式包装、实际 Godot Forward+ 复核和用户检查点 2 批准，才能逐项切换。

## 1. 坐标与导出根

- 单位：米；上轴：`+Y`；前向：`-Z`。
- 场景建筑净尺寸：`16.00 × 10.00 × 4.50 m`。北墙为 `Z=-5.00`，南墙为 `Z=+5.00`。
- 每个 GLB 只能有一个下表指定的场景导出根；根节点的 translation/rotation/scale/matrix 必须为单位变换。
- Blender 中必须应用导出根与网格的缩放；验证器继续拒绝任意节点的非单位 scale，并额外拒绝场景根的非单位 translation/rotation/scale/matrix。
- 稳定节点名区分大小写，不得在导出时自动追加 `.001`。

## 2. 所有权边界

| 模块 | GLB 根 | 视觉所有权 | Godot 所有权 |
|---|---|---|---|
| 建筑 | `bar_architecture` | 房间壳、门窗可见几何、墙裙、地板、装饰线 | 碰撞、门状态、灯光、双世界实例 |
| 前吧 | `bar_counter` | 连续台面、空心柜架、抽屉/水槽/管线/弃物/员工门/手册架可见件 | 抽屉、门、手册和站点玩法；稳定 ID；碰撞；储物状态 |
| 后吧 | `bar_backbar` | 后吧台、五湾两层瓶架、五组推拉下柜和十扇上柜可见件 | 推拉/铰链运动、互锁、柜内宿主、碰撞和每日复位 |
| 家具 | `bar_furniture` | 六吧椅、三圆桌、十二椅 | 通路诊断、可选碰撞和布局权威坐标 |
| 灯具 | `bar_lighting` | 三吊灯、两后吧线性灯外壳、四壁灯外壳 | 所有真实 Light3D、能量、颜色、阴影和世界切换 |
| 磨损 | `bar_wear_overlays` | 少量戴镜可见擦痕/缺口/污渍叠加网格 | 可见性切换；始终无碰撞 |

导入 GLB 永远只提供视觉。`NeutralGameplay`、`BarLayoutDefinition`、手写 Godot 包装、稳定玩法 ID、碰撞、信号、互锁、存储访问和每日复位保持权威。任何模块缺失或合同失败时，后续包装必须整体回退到完整灰盒，不允许半生产/半灰盒混装。

## 3. 必需根与节点

### `bar_architecture`

- 必需节点：`room_shell`、`south_main_entry`、`south_east_window`、`north_east_service_door`。
- 门窗节点保持独立，方便包装隐藏/替换可动表现；不得合并进 `room_shell`。
- 不建后台区域，不封堵南侧入口、南东窗或北东服务门。

### `bar_counter`

- 必需锚点：`Placement`。
- 八个抽屉根：`front_drawer_1_upper/lower` 至 `front_drawer_4_upper/lower`。
- 固定功能节点：`east_sink`、`sink_plumbing`、`waste_bin`、`employee_gate`、`manual_shelf`。
- 客侧台面必须比未移动的立板向 `+Z` 外挑 `0.30 m`。台下必须为空心柜架；不得恢复与抽屉包络重叠的实心棱柱。
- `east_sink` 下不得有柜门、抽屉、封闭背板或储物碰撞；`sink_plumbing` 仅为可见管线。
- 每个抽屉根的原点位于闭合位置的面板中心；本地 `-Z` 为 `0.38 m` 打开方向。抽屉可见件不得与相邻抽屉合并。

### `bar_backbar`

- 五个下柜各有固定片与活动片：`rear_lower_cabinet_1_fixed/moving` 至 `rear_lower_cabinet_5_fixed/moving`。
- 活动片原点位于闭合片中心，沿本地 X 横移半片宽；不得把两片焊成一个网格。Godot 包装会把 `moving` 视觉绑定到权威 `CabinetInteractable`。
- 十扇上柜门：`back_cabinet_1_left/right` 至 `back_cabinet_5_left/right`。每扇门原点在实际铰链边，打开方向由 Godot 权威布局提供。
- 至少保留 `bottle_rack_bay_1` 与 `bottle_rack_bay_5` 根作为五湾边界诊断；正式模型必须包含五个对齐湾、每湾两层架，不得恢复旧连续瓶架或占位瓶。

### `bar_furniture`

- 必需边界节点：`stool_1`、`stool_6`、`lounge_table_1`、`lounge_table_3`、`lounge_chair_1`、`lounge_chair_12`。
- 实际交付包含连续编号的六吧椅、三圆桌、十二椅，各自保持独立对象与可识别变换。

### `bar_lighting`

- 必需节点：`pendant_1`、`pendant_3`、`rear_linear_1/2`、`east_sconce_1/2`、`west_sconce_1/2`。
- GLB 中不得包含 Light 节点；这里只导出可见外壳。顾客区无可见灯具的填光不进入本模块。

### `bar_wear_overlays`

- 必需节点：`wear_overlay_root`。
- 叠加件不改变主轮廓、不承担碰撞、不复制建筑或吧台主网格。

## 4. 材质与变换规则

- 材质槽使用稳定英文 snake_case 名称；同一视觉家族跨模块复用同名槽。
- 禁止把最终 Light、碰撞、脚本、动画播放器或玩法状态嵌入 GLB。
- 允许移动部件的根节点保持单位 scale；网格局部坐标围绕正确枢轴建模。`Placement`、未来 `Grip` 等锚点可以有局部平移/旋转，但不得有非单位 scale。
- 所有正式 GLB 必须至少包含一个 mesh 和一个 material；导出后使用 `python tools/validate_assets.py assets/asset_manifest.json --allow-placeholders` 校验。

## 5. 占位切换门禁

1. 先运行 `python tools/validate_assets.py --self-test`。
2. 轮廓候选只进入 ignored `artifacts/`；检查点 1 批准前不得创建正式 GLB、包装或切换清单。
3. 正式候选通过合同、包装、实际 Godot Forward+ 复核和用户检查点 2 批准后，才将本批模块 `placeholder` 改为 `false`。
4. 任一未完成模块继续保持 `true`；不得为了让严格验证变绿创建空 GLB 或伪节点。
5. 每次切换后运行完整 `tools/run_verification.ps1`，并在 Godot Forward+ 中检查实际包装截图。
