# Asset batch record: `bar-architecture`

- Stage: `approved checkpoint 1 formal candidate`
- Status: REVIEW READY；Forward+ 检查点 2 等待用户决定，尚未替换游戏内灰盒。
- Candidate root: `artifacts/blender_candidates`
- Formal model root: `assets/models`
- Wrapper root: `scenes/environment/modules`
- JSON manifest: `assets/asset_manifest.json`

## Scope and contracts

| Asset ID | Runtime ID | Required anchors | Interaction kind |
|---|---|---|---|
| `bar_architecture` | `bar_architecture` | `Placement` | `environment_visual` |

当前 Z3/H3 权威建筑尺度为 `16.00 × 10.00 × 4.50 m`。正式候选沿用已批准南侧双门、南东景观窗、北东服务门、`1.05 m` 护墙与南北向地板，不恢复被打回的旧 `12 × 9 × 3.5 m` 方案。

## Checkpoint 1: neutral Blender silhouette review

- 候选合同：`BAR_MODEL_CONTRACT_PASS`；单 GLB 验证 `assets=1 errors=0`。
- 证据：`artifacts/blender_review/architecture/01_north_interior.png` 至 `06_interior_three_quarter.png`，六张 `1600×1000` PNG。
- 修正：南门矩形异物由 AREA 灯穿过门洞的矩形投影造成；改用世界环境光与无阴影 SUN 灯后消失，门附近对象边界未发现场景残留。
- 用户决定：approved。
- Approval record：用户确认“其余部分符合预期，继续执行计划”。

## Formal candidate and behavior integration

- 正式 GLB：六个环境模块均已导出到 `assets/models/`，每个模块附带独立 `.manifest.json`；正式清单仍保持 `placeholder: true`。
- 手写包装：`scenes/environment/modules/` 下六个包装只实例化视觉 GLB，并保留稳定 `asset_id` 元数据。
- 建筑锚点：`Placement` 存在；导入根、房间包络及三处开口通过合同检查。
- 坐标轴回归：主文件已按项目 Y-up 编写，导出改为不重复执行 Z-up → Y-up 转换；Godot 端 `room_shell` AABB 固定为 `16 × 4.5 × 10 m`。
- 所有权边界：候选 GLB 不含碰撞与真实灯光。灰盒玩法、碰撞、抽屉／门／手册状态仍是唯一权威；本轮仅创建预览场景，没有接管正式运行时。

## Checkpoint 2: Godot Forward+ visual review

- Capture scene：`tests/godot/BarInteriorFormalVisualCapture.tscn`。
- Renderer：Godot `4.7.1.stable.mono`，Vulkan Forward+，NVIDIA GeForce RTX 5070 Laptop GPU，`1920×1080`。
- 实际证据：`artifacts/bar-interior-formal-forward-plus/01_player_eye_backbar.png` 至 `06_overhead_layout.png`，六张均已逐张打开检查。
- SHA-256：
  - `01_player_eye_backbar.png` `220ee08e1ab5772ef29bf39ecc78006ede8955219a72b9d421e589275d21975c`
  - `02_customer_eye_front.png` `94d20ccc08f5ca147aa1efc4dccbfa1f848bc0ff2b97148aa56b92c7dd163ea7`
  - `03_east_sink_waste_gate.png` `69f2453f404f3386384bf70e08a3c9cc4db7733a991670e3c51933b7e7e02a44`
  - `04_west_manual_only.png` `fc2d54ce94696b8771c6e3a8c9bd4f6514371f53b9556c53e1c2e8826006aeba`
  - `05_customer_lounge_and_lights.png` `d405a34d959c27d0168042016af9107f83fa0176c151b34f046a31e4982ed056`
  - `06_overhead_layout.png` `c77191dbe57346ef1165d5f65295e32fcd269958efaf8f47baa62add9da25443`
- 检查结果：建筑、前后吧、五湾双层规整瓶架、家具和灯具方向正确且未越界；西侧只保留手册回位区，东侧展示开放水槽下方、弃物区与员工门；俯视图未出现南门异物。
- 集成证据：`artifacts/bar-interior-formal-forward-plus/integration.log` 同时记录 Vulkan Forward+ 捕获 PASS 和建筑包装集成 PASS。
- 用户决定：pending。
- Approval record：pending；在用户批准前不切换清单占位状态，也不替换运行时灰盒。

## Completion

### Completed items

- 完成检查点 1 修正与用户批准归档。
- 完成六模块正式样式候选、GLB/sidecar、手写预览包装和 Forward+ 检查图。
- 完成轴向、稳定 ID、锚点、无碰撞／无灯光和房间包络回归。
- 完成灰盒吧台面顶面专项截图与运行时三角形面积回归。

### Key decisions

- 南门异物只修审查灯光，不删除或掩藏建筑节点。
- 正式候选沿用 Z3/H3 `16 × 10 × 4.5 m`、`9.10 m` 前吧及当前功能收敛，不回退到旧设计。
- 视觉 GLB 与玩法状态继续分离；检查点 2 批准之前，六项环境清单全部保持占位并保留完整灰盒。

### Remaining TODOs

- 等待用户审查并决定正式几何／材质检查点 2。
- 获批后实现生产视觉加载器、移动件视觉绑定、现实／眼镜材质与灯光切换，以及任一模块失败时的全灰盒回退。
- 完成最终生产/眼镜/回退三组 Forward+ QA 后，才可将六个环境模块从 placeholder 切换为正式状态。

## Verification summary

- Command：Blender 合同、六份 sidecar 资产验证、Godot 包装集成、Forward+ 捕获、批次 `forward-plus-review` 门禁及项目一键回归。
- Status: PASS for the current Forward+ review gate；用户批准与运行时替换不在本状态内。
- Evidence：`artifacts/bar-interior-formal-forward-plus/integration.log` 与本记录所列 PNG。
- Asset manifest scope：环境六模块均保留 `placeholder: true`；既有已批准手持资产状态不变。
- Stage boundary：停在检查点 2，未执行正式运行时替换。
