#!/usr/bin/env python3
"""Export one deterministic bar module as a GLB candidate or formal asset."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import bpy

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from bar_model_common import MODULE_NAMES, descendants, ensure_artifacts_ignored


REQUIRED_NODES = {
    "bar_architecture": [
        "room_shell", "south_main_entry", "south_east_window", "north_east_service_door"
    ],
    "bar_counter": [
        *[f"front_drawer_{bay}_{level}" for bay in range(1, 5) for level in ("upper", "lower")],
        "east_sink", "sink_plumbing", "waste_bin", "employee_gate", "manual_shelf",
    ],
    "bar_backbar": [
        *[f"rear_lower_cabinet_{bay}_{leaf}" for bay in range(1, 6) for leaf in ("fixed", "moving")],
        *[f"back_cabinet_{bay}_{leaf}" for bay in range(1, 6) for leaf in ("left", "right")],
        "bottle_rack_bay_1", "bottle_rack_bay_5",
    ],
    "bar_furniture": [
        "stool_1", "stool_6", "lounge_table_1", "lounge_table_3",
        "lounge_chair_1", "lounge_chair_12",
    ],
    "bar_lighting": [
        "pendant_1", "pendant_3", "rear_linear_1", "rear_linear_2",
        "east_sconce_1", "east_sconce_2", "west_sconce_1", "west_sconce_2",
    ],
    "bar_wear_overlays": ["wear_overlay_root"],
}

REQUIRED_ANCHORS = {
    "bar_architecture": ["Placement"],
    "bar_counter": ["Placement"],
}


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--module", choices=MODULE_NAMES, required=True)
    parser.add_argument("--mode", choices=("silhouette", "final"), required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--approval-config", type=Path)
    return parser.parse_args(argv)


def _require_final_approval(config_path: Path | None) -> None:
    if config_path is None or not config_path.is_file():
        raise RuntimeError("Formal export requires an existing approved batch config")
    config = json.loads(config_path.read_text(encoding="utf-8"))
    checkpoint = config.get("checkpoints", {}).get("silhouette", {})
    if (checkpoint.get("status") != "approved" or
            checkpoint.get("approved_by") != "user" or
            not str(checkpoint.get("approval_record", "")).strip()):
        raise RuntimeError("Formal export is gated by explicit user silhouette approval")


def export_module(
    module_name: str,
    mode: str,
    output: Path,
    approval_config: Path | None = None,
) -> None:
    ensure_artifacts_ignored()
    if mode == "final":
        _require_final_approval(approval_config)
    root = bpy.data.objects.get(module_name)
    if root is None:
        raise RuntimeError(f"Missing module root {module_name}")
    objects = [root, *descendants(root)]
    if not any(obj.type == "MESH" for obj in objects):
        raise RuntimeError(f"Module {module_name} has no visual mesh")
    output.parent.mkdir(parents=True, exist_ok=True)
    reserved_placement = bpy.data.objects.get("Placement") if module_name == "bar_counter" else None
    if reserved_placement is not None:
        reserved_placement.name = "_architecture_Placement_temp"
    alias = bpy.data.objects.get("bar_counter_Placement") if module_name == "bar_counter" else None
    original_alias_name = alias.name if alias is not None else None
    if alias is not None:
        alias.name = "Placement"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(
        filepath=str(output),
        export_format="GLB",
        use_selection=True,
        # The deterministic master is authored directly in project +Y-up
        # coordinates, so Blender's native Z-up conversion must stay disabled.
        export_yup=False,
        export_apply=False,
        export_cameras=False,
        export_lights=False,
    )
    if alias is not None and original_alias_name is not None:
        alias.name = original_alias_name
    if reserved_placement is not None:
        reserved_placement.name = "Placement"
    contract = {
        "version": 1,
        "units": "meters",
        "up_axis": "+Y",
        "forward_axis": "-Z",
        "assets": [{
            "id": module_name,
            "path": output.name,
            "placeholder": False,
            "required_root": module_name,
            "required_anchors": REQUIRED_ANCHORS.get(module_name, []),
            "required_nodes": REQUIRED_NODES.get(module_name, []),
        }],
    }
    output.with_suffix(".manifest.json").write_text(
        json.dumps(contract, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"BAR_MODULE_EXPORT_PASS module={module_name} mode={mode} output={output}")


if __name__ == "__main__":
    args = parse_args()
    export_module(args.module, args.mode, args.output, args.approval_config)
