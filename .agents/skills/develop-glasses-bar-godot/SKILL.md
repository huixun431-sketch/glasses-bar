---
name: develop-glasses-bar-godot
description: Maintain and extend the Glasses Bar Godot C# project while preserving its observation-memory-manual-operation gameplay contract, parallel reality/glasses worlds, data-driven recipes, stable liquid interactions, asset handoff rules, tests, and project status. Use for gameplay implementation, Godot scene or C# changes, recipe/tool/liquid work, GLB asset integration, visual review, bug fixes, and milestone reporting in this repository.
---

# Develop Glasses Bar Godot

## Establish context

1. Inspect `project.godot`, `scenes`, `scripts`, `assets`, `docs`, `tools`, tests, and Git status.
2. Read `docs/CONTEXT_HANDOFF.md` first, then `docs/PROJECT_STATUS.md`, `docs/ROADMAP.md`, and `docs/CHANGELOG.md` before changing code.
3. Read [gameplay-contract.md](references/gameplay-contract.md) for gameplay or UX work.
4. Read [asset-handoff.md](references/asset-handoff.md) for model, material, collision, import, or replacement work.
5. Preserve newer approved design decisions over historical code or experiments.

## Manage context proactively

- Refresh `docs/CONTEXT_HANDOFF.md` before a long implementation pass, after a high-impact decision, after a verified milestone, before delegating, and before likely context compaction.
- Label retained information by priority:
  - `P0`: latest user decisions, approved gameplay direction, prohibited invention, safety, and authorization boundaries. Never drop these.
  - `P1`: active milestone, implemented interfaces, verified tests, blockers, and exact next action.
  - `P2`: asset handoff details, technical references, and useful file pointers.
  - `P3`: history, discarded experiments, and background that can be reloaded from source.
- Compress repetition into one authoritative statement plus file pointers. Do not copy full source documents into the handoff.
- Mark claims as verified, implemented-but-unverified, blocked, or planned. Never let compression promote an unverified claim to completed.
- Keep the handoff concise enough to read first; move durable detail into the appropriate design or asset document.

## Implement safely

- Keep authoritative gameplay state outside `RealityWorld` and `GlassesWorld`.
- Treat the glasses as a state transition: provide information and planning support without automating execution.
- Keep scenes modular, communicate through signals, and separate domain logic from Godot presentation.
- Define recipes, ingredients, tools, tolerances, and asset paths as data. Mark all unapproved content `IsPrototype=true`.
- Prefer stable parameterized simulation over fragile physical realism. Always allow recovery from simulation errors.
- Wrap imported GLB scenes; never attach gameplay directly to generated import nodes.
- Do not invent final recipes, customer stories, balance values, or art decisions.

## Validate and report

1. Run pure C# tests, Godot headless smoke tests, and the asset validator when the required runtimes exist.
2. For visual changes, run the project, capture actual screenshots, and inspect readability, scale, lighting, materials, blur, and interaction clarity.
3. Update `docs/PROJECT_STATUS.md` and `docs/CHANGELOG.md` to match verified implementation.
4. Refresh `docs/CONTEXT_HANDOFF.md` with the new verified state, blockers, and next action.
5. Report work as completed, in progress, or planned. Never call unrun code tested.
6. Do not commit, merge, push, publish, or rewrite history without user approval.
