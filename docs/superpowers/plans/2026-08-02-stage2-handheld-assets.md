# 第二阶段手持核心工具资产实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large` 制作可复现的低多边形 GLB、手写 Godot 包装与实际画面验证，并在 Blender 轮廓和 Godot 实景两个检查点等待用户批准。

**Architecture:** 以纯 Python 合同文件集中定义五项资产的稳定 ID、审核用包络、锚点与材质组；Blender 4.5.5 LTS 生成器读取该合同，并复用阶段 1 生成器的坐标转换、网格和 GLB 导出帮助函数。检查点 1 的候选模型只写入被忽略的 `artifacts/`，通过后才输出到 `assets/models/`；Godot 继续通过 `scenes/assets/stage2/*.tscn` 手写包装和 `ToolVisualLibrary` 接入，任何失败均保留原灰盒、碰撞和玩法状态。

**Tech Stack:** Python 3 `unittest`、Blender 4.5.5 LTS Python API、glTF 2.0/GLB、Godot 4.7.1 .NET/C#、.NET 8、PowerShell。

## Global Constraints

- 稳定 ID 只能是 `traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large`。
- GLB 使用米制、`+Y` 向上、`-Z` 向前；节点缩放必须为 `[1, 1, 1]`。
- 必需锚点名称大小写精确匹配：`Grip`、`Placement`、`FillOrigin`、`Spout`、`Interaction`。
- `traditional_filter` 左手握持；其余四项右手握持；不得改变现有左右手玩法分类。
- `jigger_small` 和 `jigger_large` 必须从 `tools/modeling/generate_stage1_assets.py::build_jigger_medium()` 的杯体比例、杯沿、腰环、锚点方向和亮银高光衍生。
- 导入 GLB 只能作为手写包装场景的视觉子场景；碰撞、脚本、稳定 ID、玩法状态和双世界切换仍由手写 Godot 层持有。
- 检查点 1 通过前不得写入正式资产路径或精修材质；检查点 2 通过前不得将五项清单状态报告为完成。
- 灰盒回退必须保留到结构、导入、交互、实际 Forward+ 截图和全量回归全部通过。
- 不改变配方、容量、平衡、顾客内容、动作提交时机、液体语义、双世界合同或其他 7 项占位资产。
- 不新增骨骼、IK、动画系统或运行时材质框架；不提交 `.blend`、`.glb.import` 的手工改写或 `artifacts/` 截图。

## File Structure

- `tools/modeling/stage2_asset_contract.py`：五项稳定 ID、审核包络、锚点、手别和材质组的纯 Python 权威合同。
- `tools/modeling/generate_stage2_assets.py`：读取合同，复用阶段 1 Blender 帮助函数，生成轮廓候选或最终 GLB。
- `tools/modeling/render_stage2_review.py`：生成检查点 1 的正面、三分之四与量酒器家族对比图。
- `tests/tools/test_stage2_asset_contract.py`：验证合同完整性、量酒器比例和锚点集合。
- `assets/models/<stage2-id>.glb`：检查点 1 通过后生成的五项最终技术样件。
- `scenes/assets/stage2/<stage2-id>.tscn`：五项手写包装场景，只实例化 GLB 并提供稳定 metadata。
- `tests/godot/Stage2AssetIntegrationTests.cs` / `.tscn`：锁定包装、锚点、灰盒回退、左右手和双世界合同。
- `tests/godot/Stage2AssetVisualCapture.cs` / `.tscn`：驱动检查点 2 的世界、手持与组合画面。
- `scripts/assets/ToolVisualLibrary.cs`：扩展稳定 ID 到包装路径及手持姿态的集中映射。
- `assets/asset_manifest.json`、`data/assets/asset_manifest.tres`：仅在最终 GLB 和包装验证后把这五项改为非占位。
- `tools/run_verification.ps1`：把阶段 2 集成测试加入全量验证。
- `docs/assets/STAGE2_ASSET_BATCH_20260802.md`：记录尺寸、锚点、两次用户批准、返修与验证证据。

---

### Task 1: 锁定阶段 2 纯 Python 资产合同

**Files:**
- Create: `tools/modeling/stage2_asset_contract.py`
- Create: `tests/tools/test_stage2_asset_contract.py`

**Interfaces:**
- Produces: `AssetContract(asset_id, envelope, anchors, hand, material_group)`、只读映射 `STAGE2_ASSETS`、`validate_contracts(contracts) -> list[str]` 和 `review_manifest_assets(model_prefix) -> list[dict]`。
- Consumes: `GameplaySceneComposer.ToolMesh()`、`HeldToolPresenter.UpdateMeshes()` 的现有灰盒包络，以及阶段 1 `jigger_medium` 的 `0.18 m` 高、`0.13 m` 最大直径和锚点方向。

- [ ] **Step 1: 写失败的合同测试**

  创建 `tests/tools/test_stage2_asset_contract.py`：

  ```python
  import sys
  import unittest
  from pathlib import Path

  sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools" / "modeling"))
  from stage2_asset_contract import (
      AssetContract, STAGE2_ASSETS, review_manifest_assets, validate_contracts,
  )


  class Stage2AssetContractTests(unittest.TestCase):
      def test_approved_contracts_validate_and_emit_review_manifest_entries(self):
          self.assertEqual(validate_contracts(STAGE2_ASSETS), [])
          entries = review_manifest_assets("models")
          self.assertEqual([entry["id"] for entry in entries], list(STAGE2_ASSETS))
          self.assertTrue(all(entry["placeholder"] is False for entry in entries))
          self.assertEqual(
              entries[0],
              {
                  "id": "traditional_filter",
                  "path": "models/traditional_filter.glb",
                  "placeholder": False,
                  "required_anchors": ["Grip", "Placement", "Spout", "Interaction"],
              },
          )

      def test_validation_rejects_invalid_hand_envelope_anchor_and_material_group(self):
          broken = {
              "broken": AssetContract(
                  "broken", (0.2, 0.0, 0.1), ("Grip",), "center", "painted_plastic"
              )
          }
          self.assertEqual(
              validate_contracts(broken),
              [
                  "broken: envelope dimensions must be positive meters",
                  "broken: Placement anchor is required",
                  "broken: hand must be left or right",
                  "broken: unknown material group painted_plastic",
              ],
          )
  ```

- [ ] **Step 2: 运行测试确认失败**

  Run: `python -m unittest discover -s tests/tools -p "test_stage2_asset_contract.py" -v`

  Expected: FAIL，错误包含 `ModuleNotFoundError: No module named 'stage2_asset_contract'`。

- [ ] **Step 3: 实现最小合同模块**

  在 `stage2_asset_contract.py` 定义冻结 dataclass 与以下审核包络：

  ```python
  from dataclasses import dataclass


  @dataclass(frozen=True)
  class AssetContract:
      asset_id: str
      envelope: tuple[float, float, float]  # width, height, depth in meters
      anchors: tuple[str, ...]
      hand: str
      material_group: str


  STAGE2_ASSETS = {
      "traditional_filter": AssetContract("traditional_filter", (0.36, 0.24, 0.30), ("Grip", "Placement", "Spout", "Interaction"), "left", "warm_brushed"),
      "bean_scoop": AssetContract("bean_scoop", (0.18, 0.08, 0.34), ("Grip", "Placement", "FillOrigin"), "right", "dark_satin"),
      "ice_tongs": AssetContract("ice_tongs", (0.10, 0.08, 0.46), ("Grip", "Placement", "Interaction"), "right", "dark_satin"),
      "jigger_small": AssetContract("jigger_small", (0.11, 0.15, 0.11), ("Grip", "Placement", "FillOrigin", "Spout"), "right", "bright_silver"),
      "jigger_large": AssetContract("jigger_large", (0.15, 0.21, 0.15), ("Grip", "Placement", "FillOrigin", "Spout"), "right", "bright_silver"),
  }

  MATERIAL_GROUPS = {"warm_brushed", "dark_satin", "bright_silver"}


  def validate_contracts(contracts: dict[str, AssetContract]) -> list[str]:
      errors: list[str] = []
      for key, contract in contracts.items():
          if contract.asset_id != key:
              errors.append(f"{key}: asset_id must match mapping key")
          if any(dimension <= 0 for dimension in contract.envelope):
              errors.append(f"{key}: envelope dimensions must be positive meters")
          if "Placement" not in contract.anchors:
              errors.append(f"{key}: Placement anchor is required")
          if contract.hand not in {"left", "right"}:
              errors.append(f"{key}: hand must be left or right")
          if contract.material_group not in MATERIAL_GROUPS:
              errors.append(f"{key}: unknown material group {contract.material_group}")
      return errors


  def review_manifest_assets(model_prefix: str) -> list[dict]:
      return [
          {
              "id": contract.asset_id,
              "path": f"{model_prefix}/{contract.asset_id}.glb",
              "placeholder": False,
              "required_anchors": list(contract.anchors),
          }
          for contract in STAGE2_ASSETS.values()
      ]
  ```

- [ ] **Step 4: 运行合同测试确认通过**

  Run: `python -m unittest discover -s tests/tools -p "test_stage2_asset_contract.py" -v`

  Expected: 2 tests PASS，退出码 0；把任一错误合同恢复为有效值会使第二项测试失败，证明测试覆盖真实校验分支。

- [ ] **Step 5: 提交合同与测试**

  ```powershell
  git add -- tools/modeling/stage2_asset_contract.py tests/tools/test_stage2_asset_contract.py
  git commit -m "test: lock stage two asset modeling contract"
  ```

### Task 2: 生成五项中性轮廓候选与临时校验清单

**Files:**
- Create: `tools/modeling/generate_stage2_assets.py`
- Test: `tools/validate_assets.py`
- Output ignored: `artifacts/stage2_checkpoint1/models/*.glb`
- Output ignored: `artifacts/stage2_checkpoint1/review_manifest.json`

**Interfaces:**
- Consumes: `STAGE2_ASSETS`；阶段 1 的 `reset_scene()`、`make_material()`、`add_root()`、`add_anchor()`、`add_cylinder()`、`add_torus()`、`add_frustum_shell()`、`export_asset()`。
- Produces: `build_traditional_filter(materials)`、`build_bean_scoop(materials)`、`build_ice_tongs(materials)`、`build_jigger_variant(asset_id, height, target_radius, materials)` 和 `write_review_manifest(output_root)`。

- [ ] **Step 1: 运行不存在的生成器确认失败**

  Run:

  ```powershell
  & 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage2_assets.py -- --mode silhouette --output artifacts/stage2_checkpoint1/models
  ```

  Expected: FAIL，因为生成器尚不存在。

- [ ] **Step 2: 实现中性轮廓生成器**

  生成器从同目录导入阶段 1 帮助函数，并使用统一中性材料；各构建器只实现审核轮廓和锚点：

  ```python
  from generate_stage1_assets import (
      add_anchor, add_cylinder, add_frustum_shell, add_root, add_torus,
      export_asset, make_material, reset_scene,
  )
  from stage2_asset_contract import STAGE2_ASSETS

  BUILDERS = {
      "traditional_filter": build_traditional_filter,
      "bean_scoop": build_bean_scoop,
      "ice_tongs": build_ice_tongs,
      "jigger_small": lambda materials: build_jigger_variant("jigger_small", 0.15, 0.055, materials),
      "jigger_large": lambda materials: build_jigger_variant("jigger_large", 0.21, 0.075, materials),
  }
  ```

  - 过滤器：16 边低矮滤杯、内层滤网盘、短侧握把、底部出口和稳定足环。
  - 豆铲：12 边开口铲腹、短柄和末端握持片；`FillOrigin` 位于铲腹中心。
  - 冰夹：两条低多边形夹臂、U 形弹性连接和清楚的双夹口；`Interaction` 位于夹口中心。
  - 量酒器：以 `z_scale = height / 0.18` 和 `radial_scale = target_radius / 0.065` 分别缩放中号母版的高度与径向尺寸；`height` / `target_radius` 直接取现有灰盒 `0.15 / 0.055 m` 与 `0.21 / 0.075 m`，不单独猜测新比例：

    ```python
    z_scale = height / 0.18
    radial_scale = target_radius / 0.065
    add_frustum_shell(root, "LowerCup", 0.0, 0.078 * z_scale,
                      0.055 * radial_scale, 0.023 * radial_scale,
                      0.049 * radial_scale, 0.018 * radial_scale, metal,
                      close_bottom=True, close_top=False)
    add_cylinder(root, "Waist", 0.023 * radial_scale, 0.032 * z_scale, 0.094 * z_scale, metal)
    add_frustum_shell(root, "UpperCup", 0.110 * z_scale, height,
                      0.023 * radial_scale, target_radius,
                      0.018 * radial_scale, 0.058 * radial_scale, metal,
                      close_bottom=False, close_top=True)
    add_anchor(root, "Grip", (0.0, 0.09 * z_scale, 0.0))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "FillOrigin", (0.0, 0.105 * z_scale, 0.0))
    add_anchor(root, "Spout", (0.0, height, -target_radius))
    ```
  - `write_review_manifest()` 调用 `review_manifest_assets("models")` 写入米制、轴向、五项 `placeholder=false` 和对应锚点；该文件只位于 `artifacts/`，不修改正式清单。

- [ ] **Step 3: 后台生成候选 GLB**

  Run:

  ```powershell
  & 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage2_assets.py -- --mode silhouette --output artifacts/stage2_checkpoint1/models
  ```

  Expected: 五行 `WROTE ...glb` 和一行 `WROTE ...review_manifest.json`，退出码 0。

- [ ] **Step 4: 验证候选结构与锚点**

  Run: `python tools/validate_assets.py artifacts/stage2_checkpoint1/review_manifest.json`

  Expected: 五项全部 `OK`，`SUMMARY assets=5 errors=0`。

- [ ] **Step 5: 提交可复现生成器，不提交候选二进制**

  ```powershell
  git add -- tools/modeling/generate_stage2_assets.py
  git commit -m "feat: generate stage two silhouette candidates"
  ```

### Task 3: 生成 Blender 检查点 1 图并硬停等待用户

**Files:**
- Create: `tools/modeling/render_stage2_review.py`
- Create: `docs/assets/STAGE2_ASSET_BATCH_20260802.md`
- Output ignored: `artifacts/stage2_checkpoint1/stage2_lineup_front.png`
- Output ignored: `artifacts/stage2_checkpoint1/stage2_lineup_three_quarter.png`
- Output ignored: `artifacts/stage2_checkpoint1/jigger_family.png`

**Interfaces:**
- Consumes: 五项候选 GLB 和已批准的 `assets/models/jigger_medium.glb`。
- Produces: 同一中性灯光、同一地面和尺寸刻度下的正面、三分之四及三量酒器家族图片。

- [ ] **Step 1: 实现审核渲染器**

  `render_stage2_review.py` 复用阶段 1 `add_studio()`、`add_camera()` 和 `import_asset()` 的灯光／相机逻辑，增加：

  ```python
  REVIEW_ASSETS = (
      "traditional_filter", "bean_scoop", "ice_tongs", "jigger_small", "jigger_large"
  )
  LINEUP_X = {
      "traditional_filter": -0.82,
      "bean_scoop": -0.40,
      "ice_tongs": 0.00,
      "jigger_small": 0.38,
      "jigger_large": 0.70,
  }

  def render_lineup(candidate_root: Path, output_root: Path, three_quarter: bool) -> None:
      reset_scene()
      add_studio()
      for asset_id, x in LINEUP_X.items():
          root = import_asset(candidate_root, asset_id)
          root.location.x = x
      camera = (1.15, -2.55, 0.78) if three_quarter else (0.0, -2.85, 0.65)
      add_camera(camera, (-0.04, 0.0, 0.13), 62.0)
      scene = bpy.context.scene
      scene.render.resolution_x = 1600
      scene.render.resolution_y = 900
      suffix = "three_quarter" if three_quarter else "front"
      scene.render.filepath = str(output_root / f"stage2_lineup_{suffix}.png")
      bpy.ops.render.render(write_still=True)

  def render_jigger_family(candidate_root: Path, stage1_root: Path, output_root: Path) -> None:
      reset_scene()
      add_studio()
      for asset_id, root_path, x in (
          ("jigger_small", candidate_root, -0.22),
          ("jigger_medium", stage1_root, 0.0),
          ("jigger_large", candidate_root, 0.24),
      ):
          root = import_asset(root_path, asset_id)
          root.location.x = x
      add_camera((0.62, -1.35, 0.46), (0.0, 0.0, 0.10), 66.0)
      scene = bpy.context.scene
      scene.render.resolution_x = 1200
      scene.render.resolution_y = 900
      scene.render.filepath = str(output_root / "jigger_family.png")
      bpy.ops.render.render(write_still=True)
  ```

  正面图保持各资产操作方向可读；三分之四图显示滤杯内部、豆铲装载区和冰夹夹口；量酒器家族图按小／中／大排列，并在背景加入 `0.05 m` 间距的刻度柱。

- [ ] **Step 2: 运行 Blender 审核渲染**

  Run:

  ```powershell
  & 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/render_stage2_review.py -- --candidates artifacts/stage2_checkpoint1/models --stage1 assets/models --output artifacts/stage2_checkpoint1
  ```

  Expected: 三行 `RENDERED ...png`，退出码 0。

- [ ] **Step 3: 实际检查三张 PNG**

  使用本地图片查看工具逐张检查：五件轮廓互不混淆；过滤出口、铲腹和夹口方向可读；小／中／大量酒器尺寸阶梯清楚；所有模型稳定落地且没有穿插。

- [ ] **Step 4: 记录候选尺寸与检查结果**

  `docs/assets/STAGE2_ASSET_BATCH_20260802.md` 记录合同包络、锚点、候选输出路径、资产验证 `5/5 OK`，并将材质和 Godot 状态明确标为“未开始，等待检查点 1”。

- [ ] **Step 5: 提交审核工具与批次记录**

  ```powershell
  git add -- tools/modeling/render_stage2_review.py docs/assets/STAGE2_ASSET_BATCH_20260802.md
  git commit -m "chore: hold stage two silhouettes for review"
  ```

- [ ] **Step 6: 检查点 1——向用户展示并停止**

  展示三张实际 Blender PNG，说明这些是低细节中性轮廓。必须等待用户明确回复“通过”或给出返修意见；不得继续 Task 4。

### Task 4: 完成批准材质、正式 GLB 与手写包装

**Files:**
- Modify: `tools/modeling/generate_stage2_assets.py`
- Create: `assets/models/traditional_filter.glb`
- Create: `assets/models/bean_scoop.glb`
- Create: `assets/models/ice_tongs.glb`
- Create: `assets/models/jigger_small.glb`
- Create: `assets/models/jigger_large.glb`
- Create: `scenes/assets/stage2/traditional_filter.tscn`
- Create: `scenes/assets/stage2/bean_scoop.tscn`
- Create: `scenes/assets/stage2/ice_tongs.tscn`
- Create: `scenes/assets/stage2/jigger_small.tscn`
- Create: `scenes/assets/stage2/jigger_large.tscn`

**Interfaces:**
- Consumes: 用户批准的 Task 3 轮廓和混合 C 材质映射。
- Produces: `--mode final` 五项正式 GLB和包装根 metadata `asset_id`；正式清单仍保持阶段 2 五项为占位，直到检查点 2 通过。

- [ ] **Step 1: 运行尚未实现的最终生成模式确认失败**

  Run:

  ```powershell
  & 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage2_assets.py -- --mode final --output artifacts/stage2_final_candidate/models
  ```

  Expected: FAIL，参数解析或模式分派明确报告 `final` 尚未支持；失败发生在写入任何正式 GLB 之前。

- [ ] **Step 2: 实现混合 C 最小 PBR 参数**

  在 Blender 生成器中加入以下批准参数，并由实际生成、GLB 结构验证和后续 Forward+ 审图验证最终行为：

  ```python
  FINAL_MATERIALS = {
      "warm_brushed": {"color": (0.46, 0.29, 0.10, 1.0), "metallic": 0.76, "roughness": 0.34},
      "dark_satin": {"color": (0.16, 0.18, 0.20, 1.0), "metallic": 0.72, "roughness": 0.27},
      "bright_silver": {"color": (0.72, 0.78, 0.84, 1.0), "metallic": 0.78, "roughness": 0.17},
  }
  ```

  亮银杯沿／腰环继续复用阶段 1 `Worn_Silver_Edge` 的明度、金属度和粗糙度关系；暖金属与旧钢不增加纹理贴图、锈迹或污渍。

- [ ] **Step 3: 重新运行合同测试并生成正式 GLB**

  Run:

  ```powershell
  python -m unittest discover -s tests/tools -p "test_stage2_asset_contract.py" -v
  & 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --python tools/modeling/generate_stage2_assets.py -- --mode final --output assets/models
  ```

  Expected: 合同校验与清单行为测试全部 PASS；五个正式 GLB 写入 `assets/models/`，生成器退出码 0。材质是否达到批准的暖金属／旧钢／亮银视觉目标由 Task 6 的实际 Forward+ 截图决定，不由常量断言代替。

- [ ] **Step 4: 创建手写包装场景**

  每个 `.tscn` 使用阶段 1 格式：根节点名 `AssetVisual`，metadata `asset_id` 精确匹配，唯一 `Model` 子节点实例化对应 GLB。为保持原碰撞接触面，`Model.position.y` 分别为：过滤器 `-0.16`、豆铲 `-0.05`、冰夹 `-0.04`、小量酒器 `-0.075`、大量酒器 `-0.105`。

- [ ] **Step 5: 用独立审核清单验证正式 GLB并保留正式灰盒状态**

  用位于 `artifacts/stage2_final_candidate/review_manifest.json` 的五项正式审核清单运行验证；正式 `assets/asset_manifest.json` 和 `data/assets/asset_manifest.tres` 不改，阶段 2 五项继续保持占位。

  Run: `python tools/validate_assets.py artifacts/stage2_final_candidate/review_manifest.json`

  Expected: 阶段 2 五项全部 `OK`，`SUMMARY assets=5 errors=0`；正式清单仍显示阶段 1 四项 `OK`、其余 12 项允许的 graybox。

- [ ] **Step 6: 提交正式资产与包装**

  显式暂存生成器、合同测试、五个 GLB和五个包装；不得暂存正式清单或 `artifacts/`。

  Commit message: `feat: deliver stage two handheld asset models`

### Task 5: 以失败集成测试接入世界、双手和全量验证

**Files:**
- Create: `tests/godot/Stage2AssetIntegrationTests.cs`
- Create: `tests/godot/Stage2AssetIntegrationTests.cs.uid`（Godot 自动生成）
- Create: `tests/godot/Stage2AssetIntegrationTests.tscn`
- Create: `assets/models/traditional_filter.glb.import`（Godot 自动生成）
- Create: `assets/models/bean_scoop.glb.import`（Godot 自动生成）
- Create: `assets/models/ice_tongs.glb.import`（Godot 自动生成）
- Create: `assets/models/jigger_small.glb.import`（Godot 自动生成）
- Create: `assets/models/jigger_large.glb.import`（Godot 自动生成）
- Modify: `scripts/assets/ToolVisualLibrary.cs`
- Modify: `tools/run_verification.ps1`

**Interfaces:**
- Consumes: `ToolVisualLibrary.Instantiate(string toolId)`、`FindAnchor()`、`ApplyHeldPose()`、`ApplyWorldStyle()`。
- Produces: 五项包装世界实例、手持节点 `HeldAssetVisual`、双世界覆盖及灰盒回退断言；验证输出 `STAGE2_ASSET_INTEGRATION_PASS`。

- [ ] **Step 1: 写失败的阶段 2 集成测试**

  `Stage2AssetIntegrationTests.cs` 复用阶段 1 的断言结构，定义：

  ```csharp
  private static readonly Dictionary<string, string[]> ExpectedAnchors = new(StringComparer.Ordinal)
  {
      ["traditional_filter"] = ["Grip", "Placement", "Spout", "Interaction"],
      ["bean_scoop"] = ["Grip", "Placement", "FillOrigin"],
      ["ice_tongs"] = ["Grip", "Placement", "Interaction"],
      ["jigger_small"] = ["Grip", "Placement", "FillOrigin", "Spout"],
      ["jigger_large"] = ["Grip", "Placement", "FillOrigin", "Spout"]
  };
  ```

  测试逐项断言 `AssetVisual`、稳定 metadata、必需锚点、原灰盒碰撞仍存在且 Mesh 隐藏；切换眼镜世界时所有导入 Mesh 获得覆盖材质，返回现实世界后恢复；左手拿过滤器、右手依次拿豆铲／冰夹／小量酒器／大量酒器时出现匹配的 `HeldAssetVisual`，权威手部 ID 不变，重置后隐藏。

- [ ] **Step 2: 运行测试确认失败**

  Run:

  ```powershell
  dotnet build GlassesBar.csproj --configuration Debug --nologo
  & 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/Stage2AssetIntegrationTests.tscn
  ```

  Expected: FAIL，因为 `ToolVisualLibrary` 尚未映射阶段 2 包装。

- [ ] **Step 3: 实现最小包装映射与手持姿态**

  在 `WrapperPaths` 增加五项 `res://scenes/assets/stage2/<id>.tscn`。在 `ApplyHeldPose()` 增加审图起点：过滤器 scale `0.72`、豆铲 `0.82`、冰夹 `0.78`、小量酒器 `0.94`、大量酒器 `0.82`；过滤器绕 Z 轴 `-8°`，豆铲 `8°`，冰夹 `-6°`，小／大量酒器沿用中号 `7°`。这些值只属于可替换表现校正，不写入玩法状态。

- [ ] **Step 4: Godot 导入并运行阶段 2 集成测试**

  Run:

  ```powershell
  & 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --editor --quit
  dotnet build GlassesBar.csproj --configuration Debug --nologo
  & 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/Stage2AssetIntegrationTests.tscn
  ```

  Expected: `STAGE2_ASSET_INTEGRATION_PASS`，退出码 0。

- [ ] **Step 5: 加入全量验证并提交**

  在 `tools/run_verification.ps1` 的阶段 1 测试之后运行阶段 2 场景并检查退出码。

  Commit message: `feat: integrate stage two asset wrappers`

### Task 6: 运行实际 Godot 检查点 2 并硬停等待用户

**Files:**
- Create: `tests/godot/Stage2AssetVisualCapture.cs`
- Create: `tests/godot/Stage2AssetVisualCapture.cs.uid`（Godot 自动生成）
- Create: `tests/godot/Stage2AssetVisualCapture.tscn`
- Modify: `docs/assets/STAGE2_ASSET_BATCH_20260802.md`
- Output ignored: `artifacts/visual_review_20260802_stage2_godot/`

**Interfaces:**
- Consumes: 五项世界／手持包装、现有玩家相机、`GameSession.ToggleWorld()` 和 `DrinkWorkstation` 手部状态。
- Produces: 实际 Forward+ 世界队列、左手过滤器、右手豆铲／冰夹／量酒器、过滤器＋高球杯、冰夹＋冰桶及必要眼镜世界 PNG。

- [ ] **Step 1: 创建确定性视觉捕获场景**

  `Stage2AssetVisualCapture.cs` 按固定帧序列：开始游戏 → 世界队列 → 现实／眼镜切换 → 左手过滤器＋右手豆铲 → 重置 → 左手过滤器＋右手冰夹 → 重置 → 高球杯＋小量酒器 → 重置 → 高球杯＋大量酒器 → 退出。核心帧表固定为：

  ```csharp
  switch (_frame)
  {
      case 24: GameSession.Instance.ToggleWorld(); break;
      case 44: GameSession.Instance.ToggleWorld(); Hold("traditional_filter", "bean_scoop"); break;
      case 72: ResetHands(); Hold("traditional_filter", "ice_tongs"); break;
      case 100: ResetHands(); Hold("highball_glass", "jigger_small"); break;
      case 128: ResetHands(); Hold("highball_glass", "jigger_large"); break;
      case 156: GetTree().Quit(0); break;
  }
  ```

  `Hold(leftId, rightId)` 只调用对应 `ToolInteractable.Interact(_context)`；`ResetHands()` 只调用现有 `DrinkWorkstation.ResetForNewDay()` 与 `PlayerController.ResetForNewDay()`。

- [ ] **Step 2: 运行非 headless Forward+ 捕获**

  Run:

  ```powershell
  New-Item -ItemType Directory -Force -Path 'artifacts/visual_review_20260802_stage2_godot' | Out-Null
  & 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --fixed-fps 30 --write-movie 'artifacts/visual_review_20260802_stage2_godot/stage2.png' res://tests/godot/Stage2AssetVisualCapture.tscn
  ```

  Expected: 帧序列 `stage200000000.png` 起写入目标目录，场景在第 156 帧退出，进程退出码 0。

- [ ] **Step 3: 实际检查关键画面并只修表现层**

  逐张检查落台、遮挡、过滤出口、高球杯组合、豆铲装载区、冰夹夹口和冰桶净空、三种金属高光及眼镜覆盖。返修范围只允许：生成器几何／PBR 参数、包装 `Model` 偏移、`ToolVisualLibrary.ApplyHeldPose()`；每轮返修后重新生成、导入、运行资产验证和阶段 2 集成测试。

- [ ] **Step 4: 更新批次记录**

  写入每轮截图目录、返修理由、验证结果，并明确标记“检查点 2 等待用户批准”。

- [ ] **Step 5: 提交视觉捕获工具与记录**

  Commit message: `chore: hold stage two Godot integration for review`

- [ ] **Step 6: 检查点 2——向用户展示并停止**

  展示世界、手持和组合关键 PNG；必须等待用户明确回复“通过”或提出返修意见。不得继续 Task 7，也不得把阶段 2 标记完成。

### Task 7: 全量验证、状态归档与阶段完成提交

**Files:**
- Modify: `docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `docs/assets/STAGE2_ASSET_BATCH_20260802.md`
- Modify: `progress.md`
- Modify: `assets/asset_manifest.json`
- Modify: `data/assets/asset_manifest.tres`

**Interfaces:**
- Consumes: 用户对两个视觉检查点的明确批准与 Tasks 1–6 的验证证据。
- Produces: 阶段 2 五项“已验证技术候选”状态、剩余 7 项灰盒状态、验证命令／结果和下一阶段边界。

- [ ] **Step 1: 在检查点 2 批准后切换正式清单**

  只把 `traditional_filter`、`bean_scoop`、`ice_tongs`、`jigger_small`、`jigger_large` 的 JSON `placeholder` 和 TRES `IsPlaceholder` 改为 `false`；阶段 3 的 7 项继续为 `true`。

- [ ] **Step 2: 运行全量验证**

  Run: `powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1`

  Expected: 资产 16 项 0 错误；阶段 1 四项和阶段 2 五项 GLB `OK`；其余 7 项为允许的 graybox；领域测试全部通过；Debug/Release 0 错误；Godot 导入、`SMOKE_TESTS_PASS`、`STAGE1_ASSET_INTEGRATION_PASS`、`STAGE2_ASSET_INTEGRATION_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS` 全部出现。

- [ ] **Step 3: 检查工作树范围**

  Run: `git status --short` 和 `git diff --check`

  Expected: 只包含阶段 2 资产、生成／审核工具、包装、测试和文档；不包含 `artifacts/`、`.blend`、未批准的阶段 3 文件或无关用户改动。

- [ ] **Step 4: 更新项目状态与交接**

  明确区分“阶段 2 已批准的 M1 技术／材质样件”“其余 7 项仍为灰盒”“最终美术、正式容量和阶段 3 未开始”。`progress.md` 必须包含已完成事项、关键决策和未完成待办；`docs/CONTEXT_HANDOFF.md` 的 P0/P1 必须指向批次记录和下一安全动作。

- [ ] **Step 5: 提交阶段完成清单与文档**

  显式暂存两份清单和七份状态／批次文档并提交。

  Commit message: `feat: complete stage two handheld asset batch`

- [ ] **Step 6: 报告完成但不推送或合并**

  向用户提供五项资产、两次批准、全量验证、最终截图路径和提交哈希；不进入阶段 3，不推送、不合并。
