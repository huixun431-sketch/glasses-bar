# 完整酒吧建筑轮廓检查点 1

日期：2026-08-03

阶段：`silhouette-review`，等待用户明确批准

候选：`artifacts/blender_candidates/bar_architecture.glb`（ignored，不是正式资产）

## 合同与生成结果

- Blender：4.5.5 LTS；确定性主场景位于 ignored `artifacts/blender/bar_interior_master.blend`。
- 权威尺寸：房间 `16.00 × 10.00 × 4.50 m`；南主入口 `1.40 × 2.10 m`；南东窗 `3.20 × 1.55 m`、窗台 `0.75 m`；北东服务门 `0.90 × 2.10 m`。
- 六个模块集合和单位导出根已建立；本检查点只填充 `bar_architecture`，其他五个集合保持空根。
- `BAR_MODEL_CONTRACT_PASS`；候选 GLB 验证 `assets=1 errors=0`；正式清单继续为 `assets=21 errors=0`，六个环境模块全部 `placeholder: true`。
- 未创建正式 `assets/models/bar_architecture.glb`、Godot 包装、碰撞、灯光或清单切换。

## 人工视觉检查

- 首轮图因相机过窄、曝光过高和 Y-up 相机翻滚未通过，已修正后完全重拍；首轮不得作为证据。
- 第二轮六图均为 `1600×1000` 中性黏土审查图，已逐张打开检查。
- 北／南立面可同时说明北东服务门、南双门和南东窗与墙体分段的关系；墙体未跨越开口。
- 1.05 m 护墙板绕开门窗，窗下仅保留 0.75 m 高护墙；地板窄板沿南北方向延伸。
- 俯视图显示 16:10 房间比例与三处开口位置；1.83 m 橙色人体参照在立面和三分之四视角均为竖直且落地。
- 未发现后台几何、重复稳定名、越出房间的内部结构或正式材质细节。

## 审查图与 SHA-256

- `01_north_interior.png` — `aec71e3e4e2035f8666cd886a9fea3c3afce2da21b6b115e00e7502d11c8cb19`
- `02_south_interior.png` — `0e56c42ed097e9bb0dcf02d45f3f13a5e3c9790e4cc3707a904de00a188e399a`
- `03_east_interior.png` — `ab96a0ef39ace50120dc91446f3a8340a19516fe4064981b1ca2499323cd3df4`
- `04_west_interior.png` — `f5418bde0560bbc7ac9af5314af6d2a196fd972d1c726eadcf1665b2ba9cd695`
- `05_overhead.png` — `9d858850393aba803cb12c2859f1c2580b11463e70f400ba4c852981b492cd76`
- `06_interior_three_quarter.png` — `107acb0c883abdffe7b451aae8f22a2d69a6602f2eb942e68dc81de8bd771c30`

## 当前门禁

等待用户对建筑轮廓检查点 1 明确批准或提出返修。批准前不得运行 `formal-candidate`，不得开始前后吧正式模型、材质抛光、Godot 包装或 placeholder 切换。
