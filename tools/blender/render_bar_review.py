#!/usr/bin/env python3
"""Render neutral architecture silhouette views from the deterministic master."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
REPOSITORY_ROOT = SCRIPT_DIRECTORY.parents[1]
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from bar_model_common import ensure_artifacts_ignored, look_at


VIEWS = (
    ("01_north_interior", (0.0, 1.90, 3.85), (1.4, 1.65, -4.75), 18.0, False),
    ("02_south_interior", (0.0, 1.90, -3.85), (1.8, 1.65, 4.75), 18.0, False),
    ("03_east_interior", (-6.65, 1.90, 0.0), (7.70, 1.65, 0.0), 18.0, False),
    ("04_west_interior", (6.65, 1.90, 0.0), (-7.70, 1.65, 0.0), 18.0, False),
    ("05_overhead", (0.0, 12.8, 0.0), (0.0, 0.0, 0.0), 50.0, True),
    ("06_interior_three_quarter", (5.8, 3.0, 3.8), (-1.0, 1.25, -2.6), 24.0, False),
)


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def add_review_lighting() -> None:
    for obj in list(bpy.data.objects):
        if obj.type in {"LIGHT", "CAMERA"}:
            bpy.data.objects.remove(obj, do_unlink=True)
    # Directional review lights avoid the hard rectangular projections that an
    # area light can cast through the south openings.  The world node supplies
    # the neutral fill; these lights only establish readable form and depth.
    light_specs = (
        ("ReviewKey", (4.5, 7.0, 3.5), 2.2, (1.0, 0.90, 0.80)),
        ("ReviewFill", (-4.0, 5.0, -3.0), 0.9, (0.76, 0.86, 1.0)),
    )
    for name, location, energy, color in light_specs:
        data = bpy.data.lights.new(name, "SUN")
        data.energy = energy
        data.color = color
        data.angle = 0.12
        data.use_shadow = False
        obj = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(obj)
        obj.location = location
        look_at(obj, (0.0, 0.8, 0.0))


def add_camera() -> bpy.types.Object:
    data = bpy.data.cameras.new("ArchitectureReviewCamera")
    data.lens = 50.0
    data.clip_start = 0.05
    data.clip_end = 100.0
    camera = bpy.data.objects.new("ArchitectureReviewCamera", data)
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return camera


def configure_render() -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.035, 0.038, 0.043, 1.0)
    background.inputs["Strength"].default_value = 0.32
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.20


def render_views(output: Path) -> None:
    ensure_artifacts_ignored()
    output.mkdir(parents=True, exist_ok=True)
    configure_render()
    add_review_lighting()
    camera = add_camera()
    ceiling = bpy.data.objects.get("ceiling")
    for name, location, target, lens, overhead in VIEWS:
        camera.location = location
        camera.data.type = "ORTHO" if overhead else "PERSP"
        if overhead:
            camera.data.ortho_scale = 18.0
        else:
            camera.data.lens = lens
        look_at(camera, target, (0.0, 0.0, -1.0) if overhead else (0.0, 1.0, 0.0))
        if ceiling is not None:
            ceiling.hide_render = overhead
        bpy.context.scene.render.filepath = str(output / f"{name}.png")
        bpy.ops.render.render(write_still=True)
    if ceiling is not None:
        ceiling.hide_render = False
    print(f"BAR_ARCHITECTURE_REVIEW_PASS images={len(VIEWS)} output={output}")


if __name__ == "__main__":
    requested_output = parse_args().output
    resolved_output = requested_output if requested_output.is_absolute() else REPOSITORY_ROOT / requested_output
    render_views(resolved_output.resolve())
