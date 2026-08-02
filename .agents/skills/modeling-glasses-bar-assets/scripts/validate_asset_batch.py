#!/usr/bin/env python3
"""Validate the current approval phase of a Glasses Bar asset batch.

The validator checks repository evidence; it never runs Blender, Godot, project
tests, changes a manifest, or grants approval. Missing evidence stays an error.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Iterable, Mapping
from pathlib import Path, PurePosixPath
from typing import Any


SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from init_asset_batch import planned_outputs, validate_config  # noqa: E402


PHASES = (
    "design",
    "silhouette-review",
    "formal-candidate",
    "forward-plus-review",
    "complete",
)
PHASE_INDEX = {phase: index for index, phase in enumerate(PHASES)}
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def _add(errors: list[tuple[str, str]], code: str, message: str) -> None:
    errors.append((code, message))


def _relative_path(value: object) -> PurePosixPath | None:
    if not isinstance(value, str) or not value.strip():
        return None
    normalized = value.strip().replace("\\", "/")
    path = PurePosixPath(normalized)
    if normalized.startswith("/") or any(part in {"", ".", ".."} for part in path.parts):
        return None
    if len(normalized) >= 2 and normalized[1] == ":":
        return None
    return path


def _tracked_files(project_root: Path) -> tuple[list[str], str | None]:
    result = subprocess.run(
        ["git", "-C", str(project_root), "ls-files"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )
    if result.returncode != 0:
        return [], result.stderr.strip() or "git ls-files failed"
    return [line.replace("\\", "/") for line in result.stdout.splitlines() if line.strip()], None


def _read_json(path: Path) -> tuple[Mapping[str, Any] | None, str | None]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        return None, str(error)
    if not isinstance(value, Mapping):
        return None, "JSON root is not an object"
    return value, None


def _read_head_json(project_root: Path, relative_path: PurePosixPath) -> Mapping[str, Any] | None:
    result = subprocess.run(
        ["git", "-C", str(project_root), "show", f"HEAD:{relative_path.as_posix()}"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )
    if result.returncode != 0:
        return None
    try:
        value = json.loads(result.stdout)
    except json.JSONDecodeError:
        return None
    return value if isinstance(value, Mapping) else None


def _manifest_assets(manifest: Mapping[str, Any]) -> tuple[dict[str, Mapping[str, Any]], str | None]:
    assets = manifest.get("assets")
    if not isinstance(assets, list):
        return {}, "manifest assets is not an array"
    by_id: dict[str, Mapping[str, Any]] = {}
    for entry in assets:
        if not isinstance(entry, Mapping) or not isinstance(entry.get("id"), str):
            return {}, "manifest contains an asset without a string id"
        asset_id = str(entry["id"])
        if asset_id in by_id:
            return {}, f"manifest duplicates asset id {asset_id!r}"
        by_id[asset_id] = entry
    return by_id, None


def _evidence_paths(
    checkpoint: Mapping[str, Any],
    field: str,
    project_root: Path,
    errors: list[tuple[str, str]],
    code: str,
) -> list[Path]:
    raw_paths = checkpoint.get(field)
    if not isinstance(raw_paths, list) or not raw_paths:
        _add(errors, code, f"checkpoint field {field!r} must list real evidence paths")
        return []
    resolved: list[Path] = []
    for raw in raw_paths:
        relative = _relative_path(raw)
        if relative is None or not relative.parts or relative.parts[0] != "artifacts":
            _add(errors, code, f"evidence path {raw!r} must be repository-relative under ignored artifacts/")
            continue
        path = (project_root / Path(relative.as_posix())).resolve()
        if not path.is_relative_to(project_root):
            _add(errors, code, f"evidence path escapes project_root through a linked path: {relative.as_posix()}")
            continue
        if not path.is_file():
            _add(errors, code, f"evidence file does not exist: {relative.as_posix()}")
            continue
        resolved.append(path)
    return resolved


def _require_user_approval(
    checkpoint: Mapping[str, Any], checkpoint_name: str, errors: list[tuple[str, str]]
) -> None:
    if checkpoint.get("status") != "approved":
        _add(errors, "CHECKPOINT_NOT_APPROVED", f"{checkpoint_name} checkpoint is not approved")
    if checkpoint.get("approved_by") != "user":
        _add(errors, "CHECKPOINT_APPROVER_MISSING", f"{checkpoint_name} approval must record approved_by=user")
    record = checkpoint.get("approval_record")
    if not isinstance(record, str) or not record.strip():
        _add(errors, "CHECKPOINT_RECORD_MISSING", f"{checkpoint_name} approval_record is missing")


def _require_skeletons(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> list[Path]:
    outputs = planned_outputs(config, project_root)
    for path in outputs:
        if not path.is_file():
            _add(errors, "SKELETON_MISSING", f"required batch file is missing: {path.relative_to(project_root).as_posix()}")
    return outputs


def _require_no_markers(
    paths: Iterable[Path], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    for path in paths:
        if path.suffix.lower() not in {".py", ".cs", ".md"} or not path.is_file():
            continue
        try:
            content = path.read_text(encoding="utf-8")
        except OSError as error:
            _add(errors, "SOURCE_UNREADABLE", f"cannot read {path.relative_to(project_root).as_posix()}: {error}")
            continue
        if "RAISE_UNTIL_" in content:
            _add(errors, "SKELETON_UNRESOLVED", f"generated stop marker remains in {path.relative_to(project_root).as_posix()}")


def _formal_paths(config: Mapping[str, object], project_root: Path) -> list[tuple[str, Path, Path]]:
    paths = config["paths"]
    assets = config["assets"]
    assert isinstance(paths, Mapping)
    assert isinstance(assets, list)
    model_root = (project_root / str(paths["formal_model_root"])).resolve()
    wrapper_root = (project_root / str(paths["wrapper_root"])).resolve()
    result: list[tuple[str, Path, Path]] = []
    for asset in assets:
        assert isinstance(asset, Mapping)
        asset_id = str(asset["asset_id"])
        result.append((asset_id, model_root / f"{asset_id}.glb", wrapper_root / f"{asset_id}.tscn"))
    return result


def _require_formal_assets(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    for asset_id, model, wrapper in _formal_paths(config, project_root):
        if not model.is_file():
            _add(errors, "FORMAL_GLB_MISSING", f"formal GLB is missing for {asset_id}: {model.relative_to(project_root).as_posix()}")
        if not wrapper.is_file():
            _add(errors, "WRAPPER_MISSING", f"hand-authored wrapper is missing for {asset_id}: {wrapper.relative_to(project_root).as_posix()}")
            continue
        try:
            wrapper_text = wrapper.read_text(encoding="utf-8")
        except OSError as error:
            _add(errors, "WRAPPER_UNREADABLE", f"cannot read wrapper for {asset_id}: {error}")
            continue
        if f'metadata/asset_id = "{asset_id}"' not in wrapper_text:
            _add(errors, "WRAPPER_STABLE_ID_MISSING", f"wrapper for {asset_id} does not declare its stable asset_id metadata")
        if ".glb\"" not in wrapper_text or "PackedScene" not in wrapper_text:
            _add(errors, "WRAPPER_VISUAL_CHILD_MISSING", f"wrapper for {asset_id} does not instance a GLB visual child")


def _reject_formal_assets(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    for asset_id, model, wrapper in _formal_paths(config, project_root):
        if model.exists():
            _add(errors, "FORMAL_BEFORE_APPROVAL", f"formal GLB exists before checkpoint 1 approval: {asset_id}")
        if wrapper.exists():
            _add(errors, "WRAPPER_BEFORE_APPROVAL", f"wrapper exists before checkpoint 1 approval: {asset_id}")


def _require_manifest_state(
    config: Mapping[str, object],
    project_root: Path,
    expected_placeholder: bool,
    check_non_batch_changes: bool,
    errors: list[tuple[str, str]],
) -> None:
    paths = config["paths"]
    assets = config["assets"]
    assert isinstance(paths, Mapping)
    assert isinstance(assets, list)
    manifest_relative = _relative_path(paths["json_manifest"])
    assert manifest_relative is not None
    manifest_path = project_root / Path(manifest_relative.as_posix())
    manifest, read_error = _read_json(manifest_path)
    if read_error or manifest is None:
        _add(errors, "MANIFEST_UNREADABLE", f"cannot read manifest {manifest_relative.as_posix()}: {read_error}")
        return
    current, shape_error = _manifest_assets(manifest)
    if shape_error:
        _add(errors, "MANIFEST_INVALID", shape_error)
        return

    batch_ids = {str(asset["runtime_id"]) for asset in assets if isinstance(asset, Mapping)}
    for runtime_id in sorted(batch_ids):
        entry = current.get(runtime_id)
        if entry is None:
            _add(errors, "MANIFEST_ASSET_MISSING", f"manifest has no batch runtime id {runtime_id!r}")
        elif entry.get("placeholder") is not expected_placeholder:
            _add(
                errors,
                "MANIFEST_GATE_VIOLATION",
                f"manifest asset {runtime_id!r} must have placeholder={str(expected_placeholder).lower()}",
            )

    if not check_non_batch_changes:
        return
    baseline_manifest = _read_head_json(project_root, manifest_relative)
    if baseline_manifest is None:
        _add(errors, "MANIFEST_BASELINE_MISSING", "cannot compare manifest changes with HEAD")
        return
    baseline, baseline_error = _manifest_assets(baseline_manifest)
    if baseline_error:
        _add(errors, "MANIFEST_BASELINE_INVALID", baseline_error)
        return
    for asset_id in sorted(set(current) | set(baseline)):
        if asset_id in batch_ids:
            continue
        current_entry = current.get(asset_id)
        baseline_entry = baseline.get(asset_id)
        if current_entry is None or baseline_entry is None:
            _add(errors, "NON_BATCH_MANIFEST_CHANGED", f"non-batch manifest membership changed for {asset_id!r}")
        elif current_entry.get("placeholder") is not baseline_entry.get("placeholder"):
            _add(errors, "NON_BATCH_MANIFEST_CHANGED", f"non-batch placeholder changed for {asset_id!r}")


def _require_silhouette_evidence(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    checkpoints = config["checkpoints"]
    paths = config["paths"]
    assets = config["assets"]
    assert isinstance(checkpoints, Mapping)
    assert isinstance(paths, Mapping)
    assert isinstance(assets, list)
    silhouette = checkpoints["silhouette"]
    assert isinstance(silhouette, Mapping)
    inspection = silhouette.get("inspection_record")
    if not isinstance(inspection, str) or not inspection.strip():
        _add(errors, "SILHOUETTE_INSPECTION_MISSING", "silhouette checkpoint must record actual image inspection")
    evidence = _evidence_paths(silhouette, "evidence", project_root, errors, "SILHOUETTE_EVIDENCE_MISSING")
    actual_png = False
    for path in evidence:
        if path.suffix.lower() != ".png":
            continue
        try:
            actual_png = actual_png or path.read_bytes()[:8] == PNG_SIGNATURE
        except OSError:
            pass
    if evidence and not actual_png:
        _add(errors, "SILHOUETTE_PNG_MISSING", "silhouette evidence has no actual PNG image")
    candidate_root = project_root / str(paths["candidate_root"])
    for asset in assets:
        assert isinstance(asset, Mapping)
        candidate = candidate_root / f"{asset['asset_id']}.glb"
        if not candidate.is_file():
            _add(errors, "CANDIDATE_GLB_MISSING", f"candidate GLB is missing: {candidate.relative_to(project_root).as_posix()}")


def _require_forward_plus_evidence(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    checkpoints = config["checkpoints"]
    assert isinstance(checkpoints, Mapping)
    checkpoint = checkpoints["forward_plus"]
    assert isinstance(checkpoint, Mapping)
    inspection = checkpoint.get("inspection_record")
    if not isinstance(inspection, str) or not inspection.strip():
        _add(errors, "FORWARD_PLUS_INSPECTION_MISSING", "forward_plus checkpoint must record actual PNG inspection")
    if str(checkpoint.get("renderer", "")).strip().lower() != "forward+":
        _add(errors, "FORWARD_PLUS_RENDERER_MISSING", "forward_plus.renderer must record Forward+")
    capture_scene = _relative_path(checkpoint.get("capture_scene"))
    if capture_scene is None or capture_scene.suffix.lower() != ".tscn" or not (project_root / Path(capture_scene.as_posix())).is_file():
        _add(errors, "FORWARD_PLUS_CAPTURE_MISSING", "forward_plus.capture_scene must name an existing Godot capture scene")

    evidence = _evidence_paths(checkpoint, "evidence", project_root, errors, "FORWARD_PLUS_EVIDENCE_MISSING")
    actual_pngs: list[Path] = []
    for path in evidence:
        if path.suffix.lower() != ".png":
            continue
        try:
            if path.read_bytes()[:8] == PNG_SIGNATURE:
                actual_pngs.append(path)
        except OSError:
            pass
    if not actual_pngs:
        _add(errors, "FORWARD_PLUS_PNG_MISSING", "Forward+ evidence has no actual PNG image")

    integration = _evidence_paths(
        checkpoint, "integration_evidence", project_root, errors, "INTEGRATION_EVIDENCE_MISSING"
    )
    if integration:
        pass_found = False
        for path in integration:
            try:
                content = path.read_text(encoding="utf-8", errors="replace").upper()
            except OSError:
                continue
            if "PASS" in content:
                pass_found = True
        if not pass_found:
            _add(errors, "INTEGRATION_PASS_MISSING", "integration evidence does not record an unambiguous PASS")


def _require_completion_record(
    config: Mapping[str, object], project_root: Path, errors: list[tuple[str, str]]
) -> None:
    paths = config["paths"]
    assert isinstance(paths, Mapping)
    record = project_root / str(paths["batch_record"])
    try:
        content = record.read_text(encoding="utf-8")
    except OSError as error:
        _add(errors, "BATCH_RECORD_UNREADABLE", f"cannot read batch record: {error}")
        return
    lowered = content.lower()
    required_headings = (
        "## completion",
        "### completed items",
        "### key decisions",
        "### remaining todos",
        "## verification summary",
    )
    for heading in required_headings:
        if heading not in lowered:
            _add(errors, "BATCH_RECORD_INCOMPLETE", f"batch record is missing heading {heading!r}")
    if "status: pass" not in lowered or "unverified" in lowered:
        _add(errors, "VERIFICATION_SUMMARY_INCOMPLETE", "batch record must contain a real Status: PASS and no UNVERIFIED marker")

    verification = config.get("verification")
    if not isinstance(verification, Mapping) or verification.get("status") != "passed":
        _add(errors, "PROJECT_VERIFICATION_MISSING", "verification.status must be passed after the complete project verification")
        return
    evidence = _evidence_paths(verification, "evidence", project_root, errors, "PROJECT_VERIFICATION_EVIDENCE_MISSING")
    if evidence:
        pass_found = False
        for path in evidence:
            try:
                text = path.read_text(encoding="utf-8", errors="replace").upper()
            except OSError:
                continue
            if "PASS" in text:
                pass_found = True
        if not pass_found:
            _add(errors, "PROJECT_VERIFICATION_PASS_MISSING", "project verification evidence does not record an unambiguous PASS")


def validate_batch(
    config: Mapping[str, object],
    project_root: Path | str,
    phase: str,
    tracked_files: Iterable[str] | None = None,
) -> list[str]:
    """Return stable ``CODE: message`` errors for the requested phase."""

    errors: list[tuple[str, str]] = []
    for message in validate_config(config):
        _add(errors, "CONFIG_INVALID", message)
    if phase not in PHASE_INDEX:
        _add(errors, "PHASE_INVALID", f"phase must be one of {', '.join(PHASES)}")
    if errors:
        return [f"{code}: {message}" for code, message in sorted(set(errors))]

    root = Path(project_root).resolve()
    paths = config["paths"]
    assert isinstance(paths, Mapping)
    for path_name in ("candidate_root", "formal_model_root", "wrapper_root", "batch_record", "json_manifest"):
        configured = (root / str(paths[path_name])).resolve()
        if not configured.is_relative_to(root):
            _add(errors, "PATH_ESCAPE", f"paths.{path_name} escapes project_root through a linked path")
    if errors:
        return [f"{code}: {message}" for code, message in sorted(set(errors))]

    phase_number = PHASE_INDEX[phase]
    if phase_number == 0:
        return []

    if tracked_files is None:
        tracked, git_error = _tracked_files(root)
        if git_error:
            _add(errors, "GIT_TRACKING_UNAVAILABLE", git_error)
            tracked = []
    else:
        tracked = [str(path).replace("\\", "/") for path in tracked_files]
    for tracked_path in sorted(set(tracked)):
        lowered = tracked_path.lower()
        if lowered == "artifacts" or lowered.startswith("artifacts/"):
            _add(errors, "TRACKED_ARTIFACT", f"ignored artifact is tracked: {tracked_path}")
        if lowered.endswith(".png"):
            _add(errors, "TRACKED_SCREENSHOT", f"review screenshot is tracked: {tracked_path}")
        if lowered.endswith(".blend"):
            _add(errors, "TRACKED_BLEND", f"Blender working file is tracked: {tracked_path}")
        if lowered.endswith(".glb.import"):
            _add(errors, "TRACKED_IMPORT_METADATA", f"manual GLB import metadata is tracked: {tracked_path}")

    skeletons = _require_skeletons(config, root, errors)
    _require_silhouette_evidence(config, root, errors)
    checkpoints = config["checkpoints"]
    assert isinstance(checkpoints, Mapping)
    silhouette = checkpoints["silhouette"]
    forward_plus = checkpoints["forward_plus"]
    assert isinstance(silhouette, Mapping)
    assert isinstance(forward_plus, Mapping)

    if phase == "silhouette-review":
        _reject_formal_assets(config, root, errors)
        _require_no_markers(skeletons[:4], root, errors)
    else:
        _require_user_approval(silhouette, "silhouette", errors)
        _require_formal_assets(config, root, errors)
        if phase != "complete":
            _require_manifest_state(config, root, True, True, errors)

    if phase_number >= PHASE_INDEX["forward-plus-review"]:
        _require_no_markers(skeletons, root, errors)
        _require_forward_plus_evidence(config, root, errors)

    if phase == "complete":
        _require_user_approval(forward_plus, "forward_plus", errors)
        _require_manifest_state(config, root, False, True, errors)
        _require_completion_record(config, root, errors)

    return [f"{code}: {message}" for code, message in sorted(set(errors))]


def _load_config(path: Path) -> Mapping[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, Mapping):
        raise ValueError("configuration root must be a JSON object")
    return value


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Check asset-batch phase gates without running Blender, Godot, tests, or approvals."
    )
    parser.add_argument("--config", required=True, type=Path, help="Batch JSON configuration")
    parser.add_argument("--project-root", required=True, type=Path, help="Repository root")
    parser.add_argument("--phase", required=True, choices=PHASES)
    args = parser.parse_args()
    try:
        config = _load_config(args.config)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"ERROR CONFIG_READ: {error}")
        print(f"SUMMARY batch=unknown phase={args.phase} errors=1")
        return 2

    errors = validate_batch(config, args.project_root, args.phase)
    for error in errors:
        print(f"ERROR {error}")
    print(f"SUMMARY batch={config.get('batch_id', 'unknown')} phase={args.phase} errors={len(errors)}")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
