#!/usr/bin/env python3
"""Render neutral studio review images for the first asset batch."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ASSETS = {
    "highball_glass": {"height": 0.25, "distance": 0.56},
    "jigger_medium": {"height": 0.18, "distance": 0.48},
    "mortar": {"height": 0.24, "distance": 0.82},
    "pestle": {"height": 0.42, "distance": 0.62},
}


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
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
    bpy.ops.import_scene.gltf(filepath=str(input_root / f"{asset_id}.glb"))
    root = bpy.data.objects.get(asset_id)
    if root is None:
        raise RuntimeError(f"Imported GLB has no {asset_id} root")
    return root


def render_individual(input_root: Path, output_root: Path, asset_id: str) -> None:
    reset_scene()
    add_studio()
    root = import_asset(input_root, asset_id)
    height = ASSETS[asset_id]["height"]
    distance = ASSETS[asset_id]["distance"]
    camera_height = max(0.24, height * 0.92)
    add_camera(
        (distance * 0.72, -distance, camera_height),
        (0, 0, height * 0.48),
        58.0,
    )
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.filepath = str(output_root / f"{asset_id}.png")
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")


def render_lineup(input_root: Path, output_root: Path) -> None:
    reset_scene()
    add_studio()
    positions = {
        "highball_glass": -0.75,
        "jigger_medium": -0.45,
        "mortar": 0.0,
        "pestle": 0.48,
    }
    for asset_id, x in positions.items():
        root = import_asset(input_root, asset_id)
        root.location.x = x

    add_camera((1.15, -2.55, 0.94), (-0.08, 0, 0.16), 64.0)
    scene = bpy.context.scene
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 900
    scene.render.filepath = str(output_root / "stage1_lineup.png")
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")


def render_pair(input_root: Path, output_root: Path) -> None:
    reset_scene()
    add_studio()
    mortar = import_asset(input_root, "mortar")
    mortar.location.x = -0.10
    pestle = import_asset(input_root, "pestle")
    pestle.rotation_euler = (math.radians(-14), math.radians(22), math.radians(-24))
    pestle.location = (0.12, 0.01, 0.14)
    add_camera((0.72, -0.92, 0.60), (0, 0, 0.17), 62.0)
    scene = bpy.context.scene
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1000
    scene.render.filepath = str(output_root / "mortar_pestle_pair.png")
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")


def main() -> None:
    args = parse_args()
    input_root = args.input.resolve()
    output_root = args.output.resolve()
    output_root.mkdir(parents=True, exist_ok=True)
    render_lineup(input_root, output_root)
    for asset_id in ASSETS:
        render_individual(input_root, output_root, asset_id)
    render_pair(input_root, output_root)


if __name__ == "__main__":
    main()
