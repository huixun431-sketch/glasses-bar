#!/usr/bin/env python3
"""Contract test for the deterministic modular bar Blender scene."""

from __future__ import annotations

import sys
from pathlib import Path

import bpy
from mathutils import Vector


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
BLENDER_TOOLS = REPOSITORY_ROOT / "tools" / "blender"
if str(BLENDER_TOOLS) not in sys.path:
    sys.path.insert(0, str(BLENDER_TOOLS))

from bar_model_common import BAR_METRICS, MODULE_NAMES  # noqa: E402
from build_bar_master import build_master_scene  # noqa: E402


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def near_vector(actual, expected, tolerance: float = 0.001) -> bool:
    return all(abs(float(a) - float(b)) <= tolerance for a, b in zip(actual, expected))


def object_world_bounds(name: str) -> tuple[Vector, Vector]:
    obj = bpy.data.objects.get(name)
    require(obj is not None, f"missing object {name}")
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector(tuple(min(point[index] for point in points) for index in range(3))),
        Vector(tuple(max(point[index] for point in points) for index in range(3))),
    )


def run() -> None:
    build_master_scene()
    scene = bpy.context.scene
    require(scene.unit_settings.system == "METRIC", "scene must use metric units")
    require(abs(scene.unit_settings.scale_length - 1.0) <= 1e-6, "one Blender unit must equal one metre")
    require(scene.get("export_up_axis") == "+Y", "export up axis must be +Y")
    require(scene.get("export_forward_axis") == "-Z", "export forward axis must be -Z")

    for module_name in MODULE_NAMES:
        collection = bpy.data.collections.get(module_name)
        require(collection is not None, f"missing module collection {module_name}")
        root = bpy.data.objects.get(module_name)
        require(root is not None and root.name in collection.objects, f"missing export root {module_name}")
        require(near_vector(root.location, (0.0, 0.0, 0.0)), f"{module_name} root translation is not identity")
        require(near_vector(root.rotation_euler, (0.0, 0.0, 0.0)), f"{module_name} root rotation is not identity")
        require(near_vector(root.scale, (1.0, 1.0, 1.0)), f"{module_name} root scale is not identity")

    names = [obj.name for obj in bpy.data.objects]
    require(len(names) == len(set(names)), "duplicate stable object names are forbidden")

    room_min, room_max = object_world_bounds("room_shell")
    require(near_vector(room_min, (-8.0, 0.0, -5.0)), f"room minimum is wrong: {tuple(room_min)}")
    require(near_vector(room_max, (8.0, 4.5, 5.0)), f"room maximum is wrong: {tuple(room_max)}")

    expected_openings = {
        "south_main_entry": ((-0.65, 1.05, 5.0), (1.40, 2.10, 0.20)),
        "south_east_window": ((4.35, 1.525, 5.0), (3.20, 1.55, 0.20)),
        "north_east_service_door": ((6.90, 1.05, -5.0), (0.90, 2.10, 0.20)),
    }
    for name, (location, size) in expected_openings.items():
        opening = bpy.data.objects.get(name)
        require(opening is not None, f"missing stable opening {name}")
        require(near_vector(opening.location, location), f"{name} location is wrong")
        require(near_vector(opening.dimensions, size), f"{name} size is wrong")

    require(BAR_METRICS["room_size"] == (16.0, 10.0, 4.5), "metric table drifted from Z3/H3")
    print("BAR_MODEL_CONTRACT_PASS")


if __name__ == "__main__":
    run()
