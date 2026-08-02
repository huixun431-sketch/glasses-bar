#!/usr/bin/env python3
"""Generate review silhouettes or final GLBs for the stage-two hand tools.

Run with Blender 4.5.5 LTS:
  blender --background --python tools/modeling/generate_stage2_assets.py -- \
    --mode silhouette --output artifacts/stage2_checkpoint1/models

  blender --background --python tools/modeling/generate_stage2_assets.py -- \
    --mode final --output assets/models
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
    parser.add_argument("--mode", choices=("silhouette", "final"), required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def make_neutral_materials() -> dict[str, bpy.types.Material]:
    """Create the one intentionally non-final material for silhouette review."""
    metal = make_material(
        "Neutral_Silhouette_Metal",
        (0.46, 0.49, 0.52, 1.0),
        metallic=0.46,
        roughness=0.44,
    )
    return {"metal": metal, "edge": metal}


FINAL_MATERIALS = {
    "warm_brushed": {
        "color": (0.46, 0.29, 0.10, 1.0),
        "metallic": 0.76,
        "roughness": 0.34,
    },
    "dark_satin": {
        "color": (0.025, 0.035, 0.05, 1.0),
        "metallic": 0.08,
        "roughness": 0.68,
    },
    "bright_silver": {
        "color": (0.58, 0.72, 0.88, 1.0),
        "metallic": 0.18,
        "roughness": 0.20,
        "coat_weight": 0.38,
        "coat_roughness": 0.06,
    },
}


def make_final_materials(asset_id: str) -> dict[str, bpy.types.Material]:
    """Create the approved mixed-C material group for one final asset."""
    contract = STAGE2_ASSETS[asset_id]
    settings = FINAL_MATERIALS[contract.material_group]
    metal = make_material(
        f"Stage2_{contract.material_group}",
        settings["color"],
        metallic=settings["metallic"],
        roughness=settings["roughness"],
        coat_weight=settings.get("coat_weight", 0.0),
        coat_roughness=settings.get("coat_roughness", 0.2),
    )
    if contract.material_group != "bright_silver":
        return {"metal": metal, "edge": metal}

    edge = make_material(
        "Worn_Silver_Edge",
        (0.92, 0.95, 0.98, 1.0),
        metallic=0.82,
        roughness=0.10,
        coat_weight=0.32,
        coat_roughness=0.05,
    )
    return {"metal": metal, "edge": edge}


def add_mesh_object(
    root: bpy.types.Object,
    name: str,
    vertices: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material: bpy.types.Material,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = root
    return obj


def add_shallow_scoop_bowl(
    root: bpy.types.Object,
    material: bpy.types.Material,
) -> tuple[bpy.types.Object, bpy.types.Object]:
    """Add a wide, shallow 12-sided loading bowl with a low front lip."""
    segments = 12
    center_y = 0.012
    outer_x_radius = 0.060
    outer_y_radius = 0.078
    inner_x_radius = 0.051
    inner_y_radius = 0.067
    vertices: list[tuple[float, float, float]] = [(0.0, center_y, 0.006)]
    for index in range(segments):
        angle = math.tau * index / segments
        front_weight = (math.sin(angle) + 1.0) / 2.0
        rim_z = 0.040 - 0.016 * front_weight
        vertices.append((
            math.cos(angle) * outer_x_radius,
            center_y + math.sin(angle) * outer_y_radius,
            rim_z,
        ))
    inner_center_index = len(vertices)
    vertices.append((0.0, center_y, 0.013))
    inner_ring_start = len(vertices)
    for index in range(segments):
        angle = math.tau * index / segments
        front_weight = (math.sin(angle) + 1.0) / 2.0
        rim_z = 0.035 - 0.015 * front_weight
        vertices.append((
            math.cos(angle) * inner_x_radius,
            center_y + math.sin(angle) * inner_y_radius,
            rim_z,
        ))

    faces: list[tuple[int, ...]] = []
    for index in range(segments):
        outer_current = 1 + index
        outer_next = 1 + (index + 1) % segments
        inner_current = inner_ring_start + index
        inner_next = inner_ring_start + (index + 1) % segments
        faces.append((0, outer_next, outer_current))
        faces.append((outer_current, outer_next, inner_next, inner_current))
        faces.append((inner_center_index, inner_current, inner_next))
    bowl = add_mesh_object(root, "ShallowScoopBowl", vertices, faces, material)

    lip_angles = [math.radians(angle) for angle in (35, 62, 90, 118, 145)]
    lip_vertices: list[tuple[float, float, float]] = []
    for radius_x, radius_y, z in (
        (outer_x_radius, outer_y_radius, 0.026),
        (inner_x_radius, inner_y_radius, 0.021),
    ):
        for angle in lip_angles:
            lip_vertices.append((
                math.cos(angle) * radius_x,
                center_y + math.sin(angle) * radius_y,
                z,
            ))
    lip_faces = [
        (index, index + 1, len(lip_angles) + index + 1, len(lip_angles) + index)
        for index in range(len(lip_angles) - 1)
    ]
    lip = add_mesh_object(root, "LoadingLip", lip_vertices, lip_faces, material)
    return bowl, lip


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


def add_metal_strip(
    root: bpy.types.Object,
    name: str,
    points: list[tuple[float, float, float]],
    width: float,
    thickness: float,
    material: bpy.types.Material,
) -> bpy.types.Object:
    """Create a thin rectangular metal strip following a planar polyline."""
    vertices: list[tuple[float, float, float]] = []
    for index, point in enumerate(points):
        previous = points[max(0, index - 1)]
        following = points[min(len(points) - 1, index + 1)]
        tangent_x = following[0] - previous[0]
        tangent_y = following[1] - previous[1]
        tangent_length = math.hypot(tangent_x, tangent_y)
        side_x = -tangent_y / tangent_length
        side_y = tangent_x / tangent_length
        for side_sign, z_sign in ((-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0)):
            vertices.append((
                point[0] + side_x * width * 0.5 * side_sign,
                point[1] + side_y * width * 0.5 * side_sign,
                point[2] + thickness * 0.5 * z_sign,
            ))

    faces: list[tuple[int, ...]] = [(3, 2, 1, 0)]
    for row in range(len(points) - 1):
        start = row * 4
        next_start = (row + 1) * 4
        for side in range(4):
            nxt = (side + 1) % 4
            faces.append((start + side, start + nxt, next_start + nxt, next_start + side))
    last_start = (len(points) - 1) * 4
    faces.append(tuple(last_start + index for index in range(4)))
    return add_mesh_object(root, name, vertices, faces, material)


def add_pickup_spoon(
    root: bpy.types.Object,
    name: str,
    center: tuple[float, float, float],
    material: bpy.types.Material,
) -> bpy.types.Object:
    """Create a closed, concave oval spoon head that reads from either side."""
    segments = 12
    radius_x = 0.017
    radius_y = 0.040
    vertices = [(0.0, 0.0, -0.008)]
    for index in range(segments):
        angle = math.tau * index / segments
        vertices.append((
            math.cos(angle) * radius_x,
            math.sin(angle) * radius_y,
            0.004,
        ))
    outer_center_index = len(vertices)
    vertices.append((0.0, 0.0, -0.012))
    outer_ring_start = len(vertices)
    for index in range(segments):
        angle = math.tau * index / segments
        vertices.append((
            math.cos(angle) * radius_x,
            math.sin(angle) * radius_y,
            0.001,
        ))

    faces: list[tuple[int, ...]] = [
        (0, 1 + index, 1 + (index + 1) % segments)
        for index in range(segments)
    ]
    for index in range(segments):
        inner_current = 1 + index
        inner_next = 1 + (index + 1) % segments
        outer_current = outer_ring_start + index
        outer_next = outer_ring_start + (index + 1) % segments
        faces.append((outer_center_index, outer_next, outer_current))
        faces.append((inner_current, outer_current, outer_next, inner_next))
    spoon = add_mesh_object(root, name, vertices, faces, material)
    spoon.location = center
    return spoon


def build_traditional_filter(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("traditional_filter")
    metal = materials["metal"]
    add_frustum_shell(root, "FilterCup", 0.078, 0.142, 0.104, 0.122, 0.094, 0.110, metal,
                      close_bottom=False, close_top=False)
    add_frustum_shell(root, "FunnelBasket", 0.028, 0.128, 0.024, 0.108, 0.010, 0.094, metal,
                      close_bottom=False, close_top=False)
    add_torus(root, "FunnelShoulderRing", 0.092, 0.005, 0.079, metal)
    add_torus(root, "FunnelDepthRing", 0.055, 0.004, 0.070, metal)
    add_torus(root, "OutletThroat", 0.016, 0.004, 0.031, metal)
    add_oriented_cylinder(root, "HandleBridgeLower", 0.009, 0.080, (0.135, 0.0, 0.090),
                          (0.0, math.pi / 2, 0.0), metal)
    add_oriented_cylinder(root, "HandleBridgeUpper", 0.009, 0.080, (0.135, 0.0, 0.126),
                          (0.0, math.pi / 2, 0.0), metal)
    grip_handle = add_cylinder(root, "GripHandle", 0.012, 0.077, 0.100, metal, vertices=8)
    grip_handle.location.x = 0.170
    add_cylinder(root, "BottomSpout", 0.018, 0.036, 0.018, metal, vertices=12)
    add_torus(root, "StabilityFoot", 0.044, 0.008, 0.023, metal)
    add_anchor(root, "Grip", (0.170, 0.100, 0.0))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "Spout", (0.0, 0.018, -0.018))
    add_anchor(root, "Interaction", (0.0, 0.080, 0.0))
    return root


def build_bean_scoop(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("bean_scoop")
    metal = materials["metal"]
    add_shallow_scoop_bowl(root, metal)
    add_oriented_cylinder(root, "ShortHandle", 0.010, 0.130, (0.0, -0.120, 0.032),
                          (math.pi / 2, 0.0, 0.0), metal)
    add_oriented_cylinder(root, "GripTab", 0.018, 0.012, (0.0, -0.191, 0.032),
                          (math.pi / 2, 0.0, 0.0), metal, vertices=8)
    add_anchor(root, "Grip", (0.0, 0.032, 0.165))
    add_anchor(root, "Placement", (0.0, 0.0, 0.0))
    add_anchor(root, "FillOrigin", (0.0, 0.018, -0.012))
    return root


def build_ice_tongs(materials: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_root("ice_tongs")
    metal = materials["metal"]
    left_strip_points = [
        (-0.016, -0.185, 0.030),
        (-0.018, -0.100, 0.030),
        (-0.026, 0.040, 0.030),
        (-0.032, 0.166, 0.030),
    ]
    right_strip_points = [(-x, y, z) for x, y, z in left_strip_points]
    spring_points = [
        left_strip_points[0],
        (-0.016, -0.207, 0.030),
        (0.0, -0.222, 0.030),
        (0.016, -0.207, 0.030),
        right_strip_points[0],
    ]
    left_jaw_center = (-0.032, 0.185, 0.030)
    right_jaw_center = (0.032, 0.185, 0.030)
    add_metal_strip(root, "LeftStrip", left_strip_points, 0.012, 0.004, metal)
    add_metal_strip(root, "RightStrip", right_strip_points, 0.012, 0.004, metal)
    add_metal_strip(root, "SpringBridge", spring_points, 0.008, 0.004, metal)
    add_pickup_spoon(root, "LeftPickupSpoon", left_jaw_center, metal)
    add_pickup_spoon(root, "RightPickupSpoon", right_jaw_center, metal)
    jaw_midpoint = tuple(
        (left_coordinate + right_coordinate) / 2.0
        for left_coordinate, right_coordinate in zip(left_jaw_center, right_jaw_center)
    )
    add_anchor(root, "Grip", (0.0, 0.030, 0.0))
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
    edge = materials["edge"]
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
              0.003 * z_scale, edge)
    add_torus(root, "UpperRim", 0.062 * radial_scale, 0.003 * radial_scale,
              0.177 * z_scale, edge)
    add_torus(root, "WaistBand", 0.024 * radial_scale, 0.003 * radial_scale,
              0.094 * z_scale, edge)
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


def write_review_manifest(
    manifest_path: Path,
    model_prefix: str,
) -> Path:
    manifest = {
        "units": "meters",
        "up_axis": "+Y",
        "forward_axis": "-Z",
        "assets": review_manifest_assets(model_prefix),
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
        materials = (
            make_neutral_materials()
            if args.mode == "silhouette"
            else make_final_materials(asset_id)
        )
        root = builder(materials)
        if root.name != asset_id or asset_id not in STAGE2_ASSETS:
            raise ValueError(f"invalid stage-two builder {asset_id}")
        export_asset(root, output_root / f"{asset_id}.glb")
    if args.mode == "silhouette":
        write_review_manifest(output_root.parent / "review_manifest.json", "models")
    else:
        repository_root = SCRIPT_DIRECTORY.parent.parent
        write_review_manifest(
            repository_root / "artifacts" / "stage2_final_candidate" / "review_manifest.json",
            "../../assets/models",
        )


if __name__ == "__main__":
    main()
