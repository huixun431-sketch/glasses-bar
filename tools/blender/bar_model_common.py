#!/usr/bin/env python3
"""Shared deterministic helpers and the approved metric table for bar modules."""

from __future__ import annotations

import math
from pathlib import Path
from typing import Iterable, Sequence

import bpy
from mathutils import Matrix, Vector


MODULE_NAMES = (
    "bar_architecture",
    "bar_counter",
    "bar_backbar",
    "bar_furniture",
    "bar_lighting",
    "bar_wear_overlays",
)

REPOSITORY_ROOT = Path(__file__).resolve().parents[1]

BAR_METRICS = {
    "room_size": (16.0, 10.0, 4.5),
    "wall_thickness": 0.20,
    "wainscot_height": 1.05,
    "floor_board_width": 0.18,
    "south_main_entry": {
        "location": (-0.65, 1.05, 5.0),
        "size": (1.40, 2.10, 0.20),
    },
    "south_east_window": {
        "location": (4.35, 1.525, 5.0),
        "size": (3.20, 1.55, 0.20),
        "sill_height": 0.75,
    },
    "north_east_service_door": {
        "location": (6.90, 1.05, -5.0),
        "size": (0.90, 2.10, 0.20),
    },
    "player_reference": {
        "location": (-2.80, 0.915, -2.72),
        "height": 1.83,
    },
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)
    base = bpy.data.collections.get("Collection")
    if base is not None:
        base.name = "_unused"
    for datablocks in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def configure_scene() -> None:
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene["export_up_axis"] = "+Y"
    scene["export_forward_axis"] = "-Z"
    scene["bar_contract_version"] = "Z3_H3_2026_08_03"


def add_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def add_root(name: str, collection: bpy.types.Collection) -> bpy.types.Object:
    root = bpy.data.objects.new(name, None)
    collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.25
    return root


def material(
    name: str,
    color: tuple[float, float, float, float],
    roughness: float = 0.72,
    metallic: float = 0.0,
) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        return existing
    result = bpy.data.materials.new(name)
    result.diffuse_color = color
    result.use_nodes = True
    shader = next(
        node for node in result.node_tree.nodes
        if node.bl_idname == "ShaderNodeBsdfPrincipled"
    )
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    return result


def _box_geometry(size: Sequence[float], center: Sequence[float] = (0.0, 0.0, 0.0)):
    hx, hy, hz = (float(value) * 0.5 for value in size)
    cx, cy, cz = (float(value) for value in center)
    vertices = [
        (cx - hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz - hz),
        (cx + hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz - hz),
        (cx - hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz + hz),
        (cx + hx, cy + hy, cz + hz), (cx - hx, cy + hy, cz + hz),
    ]
    faces = [
        (0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1),
        (1, 5, 6, 2), (2, 6, 7, 3), (4, 0, 3, 7),
    ]
    return vertices, faces


def add_box(
    name: str,
    location: Sequence[float],
    size: Sequence[float],
    parent: bpy.types.Object,
    collection: bpy.types.Collection,
    surface: bpy.types.Material,
) -> bpy.types.Object:
    vertices, faces = _box_geometry(size)
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(surface)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.parent = parent
    obj.location = tuple(float(value) for value in location)
    return obj


def add_combined_boxes(
    name: str,
    boxes: Iterable[tuple[Sequence[float], Sequence[float]]],
    parent: bpy.types.Object,
    collection: bpy.types.Collection,
    surface: bpy.types.Material,
    location: Sequence[float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for center, size in boxes:
        box_vertices, box_faces = _box_geometry(size, center)
        offset = len(vertices)
        vertices.extend(box_vertices)
        faces.extend(tuple(index + offset for index in face) for face in box_faces)
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(surface)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.parent = parent
    obj.location = tuple(float(value) for value in location)
    return obj


def add_frame(
    name: str,
    location: Sequence[float],
    size: Sequence[float],
    border: float,
    parent: bpy.types.Object,
    collection: bpy.types.Collection,
    surface: bpy.types.Material,
) -> bpy.types.Object:
    width, height, depth = (float(value) for value in size)
    parts = (
        ((-width * 0.5 + border * 0.5, 0.0, 0.0), (border, height, depth)),
        ((width * 0.5 - border * 0.5, 0.0, 0.0), (border, height, depth)),
        ((0.0, -height * 0.5 + border * 0.5, 0.0), (width - border * 2.0, border, depth)),
        ((0.0, height * 0.5 - border * 0.5, 0.0), (width - border * 2.0, border, depth)),
    )
    return add_combined_boxes(name, parts, parent, collection, surface, location)


def descendants(root: bpy.types.Object) -> list[bpy.types.Object]:
    result: list[bpy.types.Object] = []
    pending = list(root.children)
    while pending:
        current = pending.pop()
        result.append(current)
        pending.extend(current.children)
    return result


def look_at(
    obj: bpy.types.Object,
    target: Sequence[float],
    world_up: Sequence[float] = (0.0, 1.0, 0.0),
) -> None:
    """Aim a Blender camera/light while treating project +Y as visual up."""
    forward = (Vector(target) - obj.location).normalized()
    up = Vector(world_up).normalized()
    if abs(forward.dot(up)) > 0.999:
        up = Vector((0.0, 0.0, -1.0))
    local_z = -forward
    local_x = up.cross(local_z).normalized()
    local_y = local_z.cross(local_x).normalized()
    obj.rotation_euler = Matrix((local_x, local_y, local_z)).transposed().to_euler()


def save_blend(path: Path) -> None:
    ensure_artifacts_ignored()
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))


def ensure_artifacts_ignored() -> None:
    """Keep local Blender/PNG review evidence out of Godot's importer."""
    artifacts = REPOSITORY_ROOT / "artifacts"
    artifacts.mkdir(parents=True, exist_ok=True)
    marker = artifacts / ".gdignore"
    if not marker.exists():
        marker.write_text("# Generated review evidence is not a Godot resource.\n", encoding="utf-8")
