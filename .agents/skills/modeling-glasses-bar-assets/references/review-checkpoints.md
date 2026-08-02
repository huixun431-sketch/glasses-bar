# Human review checkpoints

Both checkpoints require actual evidence inspection and an explicit user decision. `pending` and `changes_requested` always stop production at the same gate.

## Checkpoint 1: neutral Blender silhouette

Before presenting the review, confirm:

- every asset uses its configured stable asset ID and approved meter-scale coordinate convention;
- transforms, roots, required anchors, mesh presence, and export structure validate;
- no formal path, wrapper, or manifest state has changed;
- the candidate set is under ignored `artifacts/`;
- actual front and three-quarter PNGs are open and inspected;
- the approved scale reference plus any batch-required family, combination, clearance, or functional profile view is visible;
- silhouettes, contact points, openings, handles, spouts, gripping/placement intent, proportions, and inter-asset scale are readable;
- the review uses neutral presentation and does not smuggle in unapproved final materials or art direction.

Present paths and a concise visual conclusion, then stop. Continue only after the user explicitly approves. Record `status: approved`, `approved_by: user`, the evidence paths, and a short `approval_record`; never infer approval from silence or prior batches.

## Checkpoint 2: actual Godot Forward+

Before presenting the review, confirm:

- formal GLBs validate and remain visual children of hand-authored wrappers;
- wrapper stable metadata and required anchors are present;
- existing gameplay-owned collision/state, hands, reset behavior, and graybox fallback remain intact;
- reality-world manual interaction and glasses-world observation use the same authoritative gameplay state;
- integration evidence records an actual pass;
- the manifest still has `placeholder=true` for the entire batch;
- a non-headless Godot Forward+ capture scene produced actual PNG files;
- the images are opened and inspected in both reality and glasses worlds and in every relevant world, hand-held, family, scale, clearance, and functional context;
- silhouette, scale, contact, PBR response, transparency where approved, highlight/readability, pose, clipping, occlusion, lighting, labels, and contextual clarity are judged from pixels rather than constants or logs.

Blender renders, parameter reviews, unit/headless results, previous-batch images, stale PNGs, and a capture command without inspected output are not checkpoint-2 evidence.

Present the exact images, environment/renderer record, integration evidence, and visual conclusion, then stop. Switch manifest state only after explicit user approval is recorded.

## Revision loop

For every requested visual revision:

1. Change only reproducible generator/wrapper/presentation source inside the approved scope.
2. Regenerate and revalidate the affected GLBs.
3. Reimport through the hand-authored wrappers.
4. Re-run relevant behavior integration.
5. Recapture fresh deterministic Forward+ images.
6. Open and inspect the replacement images.
7. Update the evidence record and return to the same user checkpoint.

Do not preserve a passing label when evidence has gone stale. Do not solve a visual failure by moving gameplay state, collision, or interaction into imported geometry.

## Evidence record shape

Each checkpoint record contains:

- status and explicit user approval metadata;
- date and approved design source;
- tool/runtime and renderer where relevant;
- candidate/formal validation output path;
- actual image paths and the views each proves;
- integration/full-verification evidence paths where relevant;
- observed issues, revisions, and final visual conclusion;
- manifest/graybox state and confirmation that the next stage was not started.

At completion, the batch record and root `progress.md` must separately list completed items, key decisions, and remaining TODOs. Missing runtimes or evidence are blockers, never successful checks.
