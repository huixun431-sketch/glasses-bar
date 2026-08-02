---
name: modeling-glasses-bar-assets
description: Use when planning, generating, reviewing, integrating, or completing a Blender-to-GLB asset batch in the Glasses Bar Godot repository.
---

# Model Glasses Bar Assets

Use this repository-local framework to keep an asset batch inside the project's approval, evidence, and fallback boundaries.

**REQUIRED SUB-SKILL:** Use `develop-glasses-bar-godot` for the project gameplay and asset-handoff contract.

## Establish context

Before changing a batch, read:

1. `docs/CONTEXT_HANDOFF.md`.
2. `docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md` and the approved design for the current batch.
3. The current batch JSON configuration.
4. `.agents/skills/develop-glasses-bar-godot/references/asset-handoff.md`.

Do not infer approval from an earlier batch. Do not invent silhouettes, materials, dimensions, capacities, recipes, balance, gameplay values, customer content, or final art.

## Route the current phase

Read exactly the reference for the phase being performed:

| Current phase | Read this reference |
|---|---|
| `design` | [framework-contract.md](references/framework-contract.md) |
| `silhouette-review` | [review-checkpoints.md](references/review-checkpoints.md) |
| `formal-candidate` | [workflow.md](references/workflow.md) |
| `forward-plus-review` | [review-checkpoints.md](references/review-checkpoints.md) |
| `complete` | [workflow.md](references/workflow.md) |

Run `scripts/validate_asset_batch.py` before and after every phase. An error means the phase is incomplete; never reinterpret missing Blender, Godot, screenshot, approval, or project-verification evidence as success.

## Non-negotiable gates

- Stop for explicit user approval after the neutral Blender silhouette review. Before that approval, candidates and screenshots stay under ignored `artifacts/`; do not create formal GLBs, hand-authored wrappers, or manifest switches.
- After checkpoint 1, formal GLBs may be visual children of hand-authored wrapper scenes. Stable IDs, gameplay collision/state, hand ownership, reality interaction, glasses-world observation, and graybox fallback remain project-owned.
- Keep every batch manifest entry `placeholder=true` until behavior integration succeeds and actual Godot Forward+ screenshots are captured and visually inspected.
- Stop again for explicit user approval of the Forward+ evidence. Parameters, Blender renders, headless output, or claimed commands do not replace actual Forward+ images.
- Only after checkpoint 2 may the batch manifest entries switch to `placeholder=false`. Run the complete project verification, record its real result, and update the batch record, project status documents, context handoff, and root `progress.md`.

Track source scripts, hand-authored wrappers, contracts, and records. Keep `.blend`, review PNGs, candidate outputs, `artifacts/`, and manually edited `.glb.import` files untracked. Never auto-approve, auto-push, auto-merge, publish, rewrite history, or begin another asset stage.
