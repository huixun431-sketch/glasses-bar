# 完整酒吧建筑轮廓检查点 1

日期：2026-08-03

阶段：`silhouette-review`，已批准

批准人：`user`

批准记录：用户在指出南门疑似异物后明确说明“其余部分符合预期，继续执行计划”；经确认疑似异物为审查 AREA 灯穿过开口产生的矩形投影，并改用无阴影方向光完整重拍后，按该条件批准继续进入正式候选阶段。

候选：`artifacts/blender_candidates/bar_architecture.glb`（ignored，不是正式资产）

## 合同与生成结果

- Blender：4.5.5 LTS；确定性主场景位于 ignored `artifacts/blender/bar_interior_master.blend`。
- 权威尺寸：房间 `16.00 × 10.00 × 4.50 m`；南主入口 `1.40 × 2.10 m`；南东窗 `3.20 × 1.55 m`、窗台 `0.75 m`；北东服务门 `0.90 × 2.10 m`。
- 六个模块集合和单位导出根已建立；本检查点只填充 `bar_architecture`，其他五个集合保持空根。
- `BAR_MODEL_CONTRACT_PASS`；候选 GLB 验证 `assets=1 errors=0`；正式清单继续为 `assets=21 errors=0`，六个环境模块全部 `placeholder: true`。
- 未创建正式 `assets/models/bar_architecture.glb`、Godot 包装、碰撞、灯光或清单切换。

## 人工视觉检查

- 首轮图因相机过窄、曝光过高和 Y-up 相机翻滚未通过；第二轮南门旁出现 AREA 灯穿过开口造成的矩形投影。两轮均不得作为证据。
- 最终轮改为世界环境光与无阴影方向光，六图均为 `1600×1000` 中性审查图，已逐张打开检查；南门矩形投影消失，门洞附近对象边界检查仅包含房间、门、把手、门缝、护墙和地板。
- 北／南立面可同时说明北东服务门、南双门和南东窗与墙体分段的关系；墙体未跨越开口。
- 1.05 m 护墙板绕开门窗，窗下仅保留 0.75 m 高护墙；地板窄板沿南北方向延伸。
- 俯视图显示 16:10 房间比例与三处开口位置；1.83 m 橙色人体参照在立面和三分之四视角均为竖直且落地。
- 未发现后台几何、重复稳定名、越出房间的内部结构或正式材质细节。

## 审查图与 SHA-256

- `01_north_interior.png` — `b21996a1ec1605f8b4af34030b794cc521148e8dce27ce0ac56ab428c1b80871`
- `02_south_interior.png` — `3bdb12fcd27af59cdea101fa8bd08d0448909141f516c2915e0eb5b30d42bb43`
- `03_east_interior.png` — `225fff2849599bfdad1d0b9014a8ea28250eff07d67ada446c341b92a45eefd7`
- `04_west_interior.png` — `a018b4135b1d312702d332392f02cbae58db2b6dba468251e2fa4e8286cdf15d`
- `05_overhead.png` — `49ae36010afa41008e2cae3ca2b3ebd015916534b5ab6422dc3279ec6808d3dd`
- `06_interior_three_quarter.png` — `31b003ee9d0a8a0683adb3f08b2500289532bfe88b0745fd98d1a6131b518088`

## 当前门禁

检查点 1 已由用户批准，可运行 `formal-candidate` 并继续前后吧正式候选模型。六个环境模块仍保持 `placeholder: true`；在 Godot Forward+ 检查点 2 获得用户明确批准前不得切换正式资产。
