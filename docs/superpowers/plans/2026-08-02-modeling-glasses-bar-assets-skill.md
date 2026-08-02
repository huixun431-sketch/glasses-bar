# Reusable Glasses Bar Asset Modeling Skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create and verify the repository-local `modeling-glasses-bar-assets` Skill and its complete batch-scaffolding/phase-validation framework without starting Stage 3.

**Architecture:** Keep `SKILL.md` as a short orchestrator that requires `develop-glasses-bar-godot`, routes detailed guidance to three references, and invokes two standard-library Python tools. `init_asset_batch.py` atomically generates consistent per-batch contract, Blender, test, Godot-capture, and record skeletons from an approved JSON configuration; `validate_asset_batch.py` enforces checkpoint, manifest, evidence, Git-tracking, and archival invariants. Tests run in temporary projects, while fresh-context subagents provide RED/GREEN evidence that the Skill changes real workflow behavior.

**Tech Stack:** Python 3 standard library and `unittest`, Codex Agent Skills format, Markdown templates, Blender 4.5.5 LTS interface conventions, Godot 4.7.1 .NET/C# scene/test conventions, Git, PowerShell.

## Global Constraints

- This implementation creates only the Skill, its framework, tests, specifications, and project status records; Stage 3 assets, contracts, GLBs, wrappers, manifests, and screenshots remain untouched.
- The Skill is repository-local at `.agents/skills/modeling-glasses-bar-assets/` and requires `develop-glasses-bar-godot` for project/gameplay contracts.
- Use RED/GREEN evidence for both Python production scripts and the Skill behavior itself. Do not write or retain production Skill files before the baseline agent scenarios are recorded.
- Generated skeletons never invent final silhouettes, material values, capacities, recipes, balance, or customer content.
- Formal manifest flags cannot change before an explicit Forward+ checkpoint-2 approval.
- Candidate GLBs and screenshots remain ignored artifacts; `.blend`, screenshots, artifacts, and manual `.glb.import` edits are never tracked.
- Do not push, merge, publish, rewrite history, or start Stage 3.

---

### Task 1: Record RED baseline behavior before the Skill exists

**Files:**
- Create: `tests/skills/modeling-glasses-bar-assets/scenarios/checkpoint-pressure.md`
- Create: `tests/skills/modeling-glasses-bar-assets/scenarios/material-shortcut.md`
- Create: `tests/skills/modeling-glasses-bar-assets/scenarios/completion-pressure.md`
- Create: `docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md`

**Interfaces:**
- Consumes: raw Stage 1/2 batch records and the design spec, but not the new Skill.
- Produces: three reusable scenario prompts and a baseline report containing verbatim decisions, omissions, and rationalizations that Task 5 must address.

- [ ] **Step 1: Write three pressure scenarios**

  Each prompt must begin with `IMPORTANT: this is a real task; choose and act.` and force a concrete response. Cover:

  1. A five-asset batch under deadline pressure where the formal path is tempting before silhouette approval.
  2. A material fix where PBR constants and headless tests pass but launching Forward+ seems expensive.
  3. A completion request with sunk cost and a dirty worktree where manifest switching, ignored screenshots, handoff, and `progress.md` can be skipped.

  Do not mention the intended correct answer or the future Skill.

- [ ] **Step 2: Run scenarios without the new Skill**

  Dispatch one fresh-context agent per scenario with only the scenario file and a small temporary project fixture. The agents must not receive the design spec, expected failures, or conclusions.

  Expected RED evidence: at least one scenario omits or weakens a checkpoint, evidence requirement, graybox gate, or archival step. If all three agents comply, record that result honestly and classify the Skill primarily as a technique/framework Skill rather than inventing a discipline failure.

- [ ] **Step 3: Record baseline results verbatim**

  `docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md` must include:

  ```markdown
  ## RED baseline
  | Scenario | Decision | Missing/unsafe behavior | Verbatim rationale |
  |---|---|---|---|
  ```

  Keep raw agent transcripts in ignored scratch storage and link their paths; do not paste lengthy transcripts into the repository.

- [ ] **Step 4: Verify Skill production files still do not exist**

  Run:

  ```powershell
  Test-Path '.agents/skills/modeling-glasses-bar-assets'
  ```

  Expected: `False`.

- [ ] **Step 5: Commit the RED fixtures and evidence**

  ```powershell
  git add -- tests/skills/modeling-glasses-bar-assets docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md
  git commit -m "test: record asset modeling skill baseline"
  ```

---

### Task 2: Initialize the Skill and lock the framework interfaces with failing tests

**Files:**
- Create: `.agents/skills/modeling-glasses-bar-assets/SKILL.md` (generated template, then replaced in Task 5)
- Create: `.agents/skills/modeling-glasses-bar-assets/agents/openai.yaml`
- Create: `.agents/skills/modeling-glasses-bar-assets/scripts/`
- Create: `.agents/skills/modeling-glasses-bar-assets/references/`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/`
- Create: `tests/tools/test_modeling_skill_framework.py`

**Interfaces:**
- Produces: CLI contracts `init_asset_batch.py --config PATH --project-root PATH [--dry-run]` and `validate_asset_batch.py --config PATH --project-root PATH --phase PHASE`.
- Consumes: a JSON batch configuration with `batch_id`, `stage`, `assets`, `paths`, and `checkpoints`.

- [ ] **Step 1: Initialize the Skill directory with the official helper**

  Run:

  ```powershell
  python 'C:\Users\lenovo\.codex\skills\.system\skill-creator\scripts\init_skill.py' modeling-glasses-bar-assets `
    --path '.agents/skills' `
    --resources scripts,references,assets `
    --interface 'display_name=Model Glasses Bar Assets' `
    --interface 'short_description=Run guarded Blender-to-Godot asset batches' `
    --interface 'default_prompt=Use $modeling-glasses-bar-assets to scaffold and execute a reviewed asset batch in the Glasses Bar repository.'
  ```

  Expected: the required Skill directory and `agents/openai.yaml` exist; no example placeholder files are requested.

- [ ] **Step 2: Write a failing framework test**

  In `tests/tools/test_modeling_skill_framework.py`, use `tempfile.TemporaryDirectory` and define a valid two-asset configuration:

  ```python
  VALID_CONFIG = {
      "batch_id": "test-fixed-stations",
      "stage": "test",
      "assets": [
          {
              "asset_id": "test_kettle",
              "runtime_id": "kettle",
              "required_anchors": ["Placement", "Spout", "Interaction"],
              "interaction_kind": "fixed_station",
          },
          {
              "asset_id": "test_bin",
              "runtime_id": "waste_bin",
              "required_anchors": ["Placement", "Interaction"],
              "interaction_kind": "fixed_station",
          },
      ],
      "paths": {
          "candidate_root": "artifacts/test-fixed-stations",
          "formal_model_root": "assets/models",
          "wrapper_root": "scenes/assets/test-fixed-stations",
          "batch_record": "docs/assets/TEST_FIXED_STATIONS_ASSET_BATCH.md",
          "json_manifest": "assets/asset_manifest.json",
      },
      "checkpoints": {
          "silhouette": {"status": "pending", "evidence": []},
          "forward_plus": {"status": "pending", "evidence": []},
      },
  }
  ```

  Assert that importing both scripts succeeds and that `validate_config(VALID_CONFIG)` returns no errors. Since scripts do not exist yet, the test must fail with `FileNotFoundError` or `ModuleNotFoundError`.

- [ ] **Step 3: Run the test and verify RED**

  Run:

  ```powershell
  python -m unittest discover -s tests/tools -p 'test_modeling_skill_framework.py' -v
  ```

  Expected: FAIL because framework scripts are absent, not because the fixture is malformed.

- [ ] **Step 4: Commit the initialized shell and failing test**

  ```powershell
  git add -- .agents/skills/modeling-glasses-bar-assets tests/tools/test_modeling_skill_framework.py
  git commit -m "test: define asset batch framework interfaces"
  ```

---

### Task 3: Implement atomic batch scaffolding and reusable templates

**Files:**
- Create: `.agents/skills/modeling-glasses-bar-assets/scripts/init_asset_batch.py`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/asset_contract.py.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/blender_generator.py.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/blender_review_renderer.py.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/contract_test.py.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/godot_integration_test.cs.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/godot_visual_capture.cs.tmpl`
- Create: `.agents/skills/modeling-glasses-bar-assets/assets/templates/asset_batch_record.md.tmpl`
- Modify: `tests/tools/test_modeling_skill_framework.py`

**Interfaces:**
- Produces: `validate_config(config: Mapping[str, object]) -> list[str]`, `planned_outputs(config, project_root) -> list[Path]`, `render_outputs(config, template_root) -> dict[Path, str]`, and CLI `main() -> int`.
- Generated Python files: `tools/modeling/<batch_slug>_asset_contract.py`, `generate_<batch_slug>_assets.py`, `render_<batch_slug>_review.py`, `tests/tools/test_<batch_slug>_asset_contract.py`.
- Generated Godot files: `tests/godot/<PascalBatch>AssetIntegrationTests.cs/.tscn` and `tests/godot/<PascalBatch>AssetVisualCapture.cs/.tscn`.
- Generated record: configured `paths.batch_record`.

- [ ] **Step 1: Extend tests for configuration validation and atomic writes**

  Add tests that assert:

  - valid configuration yields no errors and a deterministic output list;
  - duplicate asset IDs, missing anchors, invalid slugs, absolute/out-of-root paths, and unsupported checkpoint states are all reported together;
  - invalid configuration writes nothing;
  - an existing destination rejects the entire operation without partial writes;
  - `--dry-run` prints every output but writes nothing;
  - a valid run creates Python/C#/TSCN/Markdown skeletons with no unreplaced `${TOKEN}` markers.

- [ ] **Step 2: Run tests and verify RED**

  Expected: new tests fail because only the interfaces exist.

- [ ] **Step 3: Implement the minimal initializer**

  Use only standard-library `argparse`, `json`, `pathlib`, `re`, `string.Template`, and temporary staging. Validate all inputs and all target conflicts before writing. Render into a temporary sibling directory, then move each file into the project only after every template renders successfully.

  Templates must include explicit `RAISE_UNTIL_DESIGN_APPROVED` or failing-test markers only in generated skeletons, not in the Skill itself. They must never contain invented art values. The generator exposes both required CLI modes and refuses `final` while checkpoint 1 remains pending.

- [ ] **Step 4: Run tests and verify GREEN**

  Run:

  ```powershell
  python -m unittest discover -s tests/tools -p 'test_modeling_skill_framework.py' -v
  python -m py_compile .agents/skills/modeling-glasses-bar-assets/scripts/init_asset_batch.py
  ```

  Expected: all initializer tests PASS and compilation exits `0`.

- [ ] **Step 5: Commit the initializer and templates**

  ```powershell
  git add -- .agents/skills/modeling-glasses-bar-assets/scripts/init_asset_batch.py `
    .agents/skills/modeling-glasses-bar-assets/assets/templates `
    tests/tools/test_modeling_skill_framework.py
  git commit -m "feat: scaffold guarded asset modeling batches"
  ```

---

### Task 4: Implement checkpoint and manifest phase validation

**Files:**
- Create: `.agents/skills/modeling-glasses-bar-assets/scripts/validate_asset_batch.py`
- Modify: `tests/tools/test_modeling_skill_framework.py`

**Interfaces:**
- Produces: `validate_batch(config, project_root, phase, tracked_files=None) -> list[str]` and CLI phases `design`, `silhouette-review`, `formal-candidate`, `forward-plus-review`, `complete`.
- Consumes: generated files, configured candidate/formal roots, batch record, checkpoint evidence, JSON manifest, `git ls-files`, and current phase.

- [ ] **Step 1: Write failing phase-gate tests**

  Build a temporary Git repository and assert:

  - `design` requires only validated configuration;
  - `silhouette-review` requires candidate evidence under ignored `artifacts/` and rejects formal GLBs/wrappers;
  - `formal-candidate` requires checkpoint 1 `approved`, permits formal files, and requires listed manifest assets to remain `placeholder=true`;
  - `forward-plus-review` requires real PNG evidence plus successful integration evidence and still requires placeholders;
  - `complete` requires checkpoint 2 `approved`, manifest `placeholder=false` only for batch IDs, batch record completion sections, and verification summary;
  - any tracked `artifacts/`, PNG, `.blend`, or manually selected `.glb.import` file is rejected;
  - errors are deterministic and accumulated instead of failing at the first problem.

- [ ] **Step 2: Run tests and verify RED**

  Expected: phase tests fail because `validate_asset_batch.py` is missing.

- [ ] **Step 3: Implement minimal deterministic validation**

  Normalize all paths against `project_root`; reject escape. Read JSON with clear parse errors. Use `git -C <root> ls-files` when `tracked_files` is not supplied. Treat missing Blender/Godot execution evidence as an incomplete phase, never as success.

  CLI output shape:

  ```text
  ERROR <stable-code>: <message>
  ...
  SUMMARY batch=<id> phase=<phase> errors=<n>
  ```

  Return `0` only when `errors=0`.

- [ ] **Step 4: Run tests and verify GREEN**

  Run:

  ```powershell
  python -m unittest discover -s tests/tools -p 'test_modeling_skill_framework.py' -v
  python -m py_compile .agents/skills/modeling-glasses-bar-assets/scripts/validate_asset_batch.py
  ```

- [ ] **Step 5: Commit the validator**

  ```powershell
  git add -- .agents/skills/modeling-glasses-bar-assets/scripts/validate_asset_batch.py tests/tools/test_modeling_skill_framework.py
  git commit -m "test: enforce asset batch review gates"
  ```

---

### Task 5: Write the minimal Skill and routed references from RED evidence

**Files:**
- Modify: `.agents/skills/modeling-glasses-bar-assets/SKILL.md`
- Modify: `.agents/skills/modeling-glasses-bar-assets/agents/openai.yaml`
- Create: `.agents/skills/modeling-glasses-bar-assets/references/workflow.md`
- Create: `.agents/skills/modeling-glasses-bar-assets/references/framework-contract.md`
- Create: `.agents/skills/modeling-glasses-bar-assets/references/review-checkpoints.md`
- Modify: `docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md`

**Interfaces:**
- Skill trigger: asset-batch planning, Blender/GLB generation, wrapper integration, visual review, manifest switching, or batch milestone reporting in this repository.
- Required sub-skill: `develop-glasses-bar-godot`.
- Routes: workflow state machine, CLI/config contract, visual/evidence checklists.

- [ ] **Step 1: Convert baseline failures into exact guidance**

  Map every observed RED omission/rationalization to one explicit instruction, structural checklist slot, or validator command. Do not invent a rationalization table if baseline agents did not rationalize; use positive output contracts for missing/incorrect output shapes.

- [ ] **Step 2: Write concise `SKILL.md`**

  Frontmatter must contain only:

  ```yaml
  ---
  name: modeling-glasses-bar-assets
  description: Use when planning, generating, reviewing, integrating, or completing a Blender-to-GLB asset batch in the Glasses Bar Godot repository.
  ---
  ```

  The body must:

  - require `develop-glasses-bar-godot`;
  - read `docs/CONTEXT_HANDOFF.md`, the modeling plan, current batch config, and the asset-handoff reference;
  - route to exactly one relevant reference per phase;
  - run the framework validator before and after each phase;
  - hard-stop for user approval at both checkpoints;
  - distinguish tracked source/records from ignored candidates/screenshots;
  - require actual screenshot inspection and complete project verification before completion;
  - avoid duplicating detailed schemas from references.

- [ ] **Step 3: Write the three references**

  - `workflow.md`: phase inputs, actions, outputs, and stop conditions.
  - `framework-contract.md`: JSON schema, generated paths, CLI examples, atomic/overwrite behavior, stable error output.
  - `review-checkpoints.md`: Blender silhouette checklist, Forward+ material/pose/context checklist, evidence record shape, revision loop.

  Each file is directly linked from `SKILL.md`; no reference links to another reference.

- [ ] **Step 4: Regenerate UI metadata**

  Run:

  ```powershell
  python 'C:\Users\lenovo\.codex\skills\.system\skill-creator\scripts\generate_openai_yaml.py' `
    '.agents/skills/modeling-glasses-bar-assets' `
    --interface 'display_name=Model Glasses Bar Assets' `
    --interface 'short_description=Run guarded Blender-to-Godot asset batches' `
    --interface 'default_prompt=Use $modeling-glasses-bar-assets to scaffold and execute a reviewed asset batch in the Glasses Bar repository.'
  ```

- [ ] **Step 5: Run static Skill validation**

  Run:

  ```powershell
  python 'C:\Users\lenovo\.codex\skills\.system\skill-creator\scripts\quick_validate.py' '.agents/skills/modeling-glasses-bar-assets'
  rg -n 'TBD|TODO|placeholder example|@skills' '.agents/skills/modeling-glasses-bar-assets'
  ```

  Expected: validator PASS; search has no unresolved authoring placeholders or force-load references. Template substitution tokens are allowed only under `assets/templates/` and must be covered by initializer tests.

- [ ] **Step 6: Commit the Skill guidance**

  ```powershell
  git add -- .agents/skills/modeling-glasses-bar-assets docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md
  git commit -m "feat: add reusable asset modeling skill"
  ```

---

### Task 6: Verify GREEN behavior with fresh agents and close real gaps

**Files:**
- Modify as needed: `.agents/skills/modeling-glasses-bar-assets/**`
- Modify: `docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md`

**Interfaces:**
- Consumes: the three Task 1 scenarios plus one variation with different asset IDs and a temporary project.
- Produces: GREEN evidence that agents use the initializer/validator, preserve phase gates, inspect real evidence, and stop for user approval.

- [ ] **Step 1: Run the original scenarios with the Skill**

  Dispatch fresh-context agents using this shape:

  ```text
  Use $modeling-glasses-bar-assets at <absolute-skill-path> to handle the task in <temporary-project-path>.
  <scenario text>
  ```

  Do not provide expected answers, known baseline failures, or intended fixes.

- [ ] **Step 2: Run a variation scenario**

  Use a new three-asset fixed-station batch with different IDs, one invalid anchor, and pressure to reuse old screenshot evidence. Success requires the agent to reject invalid input and stale evidence before generating formal outputs.

- [ ] **Step 3: Compare RED and GREEN behavior**

  Append:

  ```markdown
  ## GREEN forward tests
  | Scenario | Skill actions | Gate behavior | Result |
  |---|---|---|---|
  ```

  Every original RED failure must be resolved. If a new rationalization or framework gap appears, change only the smallest relevant Skill/script/reference, add a regression test, and re-run the affected scenario.

- [ ] **Step 4: Run final Skill and framework validation**

  Run:

  ```powershell
  python -m unittest discover -s tests/tools -p 'test_modeling_skill_framework.py' -v
  python 'C:\Users\lenovo\.codex\skills\.system\skill-creator\scripts\quick_validate.py' '.agents/skills/modeling-glasses-bar-assets'
  ```

- [ ] **Step 5: Commit verified refinements**

  ```powershell
  git add -- .agents/skills/modeling-glasses-bar-assets tests/tools/test_modeling_skill_framework.py docs/skills/MODELING_GLASSES_BAR_ASSETS_SKILL_TESTS.md
  git commit -m "test: verify asset modeling skill workflow"
  ```

---

### Task 7: Run project regression and archive the reusable Skill milestone

**Files:**
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `progress.md`

**Interfaces:**
- Consumes: verified Skill/framework commits and RED/GREEN evidence.
- Produces: current project status stating the Skill is available, Stage 3 remains paused, and the next safe action is to design a new batch with user authorization.

- [ ] **Step 1: Run the complete project verification**

  Run:

  ```powershell
  powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
  ```

  Expected: assets `16`, errors `0`; formal/graybox states unchanged at `9/7`; domain `28/28`; Debug/Release 0 warnings/errors; smoke, Stage 1, Stage 2, input, and flow PASS.

- [ ] **Step 2: Inspect scope and generated Skill quality**

  Run:

  ```powershell
  git status --short
  git diff --check
  git ls-files artifacts '*.png' '*.blend'
  python 'C:\Users\lenovo\.codex\skills\.system\skill-creator\scripts\quick_validate.py' '.agents/skills/modeling-glasses-bar-assets'
  ```

  Expected: no tracked review artifacts, images, or `.blend`; Skill validation PASS.

- [ ] **Step 3: Update project status and handoff**

  Record:

  - Skill name, path, commands, RED/GREEN evidence, and verification;
  - no Stage 3 asset/config/model/wrapper/manifest work was started;
  - the next action requires a new Stage 3 design and explicit user approval;
  - root `progress.md` contains completed items, key decisions, and remaining TODOs.

- [ ] **Step 4: Commit milestone records**

  ```powershell
  git add -- docs/PROJECT_STATUS.md docs/ROADMAP.md docs/CHANGELOG.md docs/CONTEXT_HANDOFF.md progress.md
  git commit -m "docs: record reusable modeling skill milestone"
  ```

- [ ] **Step 5: Report completion without starting Stage 3**

  Provide Skill path, script/test commands, RED/GREEN outcome, full project verification, latest commits, and the fact that Stage 3 remains paused. Do not push or merge.
