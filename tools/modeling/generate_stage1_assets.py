#!/usr/bin/env python3
"""Generate the first Glasses Bar low-poly interaction asset batch.

Run with Blender 4.5.5 LTS:
  blender --background --python tools/modeling/generate_stage1_assets.py -- --output assets/models
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy


SEGMENTS = 16


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def set_socket(shader: bpy.types.ShaderNodeBsdfPrincipled, names: tuple[str, ...], value) -> None:
    for name in names:
        socket = shader.inputs.get(name)
        if socket is not None:
            socket.default_value = value
            return


def make_material(
    name: str,
    color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.55,
    transmission: float = 0.0,
    ior: float = 1.45,
    coat_weight: float = 0.0,
    coat_roughness: float = 0.2,
) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = color
    shader = next(
        node
        for node in material.node_tree.nodes
        if node.bl_idname == "ShaderNodeBsdfPrincipled"
    )
    set_socket(shader, ("Base Color",), color)
    set_socket(shader, ("Metallic",), metallic)
    set_socket(shader, ("Roughness",), roughness)
    set_socket(shader, ("Alpha",), color[3])
    set_socket(shader, ("Transmission Weight", "Transmission"), transmission)
    set_socket(shader, ("IOR",), ior)
    set_socket(shader, ("Coat Weight", "Clearcoat"), coat_weight)
    set_socket(shader, ("Coat Roughness", "Clearcoat Roughness"), coat_roughness)
    if color[3] < 1.0:
        if hasattr(material, "surface_render_method"):
            material.surface_render_method = "DITHERED"
        material.use_transparency_overlap = False
    return material


def add_root(asset_id: str) -> bpy.types.Object:
    root = bpy.data.objects.new(asset_id, None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.06
    bpy.context.scene.collection.objects.link(root)
    return root


def parent_to(obj: bpy.types.Object, root: bpy.types.Object) -> bpy.types.Object:
    obj.parent = root
    return obj


def add_anchor(root: bpy.types.Object, name: str, godot_position: tuple[float, float, float]) -> None:
    x, y, z = godot_position
    anchor = bpy.data.objects.new(name, None)
    anchor.empty_display_type = "PLAIN_AXES"
    anchor.empty_display_size = 0.025
    anchor.location = (x, -z, y)
    bpy.context.scene.collection.objects.link(anchor)
    anchor.parent = root


def add_cylinder(
    root: bpy.types.Object,
    name: str,
    radius: float,
    depth: float,
    z: float,
    material: bpy.types.Material,
    *,
    vertices: int = SEGMENTS,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0, 0, z))
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return parent_to(obj, root)


def add_torus(
    root: bpy.types.Object,
    name: str,
    major_radius: float,
    minor_radius: float,
    z: float,
    material: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=SEGMENTS,
        minor_segments=4,
        location=(0, 0, z),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return parent_to(obj, root)


def add_frustum_shell(
    root: bpy.types.Object,
    name: str,
    z0: float,
    z1: float,
    outer0: float,
    outer1: float,
    inner0: float,
    inner1: float,
    material: bpy.types.Material,
    *,
    close_bottom: bool,
    close_top: bool,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    for radius, z in ((outer0, z0), (outer1, z1), (inner0, z0), (inner1, z1)):
        for index in range(SEGMENTS):
            angle = math.tau * index / SEGMENTS
            vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, z))

    faces: list[tuple[int, int, int, int]] = []
    outer_bottom = 0
    outer_top = SEGMENTS
    inner_bottom = SEGMENTS * 2
    inner_top = SEGMENTS * 3
    for index in range(SEGMENTS):
        nxt = (index + 1) % SEGMENTS
        faces.append((outer_bottom + index, outer_bottom + nxt, outer_top + nxt, outer_top + index))
        faces.append((inner_bottom + nxt, inner_bottom + index, inner_top + index, inner_top + nxt))
        if close_bottom:
            faces.append((outer_bottom + nxt, outer_bottom + index, inner_bottom + index, inner_bottom + nxt))
        if close_top:
            faces.append((outer_top + index, outer_top + nxt, inner_top + nxt, inner_top + index))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return parent_to(obj, root)


def add_revolved_profile(
    root: bpy.types.Object,
    name: str,
    profile: list[tuple[float, float]],
    material: bpy.types.Material,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    for z, radius in profile:
        for index in range(SEGMENTS):
            angle = math.tau * index / SEGMENTS
            vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, z))

    faces: list[tuple[int, int, int, int]] = []
    for row in range(len(profile) - 1):
        start = row * SEGMENTS
        next_start = (row + 1) * SEGMENTS
        for index in range(SEGMENTS):
            nxt = (index + 1) % SEGMENTS
            faces.append((start + index, start + nxt, next_start + nxt, next_start + index))
    faces.append(tuple(reversed(range(SEGMENTS))))
    top_start = (len(profile) - 1) * SEGMENTS
    faces.append(tuple(top_start + index for index in range(SEGMENTS)))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return parent_to(obj, root)


def build_highball_glass() -> bpy.types.Object:
    root = add_root("highball_glass")
    glass = make_material(
        "Glass_Warm_Candidate", (0.48, 0.78, 0.86, 0.22), roughness=0.06,
        transmission=0.98, ior=1.47, coat_weight=0.34, coat_roughness=0.06)
    edge = make_material(
        "Glass_Edge_Candidate", (0.70, 0.92, 0.98, 0.86), roughness=0.07,
        transmission=0.30, ior=1.47, coat_weight=0.48, coat_roughness=0.04)
    base = make_material(
        "Glass_Base_Candidate", (0.44, 0.76, 0.86, 0.70), roughness=0.09,
        transmission=0.42, ior=1.47, coat_weight=0.44, coat_roughness=0.05)
    add_frustum_shell(root, "GlassBody", 0.018, 0.245, 0.060, 0.075, 0.050, 0.065, glass,
                      close_bottom=True, close_top=True)
    add_cylinder(root, "WeightedBase", 0.060, 0.024, 0.012, base)
    add_torus(root, "GlassRim", 0.070, 0.005, 0.246, edge)
    add_torus(root, "GlassBaseEdge", 0.057, 0.003, 0.025, edge)
    add_anchor(root, "Grip", (0, 0.13, 0.07))
    add_anchor(root, "Placement", (0, 0, 0))
    add_anchor(root, "FillOrigin", (0, 0.035, 0))
    return root


def build_jigger_medium() -> bpy.types.Object:
    root = add_root("jigger_medium")
    metal = make_material(
        "Dark_Silver_Candidate", (0.64, 0.70, 0.76, 1.0), metallic=0.72,
        roughness=0.18, coat_weight=0.24, coat_roughness=0.07)
    edge = make_material(
        "Worn_Silver_Edge", (0.92, 0.95, 0.98, 1.0), metallic=0.82,
        roughness=0.10, coat_weight=0.32, coat_roughness=0.05)
    add_frustum_shell(root, "LowerCup", 0.0, 0.078, 0.055, 0.023, 0.049, 0.018, metal,
                      close_bottom=True, close_top=False)
    add_cylinder(root, "Waist", 0.023, 0.032, 0.094, metal)
    add_frustum_shell(root, "UpperCup", 0.110, 0.18, 0.023, 0.065, 0.018, 0.058, metal,
                      close_bottom=False, close_top=True)
    add_torus(root, "LowerRim", 0.052, 0.003, 0.003, edge)
    add_torus(root, "UpperRim", 0.062, 0.003, 0.177, edge)
    add_torus(root, "WaistBand", 0.024, 0.003, 0.094, edge)
    add_anchor(root, "Grip", (0, 0.09, 0))
    add_anchor(root, "Placement", (0, 0, 0))
    add_anchor(root, "FillOrigin", (0, 0.105, 0))
    add_anchor(root, "Spout", (0, 0.18, -0.065))
    return root


def build_mortar() -> bpy.types.Object:
    root = add_root("mortar")
    body = make_material("Mortar_Composite_Candidate", (0.42, 0.25, 0.14, 1.0), metallic=0.04, roughness=0.72)
    inner = make_material("Mortar_Interior", (0.18, 0.10, 0.055, 1.0), metallic=0.02, roughness=0.84)
    brass = make_material("Mortar_Worn_Band", (0.50, 0.28, 0.09, 1.0), metallic=0.48, roughness=0.48)
    add_cylinder(root, "MortarFoot", 0.18, 0.05, 0.025, body)
    add_frustum_shell(root, "MortarBody", 0.045, 0.235, 0.18, 0.24, 0.115, 0.18, body,
                      close_bottom=True, close_top=True)
    add_cylinder(root, "MortarInnerFloor", 0.115, 0.012, 0.057, inner)
    add_torus(root, "MortarRim", 0.21, 0.03, 0.215, body)
    add_torus(root, "MortarWearBand", 0.185, 0.008, 0.060, brass)
    add_anchor(root, "Grip", (-0.20, 0.12, 0))
    add_anchor(root, "Placement", (0, 0, 0))
    add_anchor(root, "FillOrigin", (0, 0.07, 0))
    add_anchor(root, "Interaction", (0, 0.17, 0))
    return root


def build_pestle() -> bpy.types.Object:
    root = add_root("pestle")
    body = make_material("Pestle_Composite_Candidate", (0.28, 0.19, 0.13, 1.0), metallic=0.03, roughness=0.68)
    wear = make_material("Pestle_Contact_Wear", (0.16, 0.105, 0.072, 1.0), metallic=0.02, roughness=0.82)
    add_revolved_profile(
        root,
        "PestleBody",
        [(0.0, 0.058), (0.025, 0.075), (0.075, 0.062), (0.19, 0.041),
         (0.31, 0.035), (0.385, 0.047), (0.42, 0.035)],
        body,
    )
    add_torus(root, "PestleContactWear", 0.062, 0.007, 0.025, wear)
    add_torus(root, "PestleGripBand", 0.036, 0.004, 0.285, wear)
    add_anchor(root, "Grip", (0, 0.27, 0))
    add_anchor(root, "Placement", (0, 0, 0))
    add_anchor(root, "Interaction", (0, 0.025, 0))
    return root


BUILDERS = (build_highball_glass, build_jigger_medium, build_mortar, build_pestle)


def export_asset(root: bpy.types.Object, output: Path) -> None:
    bpy.ops.object.select_all(action="SELECT")
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(output),
        export_format="GLB",
        use_selection=True,
        export_yup=True,
        export_apply=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )
    print(f"WROTE {output}")


def main() -> None:
    args = parse_args()
    output_root = args.output.resolve()
    for builder in BUILDERS:
        reset_scene()
        root = builder()
        export_asset(root, output_root / f"{root.name}.glb")


if __name__ == "__main__":
    main()
