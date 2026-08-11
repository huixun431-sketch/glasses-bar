#!/usr/bin/env python3
"""Contract test for the deterministic modular bar Blender scene."""

from __future__ import annotations

import math
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
from render_bar_review import add_review_lighting, configure_render  # noqa: E402


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


def material_names(name: str) -> tuple[str, ...]:
    obj = bpy.data.objects.get(name)
    require(obj is not None and obj.data is not None, f"missing mesh object {name}")
    return tuple(slot.name for slot in obj.data.materials)


def has_plan_vertex(name: str, expected_x: float, expected_z: float, tolerance: float = 0.001) -> bool:
    obj = bpy.data.objects.get(name)
    require(obj is not None and obj.type == "MESH", f"missing polygon mesh {name}")
    return any(
        abs((obj.matrix_world @ vertex.co).x - expected_x) <= tolerance and
        abs((obj.matrix_world @ vertex.co).z - expected_z) <= tolerance
        for vertex in obj.data.vertices
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

    placement = bpy.data.objects.get("Placement")
    require(placement is not None and placement.parent == bpy.data.objects["bar_architecture"],
            "bar_architecture must expose its approved Placement anchor")
    require(near_vector(placement.location, (0.0, 0.0, 0.0)), "Placement anchor must remain at module origin")

    required_counter = [
        "bar_counter_Placement", "east_sink", "sink_base", "waste_bin",
        "employee_gate", "manual_shelf", "bar_structural_base", "bar_carcass_monolith",
        "bar_worktop_monolith",
        "guest_counter_top", "east_sink_faucet",
        *[f"front_drawer_{bay}_{level}" for bay in range(1, 5) for level in ("upper", "lower")],
        *[f"front_drawer_{bay}_{level}_tray" for bay in range(1, 5) for level in ("upper", "lower")],
    ]
    required_backbar = [
        *[f"rear_lower_cabinet_{bay}_{leaf}" for bay in range(1, 6) for leaf in ("fixed", "moving")],
        *[f"back_cabinet_{bay}_{leaf}" for bay in range(1, 6) for leaf in ("left", "right")],
        *[f"rear_cabinet_interior_{bay}" for bay in range(1, 6)],
        *[f"upper_cabinet_interior_{bay}" for bay in range(1, 6)],
        "bottle_rack_bay_1", "bottle_rack_bay_5",
    ]
    required_furniture = [
        *[f"stool_{index}" for index in range(1, 7)],
        *[f"lounge_table_{index}" for index in range(1, 4)],
        *[f"lounge_chair_{index}" for index in range(1, 13)],
    ]
    required_lighting = [
        "pendant_1", "pendant_2", "pendant_3", "rear_linear_1", "rear_linear_2",
        "lounge_pendant_1", "lounge_pendant_2", "lounge_pendant_3",
        "east_sconce_1", "east_sconce_2", "west_sconce_1", "west_sconce_2",
    ]
    for name in required_counter + required_backbar + required_furniture + required_lighting + ["wear_overlay_root"]:
        require(bpy.data.objects.get(name) is not None, f"missing formal module node {name}")
    require(not any("footrail" in name.lower() for name in bpy.data.objects.keys()),
            "front footrail geometry is forbidden")

    required_materials = {
        "deep_brown_cabinet", "dark_walnut", "warm_oak", "warm_gray_plaster",
        "copper", "brushed_brass", "dark_silver", "simple_glass",
    }
    require(required_materials.issubset(set(bpy.data.materials.keys())),
            "approved retro-modern material slots are incomplete")

    expected_materials = {
        "room_shell": "warm_gray_plaster",
        "north_south_floor_boards": "warm_oak",
        "wainscot": "dark_walnut",
        "guest_counter_top": "dark_walnut",
        "bar_worktop_monolith": "dark_walnut",
        "front_drawer_1_upper_face": "deep_brown_cabinet",
        "rear_lower_cabinet_1_moving": "deep_brown_cabinet",
        "bottle_rack_bay_1_lower_shelf": "dark_walnut",
        "lounge_table_1_top": "warm_oak",
        "lounge_chair_1_seat": "warm_oak",
        "stool_1_seat": "warm_oak",
    }
    for name, expected_material in expected_materials.items():
        require(expected_material in material_names(name),
                f"{name} must use {expected_material}, got {material_names(name)}")
    for name in ("room_shell", "north_south_floor_boards", "lounge_table_1_top",
                 "lounge_chair_1_seat", "stool_1_seat"):
        require("deep_brown_cabinet" not in material_names(name),
                f"deep brown is forbidden on non-cabinet object {name}")
    require("deep_green_cabinet" not in bpy.data.materials,
            "the rejected green cabinet material must not remain in the formal batch")

    glass = bpy.data.materials["simple_glass"]
    glass_shader = next(node for node in glass.node_tree.nodes
                        if node.bl_idname == "ShaderNodeBsdfPrincipled")
    transmission = glass_shader.inputs.get("Transmission Weight")
    require(transmission is not None and transmission.default_value >= 0.65,
            "simple glass must export a readable transmissive PBR response")
    require(glass_shader.inputs["Base Color"].default_value[3] < 0.70,
            "simple glass must retain transparent alpha")

    shade_material = bpy.data.materials.get("frosted_translucent_shade")
    require(shade_material is not None,
            "pendant shades must use an explicitly labelled frosted translucent material")
    shade_shader = next(node for node in shade_material.node_tree.nodes
                        if node.bl_idname == "ShaderNodeBsdfPrincipled")
    shade_transmission = shade_shader.inputs.get("Transmission Weight")
    require(shade_transmission is not None and shade_transmission.default_value >= 0.35,
            "frosted shade material must transmit light")
    require(shade_shader.inputs["Roughness"].default_value >= 0.55,
            "frosted shade material must retain a matte diffusion response")
    require(shade_shader.inputs["Base Color"].default_value[3] <= 0.60,
            "frosted shade material must be visibly translucent")

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

    expected_bounds = {
        "bar_structural_base": ((-7.35, 0.0, -4.06), (1.75, 0.08, -0.85)),
        "bar_worktop_monolith": ((-7.35, 1.08, -4.06), (1.75, 1.12, -1.15)),
        "guest_counter_top": ((-7.35, 1.32, -1.15), (1.75, 1.38, -0.55)),
        "upper_cabinet_shell_1": ((-7.03, 2.65, -4.05), (-5.37, 3.95, -3.63)),
    }
    for name, (expected_min, expected_max) in expected_bounds.items():
        actual_min, actual_max = object_world_bounds(name)
        require(near_vector(actual_min, expected_min), f"{name} minimum is wrong: {tuple(actual_min)}")
        require(near_vector(actual_max, expected_max), f"{name} maximum is wrong: {tuple(actual_max)}")
    for x, z in ((-6.45, -1.95), (-6.75, -2.25), (-6.75, -3.20), (-6.45, -3.50)):
        require(has_plan_vertex("bar_worktop_monolith", x, z),
                f"continuous worktop is missing an inner chamfer vertex {(x, z)}")
    require(bpy.data.objects.get("rear_bar_worktop") is None and
            bpy.data.objects.get("player_worktop") is None,
            "front/side/rear worktop seams must not survive as separate meshes")
    require(not any(name.startswith("sink_") and name not in {"sink_base"}
                    for name in bpy.data.objects.keys()),
            "sink plumbing must be concealed rather than exported as visible geometry")
    shelf_board = bpy.data.objects["manual_shelf_board"]
    shelf_root = bpy.data.objects["manual_shelf"]
    shelf_min, shelf_max = object_world_bounds(shelf_board.name)
    shelf_width_x = shelf_max.x - shelf_min.x
    shelf_depth_z = shelf_max.z - shelf_min.z
    require(shelf_depth_z >= shelf_width_x * 2.0,
            "the west manual shelf must be rotated 90 degrees so its long edge follows the west counter depth")
    require(abs(math.degrees(shelf_root.rotation_euler.y) - 90.0) <= 0.001,
            "the west manual shelf must record the approved clockwise 90 degree rotation")
    require(shelf_root.get("orientation_axis") == "west_counter_depth",
            "the west manual shelf must label its corrected west-counter orientation")
    basin = bpy.data.objects["east_sink_basin"]
    require(len(basin.data.vertices) >= 32,
            "the sink basin must be an open trough rather than a solid blocking box")
    hit, hit_location, _normal, _face_index, hit_object, _matrix = bpy.context.scene.ray_cast(
        bpy.context.evaluated_depsgraph_get(), Vector((0.65, 1.80, -1.53)),
        Vector((0.0, -1.0, 0.0)), distance=1.20)
    require(hit and hit_location.y <= 0.94 and hit_object.name == "east_sink_basin",
            f"sink center must open to its bowl bottom, got {getattr(hit_object, 'name', None)} at {hit_location.y if hit else 'no hit'}")
    require(bpy.data.objects.get("east_sink_faucet_spout") is None,
            "the obsolete transverse faucet pipe must not survive")
    require(bpy.data.objects.get("east_sink_faucet_short_neck") is None,
            "the replacement faucet must not read as another horizontal rail")
    angled_spout = bpy.data.objects.get("east_sink_faucet_angled_spout")
    require(angled_spout is not None and angled_spout.dimensions.x >= 0.28 and
            angled_spout.dimensions.y >= 0.08,
            "the east-rim faucet needs a short visibly down-angled spout into the bowl")
    riser_min, riser_max = object_world_bounds("east_sink_faucet_riser")
    outlet_min, outlet_max = object_world_bounds("east_sink_faucet_outlet")
    require(riser_min.x >= 1.10 and outlet_min.x >= 0.74,
            "the faucet must stay on the east rim and only reach into the bowl locally")
    clear_min = Vector((0.29, 0.94, -1.67))
    clear_max = Vector((1.01, 1.105, -1.39))
    allowed_sink_shell = {"east_sink_rim", "east_sink_basin"}
    sink_base_min, sink_base_max = object_world_bounds("sink_base")
    require(sink_base_max.y <= 0.90,
            "the retained sink base must stop below the open bowl clearance")
    sink = bpy.data.objects["east_sink"]
    for obj in sink.children_recursive:
        if obj.type != "MESH" or obj.name in allowed_sink_shell:
            continue
        obj_min, obj_max = object_world_bounds(obj.name)
        overlaps_clearance = all(obj_min[axis] < clear_max[axis] and
                                 obj_max[axis] > clear_min[axis]
                                 for axis in range(3))
        require(not overlaps_clearance,
                f"{obj.name} intrudes into the approved empty sink-bowl clearance")
    gate = bpy.data.objects["employee_gate_leaf"]
    require(gate.dimensions.z >= 0.90 - 0.001,
            "the east employee gate must be at least 0.90 m wide for player passage")

    for bay in range(1, 5):
        for level in ("upper", "lower"):
            drawer = bpy.data.objects[f"front_drawer_{bay}_{level}"]
            require(abs(float(drawer.get("open_travel_m", 0.0)) - 0.38) <= 0.001,
                    f"{drawer.name} must record the approved 0.38 m travel")
    for bay in range(1, 6):
        require(len(bpy.data.objects[f"rear_carcass_{bay}"].data.vertices) > 8,
                f"rear lower bay {bay} must be a hollow frame, not a solid box")
        require(len(bpy.data.objects[f"upper_cabinet_shell_{bay}"].data.vertices) > 8,
                f"upper cabinet bay {bay} must be a hollow frame, not a solid box")
        moving = bpy.data.objects[f"rear_lower_cabinet_{bay}_moving"]
        require(float(moving.get("slide_travel_m", 0.0)) > 0.70,
                f"rear lower bay {bay} must record a usable sliding travel")
        fixed = bpy.data.objects[f"rear_lower_cabinet_{bay}_fixed"]
        require(moving.location.z < fixed.location.z,
                f"rear lower bay {bay} moving leaf must sit behind the fixed leaf")
        lower_min, lower_max = object_world_bounds(f"bottle_rack_bay_{bay}_lower_shelf")
        upper_min, upper_max = object_world_bounds(f"bottle_rack_bay_{bay}_upper_shelf")
        back_min, back_max = object_world_bounds(f"bottle_rack_bay_{bay}_back")
        require(abs(lower_max.y - 1.50) <= 0.001 and abs(upper_max.y - 2.10) <= 0.001,
                f"bottle rack bay {bay} shelf heights drifted")
        require(abs(back_max.y - 2.55) <= 0.001,
                f"bottle rack bay {bay} back height drifted")
        for leaf in ("left", "right"):
            pivot = bpy.data.objects[f"back_cabinet_{bay}_{leaf}"]
            require(abs(float(pivot.get("open_angle_degrees", 0.0)) - 85.0) <= 0.001,
                    f"{pivot.name} must record the approved review angle")

    table_positions = ((4.35, -2.15), (4.65, 0.25), (4.35, 2.65))
    chair_index = 1
    for table_index, (table_x, table_z) in enumerate(table_positions, 1):
        pendant = bpy.data.objects[f"lounge_pendant_{table_index}"]
        require(near_vector((pendant.location.x, pendant.location.z), (table_x, table_z)),
                f"lounge pendant {table_index} must be centered over its table")
        for _ in range(4):
            chair = bpy.data.objects[f"lounge_chair_{chair_index}"]
            direction = Vector((table_x - chair.location.x, 0.0, table_z - chair.location.z)).normalized()
            forward = chair.rotation_euler.to_matrix() @ Vector((0.0, 0.0, 1.0))
            require(forward.dot(direction) >= 0.999,
                    f"{chair.name} must face its table center")
            chair_index += 1
    for name in tuple(f"pendant_{index}_shade" for index in range(1, 4)) + tuple(
            f"lounge_pendant_{index}_shade" for index in range(1, 4)):
        require("frosted_translucent_shade" in material_names(name),
                f"{name} must carry the labelled frosted translucent shade material")

    configure_render()
    add_review_lighting()
    review_lights = [obj for obj in bpy.data.objects if obj.type == "LIGHT"]
    require(review_lights, "review renderer must add neutral inspection lights")
    require(all(obj.data.type == "SUN" for obj in review_lights),
            "review lights must be directional to avoid rectangular area-light projections")
    require(scene.world is not None and scene.world.use_nodes,
            "review renderer must use a world-node ambient fill")
    background = scene.world.node_tree.nodes.get("Background")
    require(background is not None and background.inputs["Strength"].default_value > 0.0,
            "review world must provide non-zero neutral ambient fill")
    print("BAR_MODEL_CONTRACT_PASS")


if __name__ == "__main__":
    run()
