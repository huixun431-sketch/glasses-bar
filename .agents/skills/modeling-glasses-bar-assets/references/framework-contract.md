# Batch framework contract

The two Python tools use only the standard library and do not invoke Blender, Godot, tests, or approvals.

## Configuration

Create one repository-relative JSON file. Required shape:

```json
{
  "batch_id": "approved-batch-slug",
  "stage": "approved stage label",
  "assets": [
    {
      "asset_id": "stable_model_id",
      "runtime_id": "existing_runtime_id",
      "required_anchors": ["Placement", "Interaction"],
      "interaction_kind": "approved_kind"
    }
  ],
  "paths": {
    "candidate_root": "artifacts/approved-batch-slug",
    "formal_model_root": "assets/models",
    "wrapper_root": "scenes/assets/approved-batch-slug",
    "batch_record": "docs/assets/APPROVED_BATCH_RECORD.md",
    "json_manifest": "assets/asset_manifest.json"
  },
  "checkpoints": {
    "silhouette": {"status": "pending", "evidence": []},
    "forward_plus": {"status": "pending", "evidence": []}
  }
}
```

`batch_id` uses lowercase words separated by hyphens. Asset/runtime IDs and interaction kinds use lowercase snake case. Anchors are non-empty names copied from the approved contract. All paths must remain inside the repository; the candidate root must be under ignored `artifacts/`.

Supported checkpoint states are `pending`, `changes_requested`, and `approved`. An approved checkpoint also records `approved_by: "user"` and a non-empty `approval_record`. Checkpoint evidence is a list of existing repository-relative artifact paths.

Each review checkpoint adds a non-empty `inspection_record` summarizing what was actually seen in the current evidence. This is a record of inspection, not an approval field.

For `forward-plus-review`, the `forward_plus` object additionally records:

- `renderer: "Forward+"`;
- `capture_scene`: existing repository-relative `.tscn` path;
- `evidence`: actual Forward+ PNG paths under `artifacts/`;
- `integration_evidence`: artifact log/text paths containing an unambiguous pass result.

For `complete`, add `verification: {"status": "passed", "evidence": [...]}` with actual full-project verification logs under `artifacts/`. These fields record evidence; they never create approval.

## Initializer

```text
python .agents/skills/modeling-glasses-bar-assets/scripts/init_asset_batch.py --config PATH --project-root PATH [--dry-run]
```

Public Python interfaces:

- `validate_config(config) -> list[str]`
- `planned_outputs(config, project_root) -> list[Path]`
- `render_outputs(config, template_root) -> dict[Path, str]`
- `main() -> int`

For batch slug `approved_batch_slug` / Pascal name `ApprovedBatchSlug`, the initializer plans:

```text
tools/modeling/approved_batch_slug_asset_contract.py
tools/modeling/generate_approved_batch_slug_assets.py
tools/modeling/render_approved_batch_slug_review.py
tests/tools/test_approved_batch_slug_asset_contract.py
tests/godot/ApprovedBatchSlugAssetIntegrationTests.cs
tests/godot/ApprovedBatchSlugAssetIntegrationTests.tscn
tests/godot/ApprovedBatchSlugAssetVisualCapture.cs
tests/godot/ApprovedBatchSlugAssetVisualCapture.tscn
<paths.batch_record>
```

It validates every field and every destination conflict before writing. `--dry-run` prints all destinations and writes nothing. A normal run renders everything into a temporary sibling directory, refuses every existing destination, and rolls back newly moved files on a write failure. Generated stop markers must be replaced from approved design/integration information before later gates.

The generated Blender entry point supports only:

```text
--mode silhouette --output <ignored candidate root>
--mode final --output <formal model root>
```

`final` remains locked while checkpoint 1 is pending.

## Phase validator

```text
python .agents/skills/modeling-glasses-bar-assets/scripts/validate_asset_batch.py --config PATH --project-root PATH --phase design|silhouette-review|formal-candidate|forward-plus-review|complete
```

Public interface: `validate_batch(config, project_root, phase, tracked_files=None) -> list[str]`.

The validator accumulates deterministic errors for configuration, skeletons, ignored/tracked artifacts, checkpoint evidence, formal files, hand-authored wrappers, stable IDs, manifest graybox state, Forward+ PNGs, integration evidence, completion record, and project verification. It compares non-batch manifest placeholder state with `HEAD` at formal/complete gates.

CLI output is stable:

```text
ERROR <stable-code>: <message>
SUMMARY batch=<id> phase=<phase> errors=<n>
```

Only zero errors returns exit code `0`. The validator is a gate checker, not a substitute for asset validation, behavior tests, Godot capture, visual inspection, or the complete project verification command.
