# Asset batch workflow

This is the production state machine. Run the phase validator before and after the current phase; stop on every reported error.

| Phase | Required input | Actions | Output and stop condition |
|---|---|---|---|
| `design` | Explicitly authorized batch scope, approved design source, pending checkpoint config | Record stable asset/runtime IDs, relevant anchors, interaction kind, and repository-relative paths. Scaffold the batch. Write behavior contracts without inventing art or gameplay values. | Contract/generator/review/test/capture/record source exists. Stop if scope or design values are unapproved. |
| `silhouette-review` | Contract behavior established; neutral candidates only under ignored `artifacts/` | Generate candidate GLBs in `--mode silhouette`; validate IDs, scale conventions, transforms, anchors, and meshes; render neutral front, three-quarter, and required scale/context views; inspect the actual images. | Present evidence and stop for explicit user checkpoint-1 approval. No formal GLB, wrapper, or manifest change is allowed. |
| `formal-candidate` | Checkpoint 1 is `approved`, `approved_by=user`, with an approval record | Generate `--mode final` GLBs from only the approved design/material direction. Validate GLBs. Add hand-authored wrapper scenes that instance the GLB as a visual child. Preserve project-owned collision/state, stable runtime IDs, hands, worlds, and graybox fallback. | Formal GLBs/wrappers may exist; batch manifest entries remain `placeholder=true`. Stop if integration is incomplete. |
| `forward-plus-review` | Formal candidates, wrappers, graybox manifest state, behavior integration | Run the relevant integration checks. Launch the real Godot Forward+ project/capture scene, capture deterministic PNGs, open and inspect them for both worlds and relevant world/hand/context compositions. Repeat generation, validation, import, capture, and visual review after every revision. | Present real Forward+ images and integration evidence; stop for explicit user checkpoint-2 approval. No manifest switch is allowed. |
| `complete` | Checkpoint 2 is `approved`, `approved_by=user`, with an approval record | Switch only this batch's manifest entries to `placeholder=false`; keep all other manifest entries unchanged. Run complete project verification. Record its actual output and update batch/project status, changelog, roadmap, context handoff, and root `progress.md`. | Archive completed items, key decisions, remaining TODOs, evidence, and stage boundary. Do not start the next batch or stage. |

## Ownership contract

- GLB owns replaceable visual geometry and approved PBR presentation only.
- A hand-authored Godot wrapper owns the visual instance and stable asset metadata.
- Existing gameplay code owns collision, state, interaction, hand assignment, resets, and reality/glasses-world behavior.
- Reality remains manually interactive; the glasses world remains observation/planning only; both share one gameplay state.
- The graybox remains a recoverable fallback until checkpoint 2, manifest switching, and full verification all succeed.

## Repository hygiene

Track reproducible source, contracts, hand-authored wrappers, integration/capture source, batch records, and approved formal GLBs. Keep candidate outputs, review PNGs, `.blend`, `artifacts/`, and manual `.glb.import` edits untracked. Preserve unrelated user changes and never clean, overwrite, stage, or commit them.

No phase authorizes a push, merge, publication, history rewrite, automatic approval, or expansion into a new stage.
