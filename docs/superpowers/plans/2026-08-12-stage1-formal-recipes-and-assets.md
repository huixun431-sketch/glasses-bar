# 第一阶段正式配方与物品目录 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 9 杯正式配方、26 个成分形态、三种正式量酒器容量和去重后的资产需求接入仓库，同时只把已有冰美式标为部分可玩。

**Architecture:** 在现有 Godot Resource 定义层新增原料与配方目录，并将正式投料要求与当前成品评价步骤分开；运行时仍只加载冰美式资源。建模范围写入现有核心计划，但不提前创建 GLB、包装场景或修改 manifest 占位状态。

**Tech Stack:** Godot 4.7.1 .NET、C#、Godot Resource `.tres`、NUnit、PowerShell/Python 资产验证。

## Global Constraints

- 九杯配方正文是唯一正式用量来源；不自行换算“滴”“勺”“少量”或未注明数量。
- 26 个成分形态归并为 23 个采购来源族；资产、成分和采购来源不可混用同一统计口径。
- `jigger_small=10/20 ml`、`jigger_medium=15/30 ml`、`jigger_large=25/50 ml`。
- 冰美式状态为 `Partial`，其余 8 杯为 `CatalogOnly`；不得表述为全部可玩。
- 不生成模型、不截图、不切换任何新增或灰盒资产的 `placeholder` 状态。
- 保留并排除用户本地 `export_presets.cfg`。

---

### Task 1: 正式内容 Resource 类型与校验器

**Files:**
- Modify: `scripts/core/GameEnums.cs`
- Modify: `scripts/data/IngredientDefinition.cs`
- Modify: `scripts/data/RecipeDefinition.cs`
- Create: `scripts/data/IngredientCatalogDefinition.cs`
- Create: `scripts/data/RecipeCatalogDefinition.cs`
- Create: `scripts/data/RecipeIngredientRequirement.cs`
- Test: `tests/godot/FormalContentCatalogTests.cs`

**Interfaces:**
- Produces: `RecipeImplementationStatus`, `IngredientCatalogDefinition.BuildValidatedIndex()`, `RecipeCatalogDefinition.BuildValidatedIndex(ingredientIds)` and formal ingredient requirement fields.

- [x] Write a Godot test that rejects duplicate IDs, unknown ingredient references, invalid ranges and prototype entries in the formal catalog.
- [x] Run the focused Godot test and confirm failure because the new definitions do not exist.
- [x] Add `Drop` and `Spoon` units, implementation status, catalogs, requirements and deterministic validation exceptions.
- [x] Run the focused Godot test and confirm PASS.

### Task 2: 九杯正式数据、冰美式加载与存档兼容

**Files:**
- Create: `data/ingredients/stage1_ingredients.tres`
- Delete: `data/ingredients/prototype_ingredients.tres`
- Create: `data/recipes/iced_americano.tres`
- Create: `data/recipes/gin_and_tonic.tres`
- Create: `data/recipes/old_fashioned.tres`
- Create: `data/recipes/mojito.tres`
- Create: `data/recipes/margarita.tres`
- Create: `data/recipes/moscow_mule.tres`
- Create: `data/recipes/daiquiri.tres`
- Create: `data/recipes/martini.tres`
- Create: `data/recipes/whiskey_sour.tres`
- Create: `data/recipes/stage1_recipe_catalog.tres`
- Delete: `data/recipes/prototype_iced_americano.tres`
- Modify: `scripts/gameplay/DrinkWorkstation.cs`
- Modify: `src/Domain/SaveGameSnapshot.cs`
- Modify: `scripts/core/GameSession.cs`
- Test: `tests/godot/FormalContentCatalogTests.cs`
- Test: `tests/DomainTests.cs`

**Interfaces:**
- Consumes: Task 1 catalog validators.
- Produces: stable recipe IDs and a legacy ID normalization path from `prototype_iced_americano` to `iced_americano`.

- [x] Add tests asserting exactly 9 recipes, 26 ingredient IDs, the approved quantity/unit data, implementation statuses, and legacy ID acceptance.
- [x] Run the focused tests and confirm the old placeholder resources fail them.
- [x] Author all formal `.tres` data and update the runtime path/ID normalization.
- [x] Run focused domain and Godot catalog tests and confirm PASS.

### Task 3: 正式量酒器容量

**Files:**
- Modify: `data/gameplay/prototype_gameplay_catalog.tres`
- Modify: `scripts/gameplay/DrinkWorkstation.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`
- Modify: `tests/godot/Stage2AssetIntegrationTests.cs`

**Interfaces:**
- Produces: small/large values `10/20`, `15/30`, `25/50` through existing `ToolSpec` properties.

- [x] Add assertions for all six capacity values and update water-transfer expectations to the selected 10/20/15/30/25/50 measures.
- [x] Run focused tests and confirm they fail against `15/30`, `20/40`, `25/50`.
- [x] Replace catalog values and remove “开发占位容量” UI text while preserving measure-side switching.
- [x] Run focused tests and confirm PASS.

### Task 4: 建模计划、状态与交付审计

**Files:**
- Modify: `docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `progress.md`

**Interfaces:**
- Consumes: approved formal catalog and existing asset manifest states.
- Produces: deduplicated asset-family table, reuse/graybox/new status, animation dependency matrix and current handoff state.

- [x] Add the 9-recipe source-of-truth table and deduplicated modeling inventory to the core modeling plan.
- [x] Record existing approved assets, existing grayboxes, new assets and retirement targets for coffee placeholder tools without changing manifest state.
- [x] Add the seven animation requirements and their asset/system dependencies.
- [x] Update current status documents and `progress.md` with completed work, decisions and remaining implementation/modeling tasks.
- [x] Run catalog tests, domain tests, relevant Godot integration tests, Debug/Release builds, asset validation and `git diff --check`.
- [x] Stage only intended files, confirm `export_presets.cfg` is excluded, and commit the implementation.
