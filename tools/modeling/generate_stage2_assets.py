#!/usr/bin/env python3
"""Generate checkpoint-one neutral silhouettes for the stage-two hand tools.

Run with Blender 4.5.5 LTS:
  blender --background --python tools/modeling/generate_stage2_assets.py -- \
    --mode silhouette --output artifacts/stage2_checkpoint1/models
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from generate_stage1_assets import (
    add_anchor,
    add_cylinder,
    add_frustum_shell,
    add_root,
    add_torus,
    export_asset,
    make_material,
    reset_scene,
)
from stage2_asset_contract import STAGE2_ASSETS, review_manifest_assets


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=("silhouette",), required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def make_neutral_materials() -> dict[str, bpy.types.Material]:
    """Create one intentionally non-final material for a silhouette review."""
    metal = make_material(
        "Neutral_Silhouette_Metal",
        (0.46, 0.49, 0.52, 1.0),
        metallic=0.46,
        roughness=0.44,
    )
    return {"metal": metal}


def add_open_scoop_bowl(
    root: bpy.types.Object,
    material: bpy.types.Material,
) -> bpy.types.Object:
    """Add a 12-sided open scoop shell, with its opening facing upward."""
    segments = 12
    bottom_z = 0.020
    top_z = 0.082
    outer_bottom = 0.037
    outer_top = 0.062
    inner_bottom = 0.030
    inner_top = 0.055
    vertices: list[tuple[float, float, float]] = []
    for radius, z in ((outer_bottom, bottom_z), (outer_top, top_z), (inner_bottom, bottom_z), (inner_top, top_z)):
        for index in range(segments):
            angle = math.tau * index / segments
            vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, z))

    faces: list[tuple[int, int, int, int]] = []
    outer_bottom_start = 0
    outer_top_start = segments
    inner_bottom_start = segments * 2
    inner_top_start = segments * 3
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((outer_bottom_start + index, outer_bottom_start + nxt, outer_top_start + nxt, outer_top_start + index))
        faces.append((inner_bottom_start + nxt, inner_bottom_start + index, inner_top_start + index, inner_top_start + nxt))
        faces.append((outer_bottom_start + nxt, outer_bottom_start + index, inner_bottom_start + index, inner_bottom_start + nxt))

    mesh = bpy.data.meshes.new("ScoopBowl_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    bowl = bpy.data.objects.new("ScoopBowl", mesh)
    bpy.context.scene.collection.objects.link(bowl)
    bowl.parent = root
    return bowl


def add_oriented_cylinder(
    root: bpy.types.Object,
    name: str,
    radius: float,
    depth: float,
    location: tuple[float, float, float],
    rotation: tuple[float, float, float],
    material: bpy.types.Material,
    *,
    vertices: int = 8,
) -> bpy.types.Object:
    """Use the stage-one cylinder helper and orient it for a simple silhouette."""
    part = add_cylinder(root, name, radius, depth, 0.0, material, vertices=vertices)
    part.location = location
    part.rotation_euler = rotation
    return part


def add_u_spring(
    root: bpy.types.Object,
    name: str,
    left_join: tuple[float, float, float],
    right_join: tuple[float, float, float],
    material: bpy.types.Material,
) -> bpy.types.Object:
    """Create an open, low-poly U spring whose ends attach to the tong arms."""
    curve_segments = 6
    tube_segments = 6
    tube_radius = 0.013
    center_x = (left_join[0] + right_join[0]) / 2.0
    center_y = (left_join[1] + right_join[1]) / 2.0
    center_z = (left_join[2] + right_join[2]) / 2.0
    curve_radius = (right_join[0] - left_join[0]) / 2.0
    points = [
        (
            center_x + curve_radius * math.cos(math.pi - math.pi * index / curve_segments),
            center_y - curve_radius * math.sin(math.pi - math.pi * index / curve_segments),
            center_z,
        )
        for index in range(curve_segments + 1)
    ]

    vertices: list[tuple[float, float, float]] = []
    for index, point in enumerate(points):
        previous = points[max(0, index - 1)]
        following = points[min(len(points) - 1, index + 1)]
        tangent_x = following[0] - previous[0]
        tangent_y = following[1] - previous[1]
        tangent_length = math.hypot(tangent_x, tangent_y)
        side_x = -tangent_y / tangent_length
        side_y = tangent_x / tangent_length
        for tube_index in range(tube_segments):
            angle = math.tau * tube_index / tube_segments
            vertices.append((
                point[0] + tube_radius * math.cos(angle) * side_x,
                point[1] + tube_radius * math.cos(angle) * side_y,
                point[2] + tube_radius * math.sin(angle),
            ))

    faces: list[tuple[int, ...]] = [tuple(reversed(range(tube_segments)))]
    for row in range(curve_segments):
        start = row * tube_segments
        next_start = (row + 1) * tube_segments
        for index in range(tube_segments):
            nxt = (index + 1) % tube_segments
            faces.append((start + index, start + nxt, next_start + nxt, next_start + index))
    last_start = curve_segments * tube_segments
    faces.append(tuple(last_start + index for index in range(tube_segments)))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    spring = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(spring)
    spring.parent = root
    return spring


def build_traditional_filter(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("traditional_filter")
    metal = materials["metal"]
    add_frustum_shell(root, "FilterCup", 0.040, 0.142, 0.126, 0.102, 0.110, 0.086, metal,
                      close_bottom=True, close_top=False)
    add_cylinder(root, "FilterMeshDisc", 0.083, 0.006, 0.052, metal, vertices=16)
    add_oriented_cylinder(root, "SideHandle", 0.016, 0.125, (0.157, 0.0, 0.108),
                          (0.0, math.pi / 2, 0.0), metal)
    add_cylinder(root, "BottomSpout", 0.020, 0.052, 0.020, metal, vertices=12)
    add_torus(root, "StabilityFoot", 0.090, 0.009, 0.034, metal)
    add_anchor(root, "Grip", (0.16, 0.11, 0.0))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "Spout", (0.0, 0.02, -0.02))
    add_anchor(root, "Interaction", (0.0, 0.09, 0.0))
    return root


def build_bean_scoop(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("bean_scoop")
    metal = materials["metal"]
    add_open_scoop_bowl(root, metal)
    add_oriented_cylinder(root, "ShortHandle", 0.019, 0.155, (0.0, -0.115, 0.058),
                          (math.pi / 2, 0.0, 0.0), metal)
    add_oriented_cylinder(root, "GripTab", 0.030, 0.018, (0.0, -0.195, 0.058),
                          (math.pi / 2, 0.0, 0.0), metal, vertices=12)
    add_anchor(root, "Grip", (0.0, 0.06, 0.19))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "FillOrigin", (0.0, 0.050, 0.0))
    return root


def build_ice_tongs(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("ice_tongs")
    metal = materials["metal"]
    left_arm_center = (-0.025, 0.0, 0.030)
    right_arm_center = (0.025, 0.0, 0.030)
    arm_length = 0.340
    left_jaw_center = (-0.042, 0.175, 0.026)
    right_jaw_center = (0.042, 0.175, 0.026)
    add_oriented_cylinder(root, "LeftArm", 0.013, arm_length, left_arm_center,
                          (math.pi / 2, -0.11, 0.0), metal)
    add_oriented_cylinder(root, "RightArm", 0.013, arm_length, right_arm_center,
                          (math.pi / 2, 0.11, 0.0), metal)
    left_spring_join = (left_arm_center[0], left_arm_center[1] - arm_length / 2.0, left_arm_center[2])
    right_spring_join = (right_arm_center[0], right_arm_center[1] - arm_length / 2.0, right_arm_center[2])
    add_u_spring(root, "SpringU", left_spring_join, right_spring_join, metal)
    add_oriented_cylinder(root, "LeftJaw", 0.022, 0.050, left_jaw_center,
                          (math.pi / 2, 0.0, 0.0), metal, vertices=6)
    add_oriented_cylinder(root, "RightJaw", 0.022, 0.050, right_jaw_center,
                          (math.pi / 2, 0.0, 0.0), metal, vertices=6)
    jaw_midpoint = tuple(
        (left_coordinate + right_coordinate) / 2.0
        for left_coordinate, right_coordinate in zip(left_jaw_center, right_jaw_center)
    )
    add_anchor(root, "Grip", (0.0, 0.04, -0.14))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "Interaction", (jaw_midpoint[0], jaw_midpoint[2], -jaw_midpoint[1]))
    return root


def build_jigger_variant(
    asset_id: str,
    height: float,
    target_radius: float,
    materials: dict[str, bpy.types.Material],
) -> bpy.types.Object:
    root = add_root(asset_id)
    metal = materials["metal"]
    z_scale = height / 0.18
    radial_scale = target_radius / 0.065
    add_frustum_shell(root, "LowerCup", 0.0, 0.078 * z_scale,
                      0.055 * radial_scale, 0.023 * radial_scale,
                      0.049 * radial_scale, 0.018 * radial_scale, metal,
                      close_bottom=True, close_top=False)
    add_cylinder(root, "Waist", 0.023 * radial_scale, 0.032 * z_scale, 0.094 * z_scale, metal)
    add_frustum_shell(root, "UpperCup", 0.110 * z_scale, height,
                      0.023 * radial_scale, target_radius,
                      0.018 * radial_scale, 0.058 * radial_scale, metal,
                      close_bottom=False, close_top=True)
    add_torus(root, "LowerRim", 0.052 * radial_scale, 0.003 * radial_scale,
              0.003 * z_scale, metal)
    add_torus(root, "UpperRim", 0.062 * radial_scale, 0.003 * radial_scale,
              0.177 * z_scale, metal)
    add_torus(root, "WaistBand", 0.024 * radial_scale, 0.003 * radial_scale,
              0.094 * z_scale, metal)
    add_anchor(root, "Grip", (0.0, 0.09 * z_scale, 0.0))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "FillOrigin", (0.0, 0.105 * z_scale, 0.0))
    add_anchor(root, "Spout", (0.0, height, -target_radius))
    return root


BUILDERS = {
    "traditional_filter": build_traditional_filter,
    "bean_scoop": build_bean_scoop,
    "ice_tongs": build_ice_tongs,
    "jigger_small": lambda materials: build_jigger_variant("jigger_small", 0.15, 0.055, materials),
    "jigger_large": lambda materials: build_jigger_variant("jigger_large", 0.21, 0.075, materials),
}


def write_review_manifest(output_root: Path) -> Path:
    manifest_path = output_root.parent / "review_manifest.json"
    manifest = {
        "units": "meters",
        "up_axis": "+Y",
        "forward_axis": "-Z",
        "assets": review_manifest_assets("models"),
    }
    import json

    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"WROTE {manifest_path}")
    return manifest_path


def main() -> None:
    args = parse_args()
    output_root = args.output.resolve()
    for asset_id, builder in BUILDERS.items():
        reset_scene()
        root = builder(make_neutral_materials())
        if root.name != asset_id or asset_id not in STAGE2_ASSETS:
            raise ValueError(f"invalid stage-two builder {asset_id}")
        export_asset(root, output_root / f"{asset_id}.glb")
    write_review_manifest(output_root)


if __name__ == "__main__":
    main()
