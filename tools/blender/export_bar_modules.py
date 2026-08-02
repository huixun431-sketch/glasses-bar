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
}


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--module", choices=MODULE_NAMES, required=True)
    parser.add_argument("--mode", choices=("silhouette", "final"), required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def export_module(module_name: str, mode: str, output: Path) -> None:
    ensure_artifacts_ignored()
    if mode == "final":
        raise RuntimeError("Formal architecture export is gated by silhouette checkpoint approval")
    root = bpy.data.objects.get(module_name)
    if root is None:
        raise RuntimeError(f"Missing module root {module_name}")
    objects = [root, *descendants(root)]
    if not any(obj.type == "MESH" for obj in objects):
        raise RuntimeError(f"Module {module_name} has no visual mesh")
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(
        filepath=str(output),
        export_format="GLB",
        use_selection=True,
        export_yup=True,
        export_apply=False,
        export_cameras=False,
        export_lights=False,
    )
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
    export_module(args.module, args.mode, args.output)
