# 阶段 2 手持资产检查点 1 批次记录

- 批次/日期：阶段 2 / 2026-08-02
- Blender 基线：4.5.5 LTS
- 状态：检查点 1 候选轮廓；必须等待用户审图决定后才可进入材质、正式 GLB、包装场景或 Godot 接入。
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

## 明确未开始项

- 材质：未开始，等待检查点 1。
- 正式 GLB 与资产清单替换：未开始，等待检查点 1。
- Godot 包装场景、世界摆放、手持表现和交互接入：未开始，等待检查点 1。

本记录仅覆盖低细节中性轮廓候选，不构成最终美术、材质、配方或平衡值批准。
