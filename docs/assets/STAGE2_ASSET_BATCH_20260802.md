# 阶段 2 手持资产批次记录

- 批次/日期：阶段 2 / 2026-08-02
- Blender 基线：4.5.5 LTS
- 状态：检查点 1 与检查点 2 均已获用户明确批准；正式 GLB、手写包装、行为接入、清单切换与阶段收尾已完成。
- 范围：`traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large`。
- 坐标约定：米制，`+Y` 向上，`-Z` 向前；每件候选模型的根原点和 `Placement` 对齐至落地接触面。

## 合同尺寸与锚点

| 资产 ID | 合同包络 (X × Y × Z) | 必需锚点 | 候选 GLB 输出 |
|---|---:|---|---|
| `traditional_filter` | `0.36 × 0.24 × 0.30 m` | `Grip`, `Placement`, `Spout`, `Interaction` | `artifacts/stage2_checkpoint1/models/traditional_filter.glb` |
| `bean_scoop` | `0.18 × 0.08 × 0.34 m` | `Grip`, `Placement`, `FillOrigin` | `artifacts/stage2_checkpoint1/models/bean_scoop.glb` |
| `ice_tongs` | `0.10 × 0.08 × 0.46 m` | `Grip`, `Placement`, `Interaction` | `artifacts/stage2_checkpoint1/models/ice_tongs.glb` |
| `jigger_small` | `0.11 × 0.15 × 0.11 m` | `Grip`, `Placement`, `FillOrigin`, `Spout` | `artifacts/stage2_checkpoint1/models/jigger_small.glb` |
| `jigger_large` | `0.15 × 0.21 × 0.15 m` | `Grip`, `Placement`, `FillOrigin`, `Spout` | `artifacts/stage2_checkpoint1/models/jigger_large.glb` |

## 检查点 1 证据

- 候选资产验证：`5/5 OK`，`errors=0`；临时清单：`artifacts/stage2_checkpoint1/review_manifest.json`。
- 中性轮廓审核图：
  - `artifacts/stage2_checkpoint1/stage2_lineup_front.png`
  - `artifacts/stage2_checkpoint1/stage2_lineup_three_quarter.png`
  - `artifacts/stage2_checkpoint1/jigger_family.png`
- 审核图统一使用阶段 1 的中性影棚、地面和相机逻辑。量酒器家族图从左到右为小／中／大，并包含 `0.05 m` 间距刻度柱。

## 检查点 1 审核结果

- 用户要求滤器补足已连接握把和明显漏斗路径、豆铲缩小为宽口浅勺、冰夹改为轻薄张开的双夹片；返修后用户明确回复“通过”。
- 正式候选采用批准的混合 C 材质方向：滤器为暖色低饱和金属，豆铲／冰夹为深色旧钢，大小量酒器为亮银金属。
- 正式 GLB 验证：`artifacts/stage2_final_candidate/review_manifest.json`，五项 `OK`，`SUMMARY assets=5 errors=0`。
- 五项手写包装和左右手行为接入已通过 `STAGE2_ASSET_INTEGRATION_PASS`；碰撞、稳定 ID、现实可交互／眼镜只观察和每日复位语义不变。

## 检查点 2：Godot Forward+ 实机审核

- 捕获场景：`tests/godot/Stage2AssetVisualCapture.tscn`。
- 最终截图目录：`artifacts/visual_review_20260802_stage2_godot/`。
- 运行环境：Godot 4.7.1 Mono，Vulkan Forward+，NVIDIA GeForce RTX 5070 Laptop GPU，`1280 × 720 @ 30 FPS`。
- 运行结果：固定 156 帧，退出码 `0`；输出 `stage200000000.png` 至 `stage200000155.png`。

### 视觉迭代记录

1. 首轮写入同一确定性目录并实际检查：世界队列和量酒器组合可读，但手持豆铲／冰夹角度过侧，显得过小、过暗；冰桶因相机距离过近未进入画面。
2. 第二轮仍写入同一目录并覆盖对应帧：仅调整 `ToolVisualLibrary.ApplyHeldPose()` 中豆铲／冰夹的缩放和朝向，并拉远捕获相机。豆铲浅勺面开始可读，冰夹双臂可读；发现 `ResetForNewDay()` 按既有语义恢复玩家位置，导致冰桶构图再次丢失。
3. 最终轮写入同一目录并覆盖对应帧：在复位后重新设置捕获相机，不改变复位或玩法语义；冰桶、抽屉、滤器和冰夹获得稳定组合画面。冰夹再做一次仅限手持表现的轻微缩小和斜向校准，使双薄臂、张开间隙和端部夹头同时可见。

每次表现层返修后均重新运行正式五项 GLB 验证和阶段 2 Godot 集成测试；最终结果为 `SUMMARY assets=5 errors=0`、`STAGE2_ASSET_INTEGRATION_PASS`，Debug 构建 0 警告、0 错误。

### 最终关键帧

| 检查内容 | PNG |
|---|---|
| 现实世界五项队列、落台与三种金属方向 | `artifacts/visual_review_20260802_stage2_godot/stage200000018.png` |
| 眼镜世界队列、材质覆盖与观察标签 | `artifacts/visual_review_20260802_stage2_godot/stage200000036.png` |
| 左手滤器＋右手豆铲 | `artifacts/visual_review_20260802_stage2_godot/stage200000058.png` |
| 左手滤器＋右手冰夹＋打开的冰桶抽屉 | `artifacts/visual_review_20260802_stage2_godot/stage200000076.png` |
| 滤器／冰夹手持与台面高球杯净空 | `artifacts/visual_review_20260802_stage2_godot/stage200000092.png` |
| 高球杯＋小量酒器 | `artifacts/visual_review_20260802_stage2_godot/stage200000112.png` |
| 高球杯＋大量酒器 | `artifacts/visual_review_20260802_stage2_godot/stage200000140.png` |

### 人工视觉结论

- 世界摆放：五项资产均完整落在前吧台可读区域；滤器底部出口、豆铲浅勺口、冰夹张口和三个量酒器的尺寸序列均可辨。
- 材质：暖色滤器有连续金属边缘高光；深色旧钢豆铲／冰夹以亮边保留轮廓；大小量酒器以亮银杯沿和腰环与深色工具区分。眼镜世界统一青绿色覆盖正常恢复为观察层，不保留现实材质差异。
- 手持：滤器握把连接和底部出口未被遮挡；豆铲浅勺面与暗色握柄可区分；冰夹两片薄臂保持张开并在冰桶前留有净空；高球杯透明杯壁与大小量酒器均无互相穿插。
- 构图：现实／眼镜队列、滤器＋豆铲、滤器＋冰夹＋冰桶、滤器／冰夹＋高球杯、两组高球杯＋量酒器均有稳定帧。眼镜世界的量酒器标签在队列密集处存在原型级文字重叠，但不遮挡资产主体或材质覆盖判断，本批不改公共标签逻辑。

## 当前审批边界

用户已于最终关键帧审核后明确回复“通过”，检查点 2 批准成立。两份正式资产清单只将本批五项切换为非占位；阶段 3／4 的 7 项继续保持灰盒。此次批准仅代表五项 M1 技术／材质候选可进入原型，不构成最终美术、正式容量、配方、平衡值或顾客内容批准，也不授权开始阶段 3。

## 2026-08-02 检查点 2 内部审图后视觉返修

- 审核反馈：量酒器在现实暖光中一度接近黑色／铜色；豆铲浅勺面过亮且手持过大；冰夹夹头过小、单面，弹性连接未入画，整体易读成两根粗杆。
- 材质修正：量酒器主体改为低金属度的冷银基色，并保留高金属度亮银杯沿和腰环；豆铲／冰夹改为更暗、更粗糙的缎面旧钢，压低白色与金色镜面峰值。
- 豆铲修正：手持缩放由 `1.08` 降至 `0.90`，勺面改为斜视，最终画面可同时读出宽口、浅腹、低前缘和独立握柄。
- 冰夹修正：夹头由单面 8 边小扇面改为闭合双面的 12 边凹形椭圆小勺，并在 `0.10 × 0.08 × 0.46 m` 合同包络内放大；握点移至双臂中段，手持姿态露出两片细长臂、张开间距、两个夹头和底部 U 形弹簧。
- 最终实机复拍仍为 Godot 4.7.1 Mono、Vulkan Forward+、RTX 5070 Laptop、`1280 × 720 @ 30 FPS`、156 帧、退出码 `0`；关键帧沿用上表路径并已覆盖为最终版本。
- 返修完成时的审核边界：公共眼镜标签重叠仍为非阻塞原型问题；该返修未修改玩法、碰撞、清单或占位标记。随后用户明确批准检查点 2，Task 7 才切换本批五项清单状态并完成归档。

## 阶段收尾与完整验证

- 两次用户批准：检查点 1 在滤器握把连接、漏斗读形、浅口豆铲和轻薄张开冰夹返修后通过；检查点 2 在最终 Godot Forward+ 关键帧复核后通过。
- 正式清单：`traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large` 在 `assets/asset_manifest.json` 与 `data/assets/asset_manifest.tres` 中均为非占位；其余 7 项保持占位灰盒。
- 完整验证命令：`powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1`，退出码 `0`。
- 资产验证器自测：good／bad／wrong-root 三组均 `PASS`；正式清单 `SUMMARY assets=16 errors=0`。阶段 1 四项与阶段 2 五项均输出 `OK`，其余 7 项输出允许的 `graybox placeholder`。
- 构建与领域测试：Debug `0` 警告／`0` 错误；Release `0` 警告／`0` 错误；领域测试失败 `0`、通过 `28`、跳过 `0`、总计 `28`。
- Godot：编辑器导入通过，并依次出现 `SMOKE_TESTS_PASS`、`STAGE1_ASSET_INTEGRATION_PASS`、`STAGE2_ASSET_INTEGRATION_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS`。
- 阶段 3 未开始；稳定 ID、碰撞、玩法状态、现实可交互／眼镜只观察、正式容量／配方／平衡／顾客内容均未改变。
