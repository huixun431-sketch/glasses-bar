#!/usr/bin/env python3
"""Scaffold a guarded Glasses Bar asset batch.

This tool only creates neutral implementation skeletons. It does not run Blender or
Godot, approve a checkpoint, choose art values, or change an asset manifest.
"""

from __future__ import annotations

import argparse
import json
import re
import tempfile
from collections.abc import Mapping
from pathlib import Path, PurePosixPath
from string import Template
from typing import Any


BATCH_ID_RE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
IDENTIFIER_RE = re.compile(r"^[a-z][a-z0-9_]*$")
ANCHOR_RE = re.compile(r"^[A-Za-z][A-Za-z0-9_]*$")
CHECKPOINT_STATES = {"pending", "changes_requested", "approved"}
REQUIRED_PATHS = (
    "candidate_root",
    "formal_model_root",
    "wrapper_root",
    "batch_record",
    "json_manifest",
)
TEMPLATE_FILES = {
    "contract": "asset_contract.py.tmpl",
    "generator": "blender_generator.py.tmpl",
    "renderer": "blender_review_renderer.py.tmpl",
    "contract_test": "contract_test.py.tmpl",
    "integration": "godot_integration_test.cs.tmpl",
    "capture": "godot_visual_capture.cs.tmpl",
    "record": "asset_batch_record.md.tmpl",
}


def _safe_relative_path(value: object) -> tuple[PurePosixPath | None, str | None]:
    if not isinstance(value, str) or not value.strip():
        return None, "must be a non-empty repository-relative path"
    normalized = value.strip().replace("\\", "/")
    if normalized.startswith("/") or re.match(r"^[A-Za-z]:", normalized):
        return None, "must not be absolute"
    path = PurePosixPath(normalized)
    if any(part in {"", ".", ".."} for part in path.parts):
        return None, "must not contain empty, current-directory, or parent segments"
    return path, None


def validate_config(config: Mapping[str, object]) -> list[str]:
    """Return every deterministic configuration error without writing files."""

    errors: list[str] = []
    if not isinstance(config, Mapping):
        return ["config must be a JSON object"]

    batch_id = config.get("batch_id")
    if not isinstance(batch_id, str) or not BATCH_ID_RE.fullmatch(batch_id):
        errors.append("batch_id must use lowercase letters, numbers, and single hyphens")

    stage = config.get("stage")
    if isinstance(stage, bool) or not isinstance(stage, (str, int)) or str(stage).strip() == "":
        errors.append("stage must be a non-empty string or integer")

    assets = config.get("assets")
    seen_asset_ids: set[str] = set()
    seen_runtime_ids: set[str] = set()
    if not isinstance(assets, list) or not assets:
        errors.append("assets must be a non-empty array")
    else:
        for index, asset in enumerate(assets):
            prefix = f"assets[{index}]"
            if not isinstance(asset, Mapping):
                errors.append(f"{prefix} must be an object")
                continue
            asset_id = asset.get("asset_id")
            if not isinstance(asset_id, str) or not IDENTIFIER_RE.fullmatch(asset_id):
                errors.append(f"{prefix}.asset_id must be a lowercase snake_case identifier")
            elif asset_id in seen_asset_ids:
                errors.append(f"{prefix}.asset_id duplicates {asset_id!r}")
            else:
                seen_asset_ids.add(asset_id)

            runtime_id = asset.get("runtime_id")
            if not isinstance(runtime_id, str) or not IDENTIFIER_RE.fullmatch(runtime_id):
                errors.append(f"{prefix}.runtime_id must be a lowercase snake_case identifier")
            elif runtime_id in seen_runtime_ids:
                errors.append(f"{prefix}.runtime_id duplicates {runtime_id!r}")
            else:
                seen_runtime_ids.add(runtime_id)

            anchors = asset.get("required_anchors")
            if not isinstance(anchors, list) or not anchors:
                errors.append(f"{prefix}.required_anchors must be a non-empty array")
            else:
                seen_anchors: set[str] = set()
                for anchor in anchors:
                    if not isinstance(anchor, str) or not ANCHOR_RE.fullmatch(anchor):
                        errors.append(f"{prefix}.required_anchors contains an invalid anchor name")
                    elif anchor in seen_anchors:
                        errors.append(f"{prefix}.required_anchors duplicates {anchor!r}")
                    else:
                        seen_anchors.add(anchor)

            interaction_kind = asset.get("interaction_kind")
            if not isinstance(interaction_kind, str) or not IDENTIFIER_RE.fullmatch(interaction_kind):
                errors.append(f"{prefix}.interaction_kind must be a lowercase snake_case identifier")

    paths = config.get("paths")
    parsed_paths: dict[str, PurePosixPath] = {}
    if not isinstance(paths, Mapping):
        errors.append("paths must be an object")
    else:
        for key in REQUIRED_PATHS:
            parsed, error = _safe_relative_path(paths.get(key))
            if error:
                errors.append(f"paths.{key} {error}")
            elif parsed is not None:
                parsed_paths[key] = parsed
        candidate = parsed_paths.get("candidate_root")
        if candidate is not None and (not candidate.parts or candidate.parts[0] != "artifacts"):
            errors.append("paths.candidate_root must be under ignored artifacts/")
        if len(set(parsed_paths.values())) != len(parsed_paths):
            errors.append("paths entries must not resolve to the same repository path")
        if isinstance(batch_id, str) and BATCH_ID_RE.fullmatch(batch_id) and "batch_record" in parsed_paths:
            batch_slug = batch_id.replace("-", "_")
            reserved = {
                PurePosixPath(f"tools/modeling/{batch_slug}_asset_contract.py"),
                PurePosixPath(f"tools/modeling/generate_{batch_slug}_assets.py"),
                PurePosixPath(f"tools/modeling/render_{batch_slug}_review.py"),
                PurePosixPath(f"tests/tools/test_{batch_slug}_asset_contract.py"),
            }
            if parsed_paths["batch_record"] in reserved:
                errors.append("paths.batch_record collides with a generated source path")

    checkpoints = config.get("checkpoints")
    if not isinstance(checkpoints, Mapping):
        errors.append("checkpoints must be an object")
    else:
        for checkpoint_name in ("silhouette", "forward_plus"):
            checkpoint = checkpoints.get(checkpoint_name)
            if not isinstance(checkpoint, Mapping):
                errors.append(f"checkpoints.{checkpoint_name} must be an object")
                continue
            status = checkpoint.get("status")
            if status not in CHECKPOINT_STATES:
                errors.append(
                    f"checkpoints.{checkpoint_name}.status must be one of "
                    + ", ".join(sorted(CHECKPOINT_STATES))
                )
            evidence = checkpoint.get("evidence")
            if not isinstance(evidence, list) or any(not isinstance(item, str) for item in evidence):
                errors.append(f"checkpoints.{checkpoint_name}.evidence must be an array of paths")

    return errors


def _batch_names(config: Mapping[str, object]) -> tuple[str, str]:
    batch_id = str(config["batch_id"])
    batch_slug = batch_id.replace("-", "_")
    pascal_batch = "".join(part.capitalize() for part in batch_id.split("-"))
    return batch_slug, pascal_batch


def _relative_outputs(config: Mapping[str, object]) -> list[Path]:
    batch_slug, pascal_batch = _batch_names(config)
    paths = config["paths"]
    assert isinstance(paths, Mapping)
    return [
        Path(f"tools/modeling/{batch_slug}_asset_contract.py"),
        Path(f"tools/modeling/generate_{batch_slug}_assets.py"),
        Path(f"tools/modeling/render_{batch_slug}_review.py"),
        Path(f"tests/tools/test_{batch_slug}_asset_contract.py"),
        Path(f"tests/godot/{pascal_batch}AssetIntegrationTests.cs"),
        Path(f"tests/godot/{pascal_batch}AssetIntegrationTests.tscn"),
        Path(f"tests/godot/{pascal_batch}AssetVisualCapture.cs"),
        Path(f"tests/godot/{pascal_batch}AssetVisualCapture.tscn"),
        Path(str(paths["batch_record"]).replace("\\", "/")),
    ]


def planned_outputs(config: Mapping[str, object], project_root: Path | str) -> list[Path]:
    """Return the deterministic absolute output list for a valid configuration."""

    errors = validate_config(config)
    if errors:
        raise ValueError("; ".join(errors))
    root = Path(project_root).resolve()
    outputs = [(root / relative).resolve() for relative in _relative_outputs(config)]
    escaped = [path for path in outputs if not path.is_relative_to(root)]
    if escaped:
        raise ValueError("generated output escapes project_root through a linked path")
    return outputs


def _template_values(config: Mapping[str, object]) -> dict[str, str]:
    batch_slug, pascal_batch = _batch_names(config)
    assets = config["assets"]
    paths = config["paths"]
    checkpoints = config["checkpoints"]
    assert isinstance(assets, list)
    assert isinstance(paths, Mapping)
    assert isinstance(checkpoints, Mapping)

    builders: list[str] = []
    csharp_anchors: list[str] = []
    csharp_wrappers: list[str] = []
    record_rows: list[str] = []
    wrapper_root = str(paths["wrapper_root"]).replace("\\", "/")
    for asset in assets:
        assert isinstance(asset, Mapping)
        asset_id = str(asset["asset_id"])
        runtime_id = str(asset["runtime_id"])
        anchors = list(asset["required_anchors"])
        builders.append(
            f"def build_{asset_id}(contract):\n"
            f"    raise RuntimeError(\"RAISE_UNTIL_DESIGN_APPROVED: implement approved geometry for {asset_id}\")\n\n"
            f"BUILDERS[{asset_id!r}] = build_{asset_id}"
        )
        csharp_anchor_values = ", ".join(json.dumps(str(anchor)) for anchor in anchors)
        csharp_anchors.append(f"        [{json.dumps(asset_id)}] = new[] {{ {csharp_anchor_values} }},")
        wrapper_path = f"res://{wrapper_root}/{asset_id}.tscn"
        csharp_wrappers.append(f"        [{json.dumps(asset_id)}] = {json.dumps(wrapper_path)},")
        record_rows.append(
            f"| `{asset_id}` | `{runtime_id}` | "
            + ", ".join(f"`{anchor}`" for anchor in anchors)
            + f" | `{asset['interaction_kind']}` |"
        )

    silhouette = checkpoints["silhouette"]
    assert isinstance(silhouette, Mapping)
    return {
        "BATCH_ID": str(config["batch_id"]),
        "BATCH_ID_REPR": repr(str(config["batch_id"])),
        "BATCH_SLUG": batch_slug,
        "PASCAL_BATCH": pascal_batch,
        "STAGE": str(config["stage"]),
        "STAGE_REPR": repr(str(config["stage"])),
        "CONFIG_JSON": json.dumps(config, indent=2, ensure_ascii=False),
        "ASSET_DEFINITIONS_REPR": repr(assets),
        "ASSET_BUILDERS": "\n\n".join(builders),
        "CSHARP_ANCHORS": "\n".join(csharp_anchors),
        "CSHARP_WRAPPERS": "\n".join(csharp_wrappers),
        "RECORD_ASSET_ROWS": "\n".join(record_rows),
        "SILHOUETTE_STATUS": str(silhouette.get("status", "pending")),
        "SILHOUETTE_STATUS_REPR": repr(str(silhouette.get("status", "pending"))),
        "CANDIDATE_ROOT": str(paths["candidate_root"]).replace("\\", "/"),
        "FORMAL_MODEL_ROOT": str(paths["formal_model_root"]).replace("\\", "/"),
        "WRAPPER_ROOT": str(paths["wrapper_root"]).replace("\\", "/"),
        "JSON_MANIFEST": str(paths["json_manifest"]).replace("\\", "/"),
    }


def render_outputs(config: Mapping[str, object], template_root: Path | str) -> dict[Path, str]:
    """Render all batch files in memory; returned keys are repository-relative."""

    errors = validate_config(config)
    if errors:
        raise ValueError("; ".join(errors))
    root = Path(template_root)
    values = _template_values(config)
    relative = _relative_outputs(config)
    rendered: dict[Path, str] = {}
    template_by_output = {
        relative[0]: TEMPLATE_FILES["contract"],
        relative[1]: TEMPLATE_FILES["generator"],
        relative[2]: TEMPLATE_FILES["renderer"],
        relative[3]: TEMPLATE_FILES["contract_test"],
        relative[4]: TEMPLATE_FILES["integration"],
        relative[6]: TEMPLATE_FILES["capture"],
        relative[8]: TEMPLATE_FILES["record"],
    }
    for output, template_name in template_by_output.items():
        source = (root / template_name).read_text(encoding="utf-8")
        rendered[output] = Template(source).substitute(values)

    rendered[relative[5]] = (
        "[gd_scene load_steps=3 format=3]\n\n"
        f"[ext_resource type=\"Script\" path=\"res://{relative[4].as_posix()}\" id=\"1_test\"]\n"
        "[ext_resource type=\"PackedScene\" path=\"res://scenes/Main.tscn\" id=\"2_main\"]\n\n"
        f"[node name=\"{values['PASCAL_BATCH']}AssetIntegrationTests\" type=\"Node\"]\n"
        "script = ExtResource(\"1_test\")\n\n"
        "[node name=\"Main\" parent=\".\" instance=ExtResource(\"2_main\")]\n"
    )
    rendered[relative[7]] = (
        "[gd_scene load_steps=3 format=3]\n\n"
        f"[ext_resource type=\"Script\" path=\"res://{relative[6].as_posix()}\" id=\"1_capture\"]\n"
        "[ext_resource type=\"PackedScene\" path=\"res://scenes/Main.tscn\" id=\"2_main\"]\n\n"
        f"[node name=\"{values['PASCAL_BATCH']}AssetVisualCapture\" type=\"Node\"]\n"
        "script = ExtResource(\"1_capture\")\n\n"
        "[node name=\"Main\" parent=\".\" instance=ExtResource(\"2_main\")]\n"
    )
    return {path: rendered[path] for path in relative}


def _write_atomically(project_root: Path, rendered: Mapping[Path, str]) -> None:
    moved: list[Path] = []
    with tempfile.TemporaryDirectory(prefix=".asset-batch-", dir=project_root.parent) as temporary:
        staging = Path(temporary)
        for relative, content in rendered.items():
            staged = staging / relative
            staged.parent.mkdir(parents=True, exist_ok=True)
            staged.write_text(content, encoding="utf-8", newline="\n")
        try:
            for relative in rendered:
                destination = (project_root / relative).resolve()
                if not destination.is_relative_to(project_root):
                    raise ValueError(f"destination escapes project root: {relative}")
                destination.parent.mkdir(parents=True, exist_ok=True)
                if destination.exists():
                    raise FileExistsError(f"destination appeared during scaffold: {destination}")
                (staging / relative).replace(destination)
                moved.append(destination)
        except Exception:
            for destination in reversed(moved):
                destination.unlink(missing_ok=True)
            raise


def _load_config(path: Path) -> Mapping[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, Mapping):
        raise ValueError("configuration root must be a JSON object")
    return value


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Create an unapproved Glasses Bar asset-batch skeleton without running Blender or Godot."
    )
    parser.add_argument("--config", required=True, type=Path, help="Batch JSON configuration")
    parser.add_argument("--project-root", required=True, type=Path, help="Repository root")
    parser.add_argument("--dry-run", action="store_true", help="List outputs without writing them")
    args = parser.parse_args()

    try:
        config = _load_config(args.config)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"ERROR CONFIG_READ: {error}")
        return 2

    errors = validate_config(config)
    if errors:
        for error in errors:
            print(f"ERROR CONFIG_INVALID: {error}")
        return 2

    project_root = args.project_root.resolve()
    if not project_root.is_dir():
        print(f"ERROR PROJECT_ROOT_INVALID: {project_root}")
        return 2
    outputs = planned_outputs(config, project_root)
    conflicts = sorted(path for path in outputs if path.exists())
    if conflicts:
        for path in conflicts:
            print(f"ERROR OUTPUT_EXISTS: {path}")
        return 3

    for path in outputs:
        print(f"PLAN {path}")
    if args.dry_run:
        return 0

    template_root = Path(__file__).resolve().parents[1] / "assets" / "templates"
    try:
        rendered = render_outputs(config, template_root)
        _write_atomically(project_root, rendered)
    except (OSError, KeyError, ValueError) as error:
        print(f"ERROR SCAFFOLD_FAILED: {error}")
        return 4

    print(f"SUMMARY batch={config['batch_id']} created={len(outputs)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
