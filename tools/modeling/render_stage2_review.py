#!/usr/bin/env python3
"""Render checkpoint-one neutral silhouette reviews for stage-two hand tools.

Run with Blender 4.5.5 LTS:
  blender --background --python tools/modeling/render_stage2_review.py -- \
    --candidates artifacts/stage2_checkpoint1/models \
    --stage1 assets/models --output artifacts/stage2_checkpoint1
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector


REVIEW_ASSETS = (
    "traditional_filter", "bean_scoop", "ice_tongs", "jigger_small", "jigger_large"
)

LINEUP_X = {
    "traditional_filter": -0.82,
    "bean_scoop": -0.40,
    "ice_tongs": 0.00,
    "jigger_small": 0.38,
    "jigger_large": 0.70,
}


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidates", type=Path, required=True)
    parser.add_argument("--stage1", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def material(name: str, color: tuple[float, float, float, float], roughness: float) -> bpy.types.Material:
    result = bpy.data.materials.new(name)
    result.diffuse_color = color
    result.use_nodes = True
    shader = next(node for node in result.node_tree.nodes if node.bl_idname == "ShaderNodeBsdfPrincipled")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = roughness
    return result


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_studio() -> None:
    """Use the same neutral review studio as the approved stage-one renderer."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_mode = "RGBA"
    scene.world.color = (0.018, 0.024, 0.034)
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 0.4

    ground_material = material("ReviewGround", (0.055, 0.070, 0.085, 1.0), 0.72)
    bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, -0.003))
    ground = bpy.context.object
    ground.name = "ReviewGround"
    ground.data.materials.append(ground_material)

    light_specs = (
        ("WarmKey", (1.7, -1.7, 2.4), 720.0, (1.0, 0.72, 0.48), 1.8),
        ("CoolFill", (-1.6, -0.7, 1.45), 440.0, (0.44, 0.70, 1.0), 1.6),
        ("Rim", (0.2, 1.5, 2.0), 620.0, (1.0, 0.48, 0.24), 1.2),
    )
    for name, location, energy, color, size in light_specs:
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.color = color
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(light)
        light.location = location
        look_at(light, (0, 0, 0.12))


def add_camera(location: tuple[float, float, float], target: tuple[float, float, float], lens: float) -> None:
    data = bpy.data.cameras.new("ReviewCamera")
    data.lens = lens
    camera = bpy.data.objects.new("ReviewCamera", data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = location
    look_at(camera, target)
    bpy.context.scene.camera = camera


def import_asset(input_root: Path, asset_id: str) -> bpy.types.Object:
    asset_path = input_root / f"{asset_id}.glb"
    if not asset_path.is_file():
        raise FileNotFoundError(f"Missing review asset: {asset_path}")
    bpy.ops.import_scene.gltf(filepath=str(asset_path))
    root = bpy.data.objects.get(asset_id)
    if root is None:
        raise RuntimeError(f"Imported GLB has no {asset_id} root")
    return root


def add_jigger_scale() -> None:
    """Add a background ruler with 0.05 m marks for the family-size comparison."""
    scale_material = material("JiggerScale", (0.42, 0.58, 0.70, 1.0), 0.38)
    x, y = 0.37, 0.12
    bpy.ops.mesh.primitive_cube_add(location=(x, y, 0.125), scale=(0.006, 0.006, 0.125))
    spine = bpy.context.object
    spine.name = "JiggerScaleSpine"
    spine.data.materials.append(scale_material)
    for index in range(6):
        z = index * 0.05
        length = 0.034 if index % 2 == 0 else 0.020
        bpy.ops.mesh.primitive_cube_add(location=(x - length / 2, y, z), scale=(length / 2, 0.004, 0.004))
        tick = bpy.context.object
        tick.name = f"JiggerScaleTick_{index * 5:02d}cm"
        tick.data.materials.append(scale_material)


def render_lineup(candidate_root: Path, output_root: Path, three_quarter: bool) -> None:
    reset_scene()
    add_studio()
    lineup_scale = 1.0 if three_quarter else 0.78
    for asset_id in REVIEW_ASSETS:
        root = import_asset(candidate_root, asset_id)
        root.location.x = LINEUP_X[asset_id] * lineup_scale
    camera = (1.15, -2.55, 0.78) if three_quarter else (0.0, -2.85, 0.65)
    add_camera(camera, (-0.04, 0.0, 0.13), 62.0)
    scene = bpy.context.scene
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 900
    suffix = "three_quarter" if three_quarter else "front"
    scene.render.filepath = str(output_root / f"stage2_lineup_{suffix}.png")
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")


def render_jigger_family(candidate_root: Path, stage1_root: Path, output_root: Path) -> None:
    reset_scene()
    add_studio()
    for asset_id, root_path, x in (
        ("jigger_small", candidate_root, -0.22),
        ("jigger_medium", stage1_root, 0.0),
        ("jigger_large", candidate_root, 0.24),
    ):
        root = import_asset(root_path, asset_id)
        root.location.x = x
    add_jigger_scale()
    add_camera((0.62, -1.35, 0.46), (0.0, 0.0, 0.10), 66.0)
    scene = bpy.context.scene
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.filepath = str(output_root / "jigger_family.png")
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")


def main() -> None:
    args = parse_args()
    candidate_root = args.candidates.resolve()
    stage1_root = args.stage1.resolve()
    output_root = args.output.resolve()
    output_root.mkdir(parents=True, exist_ok=True)
    render_lineup(candidate_root, output_root, three_quarter=False)
    render_lineup(candidate_root, output_root, three_quarter=True)
    render_jigger_family(candidate_root, stage1_root, output_root)


if __name__ == "__main__":
    main()
