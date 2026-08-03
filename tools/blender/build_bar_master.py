#!/usr/bin/env python3
"""Build the deterministic modular master scene for the approved Z3/H3 bar."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
import math

import bpy

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from bar_model_common import (
    BAR_METRICS,
    MODULE_NAMES,
    add_box,
    add_collection,
    add_combined_boxes,
    add_frame,
    add_root,
    configure_scene,
    material,
    reset_scene,
    save_blend,
)


def _wall_segments():
    width, depth, height = BAR_METRICS["room_size"]
    thickness = BAR_METRICS["wall_thickness"]
    half_width = width * 0.5
    half_depth = depth * 0.5
    entry = BAR_METRICS["south_main_entry"]
    window = BAR_METRICS["south_east_window"]
    service = BAR_METRICS["north_east_service_door"]

    def horizontal_span(start: float, end: float, y: float, wall_z: float, segment_height: float):
        return ((start + end) * 0.5, y, wall_z), (end - start, segment_height, thickness)

    entry_min = entry["location"][0] - entry["size"][0] * 0.5
    entry_max = entry["location"][0] + entry["size"][0] * 0.5
    window_min = window["location"][0] - window["size"][0] * 0.5
    window_max = window["location"][0] + window["size"][0] * 0.5
    service_min = service["location"][0] - service["size"][0] * 0.5
    service_max = service["location"][0] + service["size"][0] * 0.5

    boxes = [
        ((-half_width + thickness * 0.25, height * 0.5, 0.0),
         (thickness * 0.5, height, depth)),
        ((half_width - thickness * 0.25, height * 0.5, 0.0),
         (thickness * 0.5, height, depth)),
        horizontal_span(-half_width, service_min, height * 0.5, -half_depth + thickness * 0.5, height),
        horizontal_span(service_max, half_width, height * 0.5, -half_depth + thickness * 0.5, height),
        horizontal_span(service_min, service_max,
                        service["size"][1] + (height - service["size"][1]) * 0.5,
                        -half_depth + thickness * 0.5,
                        height - service["size"][1]),
        horizontal_span(-half_width, entry_min, height * 0.5, half_depth - thickness * 0.5, height),
        horizontal_span(entry_max, window_min, height * 0.5, half_depth - thickness * 0.5, height),
        horizontal_span(window_max, half_width, height * 0.5, half_depth - thickness * 0.5, height),
        horizontal_span(entry_min, entry_max,
                        entry["size"][1] + (height - entry["size"][1]) * 0.5,
                        half_depth - thickness * 0.5,
                        height - entry["size"][1]),
        horizontal_span(window_min, window_max,
                        window["size"][1] + window["sill_height"] +
                        (height - window["size"][1] - window["sill_height"]) * 0.5,
                        half_depth - thickness * 0.5,
                        height - window["size"][1] - window["sill_height"]),
        horizontal_span(window_min, window_max,
                        window["sill_height"] * 0.5,
                        half_depth - thickness * 0.5,
                        window["sill_height"]),
        ((0.0, 0.04, 0.0), (width, 0.08, depth)),
    ]
    return boxes


def _wainscot_segments():
    width, depth, _height = BAR_METRICS["room_size"]
    panel_height = BAR_METRICS["wainscot_height"]
    entry = BAR_METRICS["south_main_entry"]
    window = BAR_METRICS["south_east_window"]
    service = BAR_METRICS["north_east_service_door"]
    half_width = width * 0.5
    inner_x = half_width - BAR_METRICS["wall_thickness"] - 0.0125
    inner_z = depth * 0.5 - BAR_METRICS["wall_thickness"] - 0.0125
    entry_min = entry["location"][0] - entry["size"][0] * 0.5
    entry_max = entry["location"][0] + entry["size"][0] * 0.5
    window_min = window["location"][0] - window["size"][0] * 0.5
    window_max = window["location"][0] + window["size"][0] * 0.5
    service_min = service["location"][0] - service["size"][0] * 0.5
    service_max = service["location"][0] + service["size"][0] * 0.5

    def span(start: float, end: float, z: float, height: float = panel_height):
        return ((start + end) * 0.5, height * 0.5, z), (end - start, height, 0.025)

    return [
        ((-inner_x, panel_height * 0.5, 0.0), (0.025, panel_height, depth - 0.22)),
        ((inner_x, panel_height * 0.5, 0.0), (0.025, panel_height, depth - 0.22)),
        span(-inner_x, service_min, -inner_z),
        span(service_max, inner_x, -inner_z),
        span(-inner_x, entry_min, inner_z),
        span(entry_max, window_min, inner_z),
        span(window_max, inner_x, inner_z),
        span(window_min, window_max, inner_z, window["sill_height"]),
    ]


def _build_architecture(root: bpy.types.Object, collection: bpy.types.Collection) -> None:
    clay = material("warm_gray_plaster", (0.47, 0.43, 0.38, 1.0), 0.88)
    dark_clay = material("dark_walnut", (0.16, 0.065, 0.035, 1.0), 0.76)
    trim_clay = material("warm_oak", (0.48, 0.25, 0.095, 1.0), 0.72)
    glass_clay = material("simple_glass", (0.16, 0.29, 0.32, 0.55), 0.28)
    metal_clay = material("dark_silver", (0.18, 0.20, 0.22, 1.0), 0.34, 0.58)

    placement = bpy.data.objects.new("Placement", None)
    collection.objects.link(placement)
    placement.parent = root
    placement.empty_display_type = "PLAIN_AXES"
    placement.empty_display_size = 0.18

    add_combined_boxes("room_shell", _wall_segments(), root, collection, clay)
    add_box("ceiling", (0.0, 4.46, 0.0), (16.0, 0.08, 10.0), root, collection, clay)
    add_combined_boxes("wainscot", _wainscot_segments(), root, collection, dark_clay)

    board_width = BAR_METRICS["floor_board_width"]
    board_count = int(16.0 / board_width) + 1
    board_boxes = []
    for index in range(board_count):
        start = -8.0 + index * board_width
        end = min(start + board_width - 0.008, 8.0)
        if end <= start:
            continue
        board_boxes.append((((start + end) * 0.5, 0.085, 0.0), (end - start, 0.01, 9.78)))
    add_combined_boxes("north_south_floor_boards", board_boxes, root, collection, trim_clay)

    entry = BAR_METRICS["south_main_entry"]
    entry_obj = add_box("south_main_entry", entry["location"], entry["size"], root, collection, dark_clay)
    entry_obj["leaf_count"] = 2
    add_box("south_main_entry_seam", (entry["location"][0], entry["location"][1], 4.885),
            (0.018, entry["size"][1] - 0.08, 0.025), root, collection, trim_clay)
    for offset, name in ((-0.10, "south_main_entry_left_handle"), (0.10, "south_main_entry_right_handle")):
        add_box(name, (entry["location"][0] + offset, 1.05, 4.84),
                (0.035, 0.28, 0.04), root, collection, metal_clay)

    service = BAR_METRICS["north_east_service_door"]
    add_box("north_east_service_door", service["location"], service["size"], root, collection, dark_clay)
    add_box("north_east_service_door_handle", (service["location"][0] - 0.28, 1.05, -4.84),
            (0.035, 0.24, 0.04), root, collection, metal_clay)

    window = BAR_METRICS["south_east_window"]
    add_frame("south_east_window", window["location"], window["size"], 0.10,
              root, collection, trim_clay)
    add_box("south_east_window_glass", (window["location"][0], window["location"][1], 4.985),
            (window["size"][0] - 0.20, window["size"][1] - 0.20, 0.015),
            root, collection, glass_clay)
    add_box("south_east_window_mullion", (window["location"][0], window["location"][1], 4.86),
            (0.055, window["size"][1] - 0.18, 0.055), root, collection, trim_clay)

    rail_y = BAR_METRICS["wainscot_height"] + 0.035
    add_box("west_wainscot_rail", (-7.765, rail_y, 0.0), (0.055, 0.07, 9.58), root, collection, trim_clay)
    add_box("east_wainscot_rail", (7.765, rail_y, 0.0), (0.055, 0.07, 9.58), root, collection, trim_clay)


def _empty(name, location, parent, collection):
    obj = bpy.data.objects.new(name, None)
    collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.12
    return obj


def _move_primitive(obj, name, parent, collection, surface):
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.parent = parent
    if obj.data is not None:
        obj.data.name = f"{name}_Mesh"
        obj.data.materials.append(surface)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)
    return obj


def _cylinder(name, location, radius, height, parent, collection, surface, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=height, location=location,
        rotation=(math.pi * 0.5, 0.0, 0.0))
    return _move_primitive(bpy.context.object, name, parent, collection, surface)


def _torus(name, location, major_radius, minor_radius, parent, collection, surface):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius, minor_radius=minor_radius,
        major_segments=16, minor_segments=6, location=location,
        rotation=(math.pi * 0.5, 0.0, 0.0))
    return _move_primitive(bpy.context.object, name, parent, collection, surface)


def _palette():
    return {
        "green": material("deep_green_cabinet", (0.035, 0.16, 0.105, 1.0), 0.76),
        "walnut": material("dark_walnut", (0.16, 0.065, 0.035, 1.0), 0.76),
        "oak": material("warm_oak", (0.48, 0.25, 0.095, 1.0), 0.72),
        "plaster": material("warm_gray_plaster", (0.47, 0.43, 0.38, 1.0), 0.88),
        "copper": material("copper", (0.56, 0.22, 0.095, 1.0), 0.42, 0.72),
        "brass": material("brushed_brass", (0.56, 0.38, 0.12, 1.0), 0.38, 0.68),
        "silver": material("dark_silver", (0.18, 0.20, 0.22, 1.0), 0.34, 0.58),
        "glass": material("simple_glass", (0.16, 0.29, 0.32, 0.55), 0.28),
    }


def _build_counter(root, collection):
    p = _palette()
    _empty("bar_counter_Placement", (0.0, 0.0, 0.0), root, collection)
    west, east = -7.35, 1.75
    add_box("front_guest_apron", (-3.725, 0.59, -0.90), (7.25, 1.02, 0.10), root, collection, p["green"])
    add_box("guest_counter_top", (-2.80, 1.35, -0.85), (9.10, 0.06, 0.60), root, collection, p["walnut"])
    add_box("player_worktop_west", (-3.725, 1.10, -1.53), (7.25, 0.05, 0.78), root, collection, p["walnut"])
    add_box("player_worktop_east_cap", (1.575, 1.10, -1.53), (0.35, 0.05, 0.78), root, collection, p["walnut"])
    for index, x in enumerate((-7.29, -5.51, -4.05, -2.59, -1.13, -0.13), 1):
        add_box(f"counter_carcass_divider_{index}", (x, 0.55, -1.53), (0.06, 1.00, 0.76), root, collection, p["green"])
    add_box("counter_carcass_top_rail", (-3.72, 1.045, -1.53), (7.18, 0.05, 0.76), root, collection, p["green"])
    drawer_x = (-6.24, -4.78, -3.32, -1.86)
    for bay, x in enumerate(drawer_x, 1):
        for level, y in (("upper", 0.83), ("lower", 0.39)):
            drawer = _empty(f"front_drawer_{bay}_{level}", (x, y, -1.91), root, collection)
            add_box(f"front_drawer_{bay}_{level}_face", (0.0, 0.0, 0.0), (1.30, 0.36, 0.08), drawer, collection, p["green"])
            add_box(f"front_drawer_{bay}_{level}_handle", (0.0, 0.0, -0.055), (0.34, 0.035, 0.035), drawer, collection, p["brass"])
    add_box("west_manual_return", (-7.0, 0.56, -2.72), (0.70, 1.12, 1.54), root, collection, p["green"])
    add_box("west_manual_return_top", (-7.0, 1.10, -2.72), (0.70, 0.05, 1.54), root, collection, p["walnut"])
    shelf = _empty("manual_shelf", (-7.0, 1.145, -2.72), root, collection)
    shelf.rotation_euler.x = math.radians(-10.0)
    add_box("manual_shelf_board", (0.0, 0.0, 0.0), (0.52, 0.05, 0.38), shelf, collection, p["walnut"])
    add_box("manual_shelf_stop", (0.0, 0.055, 0.17), (0.52, 0.09, 0.025), shelf, collection, p["brass"])
    sink = _empty("east_sink", (0.65, 1.12, -1.40), root, collection)
    add_frame("east_sink_rim", (0.0, 0.01, 0.0), (1.10, 0.62, 0.05), 0.08, sink, collection, p["silver"])
    add_box("east_sink_basin", (0.0, -0.12, 0.0), (0.92, 0.20, 0.44), sink, collection, p["silver"])
    plumbing = _empty("sink_plumbing", (0.0, 0.0, 0.0), root, collection)
    for name, loc, size in (
        ("sink_drain_vertical", (0.65, 0.75, -1.40), (0.09, 0.42, 0.09)),
        ("sink_trap_bottom", (0.52, 0.42, -1.40), (0.32, 0.09, 0.09)),
        ("sink_hot_supply", (0.87, 0.56, -1.60), (0.035, 0.78, 0.035)),
        ("sink_cold_supply", (0.99, 0.56, -1.60), (0.035, 0.78, 0.035))):
        add_box(name, loc, size, plumbing, collection, p["copper"])
    waste = _empty("waste_bin", (1.40, 0.50, -3.42), root, collection)
    add_box("waste_bin_body", (0.0, 0.0, 0.0), (0.70, 1.00, 0.76), waste, collection, p["green"])
    add_box("waste_bin_opening", (0.0, 0.42, -0.39), (0.46, 0.22, 0.025), waste, collection, p["silver"])
    gate = _empty("employee_gate", (1.40, 0.49, -2.62), root, collection)
    add_box("employee_gate_leaf", (0.0, 0.0, 0.0), (0.08, 0.98, 0.72), gate, collection, p["green"])
    add_box("workboard", (-3.15, 1.15, -1.50), (2.05, 0.04, 0.52), root, collection, p["oak"])
    for index, x in enumerate((-3.77, -3.15, -2.53), 1):
        add_box(f"workboard_slot_{index}", (x, 1.176, -1.50), (0.28, 0.006, 0.22), root, collection, p["brass"])


def _build_backbar(root, collection):
    p = _palette()
    add_box("rear_bar_worktop", (-2.80, 1.09, -3.78), (9.10, 0.06, 0.56), root, collection, p["walnut"])
    centers = tuple(-6.20 + index * 1.70 for index in range(5))
    for bay, x in enumerate(centers, 1):
        add_box(f"rear_carcass_{bay}", (x, 0.52, -3.84), (1.64, 0.98, 0.46), root, collection, p["green"])
        add_box(f"rear_lower_cabinet_{bay}_fixed", (x - 0.39, 0.52, -3.49), (0.78, 0.96, 0.045), root, collection, p["green"])
        add_box(f"rear_lower_cabinet_{bay}_moving", (x + 0.39, 0.52, -3.46), (0.78, 0.96, 0.045), root, collection, p["green"])
        rack = _empty(f"bottle_rack_bay_{bay}", (x, 0.0, 0.0), root, collection)
        add_box(f"bottle_rack_bay_{bay}_back", (0.0, 1.835, -4.10), (1.62, 1.43, 0.05), rack, collection, p["walnut"])
        for level, y in (("lower", 1.48), ("upper", 2.08)):
            add_box(f"bottle_rack_bay_{bay}_{level}_shelf", (0.0, y, -3.84), (1.62, 0.04, 0.48), rack, collection, p["walnut"])
            add_box(f"bottle_rack_bay_{bay}_{level}_lip", (0.0, y + 0.035, -3.61), (1.62, 0.07, 0.012), rack, collection, p["brass"])
        add_box(f"upper_cabinet_shell_{bay}", (x, 3.30, -3.84), (1.66, 1.30, 0.42), root, collection, p["walnut"])
        for leaf, sign in (("left", -1.0), ("right", 1.0)):
            pivot = _empty(f"back_cabinet_{bay}_{leaf}", (x + sign * 0.81, 3.30, -3.62), root, collection)
            add_box(f"back_cabinet_{bay}_{leaf}_panel", (-sign * 0.405, 0.0, 0.0), (0.81, 1.22, 0.06), pivot, collection, p["green"])
            add_box(f"back_cabinet_{bay}_{leaf}_handle", (-sign * 0.70, 0.0, -0.05), (0.035, 0.34, 0.035), pivot, collection, p["brass"])


def _build_furniture(root, collection):
    p = _palette()
    for index in range(6):
        x = -5.55 + index * 1.10
        item = _empty(f"stool_{index + 1}", (x, 0.0, -0.24), root, collection)
        _cylinder(f"stool_{index + 1}_seat", (0.0, 0.78, 0.0), 0.20, 0.12, item, collection, p["oak"])
        _cylinder(f"stool_{index + 1}_stem", (0.0, 0.39, 0.0), 0.035, 0.66, item, collection, p["silver"], 12)
        _torus(f"stool_{index + 1}_ring", (0.0, 0.28, 0.0), 0.16, 0.018, item, collection, p["silver"])
    table_positions = ((4.35, -2.15), (4.65, 0.25), (4.35, 2.65))
    chair_index = 1
    for table_index, (x, z) in enumerate(table_positions, 1):
        table = _empty(f"lounge_table_{table_index}", (x, 0.0, z), root, collection)
        _cylinder(f"lounge_table_{table_index}_top", (0.0, 0.71, 0.0), 0.40, 0.08, table, collection, p["oak"])
        _cylinder(f"lounge_table_{table_index}_stem", (0.0, 0.35, 0.0), 0.045, 0.66, table, collection, p["silver"], 12)
        for dx, dz in ((0.0, 0.68), (0.0, -0.68), (-0.68, 0.0), (0.68, 0.0)):
            chair = _empty(f"lounge_chair_{chair_index}", (x + dx, 0.0, z + dz), root, collection)
            add_box(f"lounge_chair_{chair_index}_seat", (0.0, 0.43, 0.0), (0.46, 0.08, 0.46), chair, collection, p["oak"])
            add_box(f"lounge_chair_{chair_index}_back", (0.0, 0.68, -0.20), (0.46, 0.44, 0.06), chair, collection, p["oak"])
            for leg, lx, lz in ((1, -0.18, -0.18), (2, 0.18, -0.18), (3, -0.18, 0.18), (4, 0.18, 0.18)):
                add_box(f"lounge_chair_{chair_index}_leg_{leg}", (lx, 0.21, lz), (0.035, 0.42, 0.035), chair, collection, p["silver"])
            chair_index += 1


def _build_lighting(root, collection):
    p = _palette()
    for index, x in enumerate((-5.40, -2.80, -0.20), 1):
        pendant = _empty(f"pendant_{index}", (x, 0.0, -1.40), root, collection)
        _cylinder(f"pendant_{index}_canopy", (0.0, 4.38, 0.0), 0.12, 0.05, pendant, collection, p["silver"], 16)
        _cylinder(f"pendant_{index}_cord", (0.0, 3.47, 0.0), 0.012, 1.78, pendant, collection, p["silver"], 8)
        _cylinder(f"pendant_{index}_shade", (0.0, 2.53, 0.0), 0.24, 0.18, pendant, collection, p["copper"], 16)
        _cylinder(f"pendant_{index}_emitter", (0.0, 2.43, 0.0), 0.14, 0.015, pendant, collection, p["brass"], 16)
    for index, x in enumerate((-5.05, -0.55), 1):
        add_box(f"rear_linear_{index}", (x, 2.45, -3.58), (4.25, 0.08, 0.10), root, collection, p["silver"])
    for side, x in (("west", -7.84), ("east", 7.84)):
        for index, z in enumerate((0.15, 2.85), 1):
            sconce = _empty(f"{side}_sconce_{index}", (x, 2.15, z), root, collection)
            add_box(f"{side}_sconce_{index}_backplate", (0.0, 0.0, 0.0), (0.08, 0.34, 0.22), sconce, collection, p["silver"])
            add_box(f"{side}_sconce_{index}_arm", ((0.10 if side == "west" else -0.10), 0.0, 0.0), (0.20, 0.035, 0.035), sconce, collection, p["brass"])
            _cylinder(f"{side}_sconce_{index}_shade", ((0.20 if side == "west" else -0.20), 0.0, 0.0), 0.13, 0.18, sconce, collection, p["copper"], 12)


def _build_wear(root, collection):
    p = _palette()
    overlay = _empty("wear_overlay_root", (0.0, 0.0, 0.0), root, collection)
    for index, (loc, size) in enumerate((
        ((-6.6, 1.385, -0.84), (0.38, 0.006, 0.025)),
        ((-1.8, 1.385, -0.84), (0.26, 0.006, 0.02)),
        ((-5.7, 1.505, -3.60), (0.28, 0.006, 0.018)),
        ((-3.0, 2.105, -3.59), (0.22, 0.006, 0.018))), 1):
        add_box(f"wear_mark_{index}", loc, size, overlay, collection, p["silver"])


def _build_review_helpers() -> None:
    collection = add_collection("review_helpers")
    root = add_root("review_helpers", collection)
    reference_material = material("review_player_reference", (0.70, 0.32, 0.20, 1.0), 0.62)
    reference = BAR_METRICS["player_reference"]
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16,
        radius=0.22,
        depth=reference["height"],
        location=reference["location"],
        rotation=(1.57079632679, 0.0, 0.0),
    )
    mannequin = bpy.context.object
    mannequin.name = "player_scale_reference_1_83m"
    for owner in list(mannequin.users_collection):
        owner.objects.unlink(mannequin)
    collection.objects.link(mannequin)
    mannequin.parent = root
    mannequin.data.materials.append(reference_material)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def build_master_scene() -> None:
    reset_scene()
    configure_scene()
    roots = {}
    for module_name in MODULE_NAMES:
        collection = add_collection(module_name)
        roots[module_name] = add_root(module_name, collection)
    _build_architecture(roots["bar_architecture"], bpy.data.collections["bar_architecture"])
    _build_counter(roots["bar_counter"], bpy.data.collections["bar_counter"])
    _build_backbar(roots["bar_backbar"], bpy.data.collections["bar_backbar"])
    _build_furniture(roots["bar_furniture"], bpy.data.collections["bar_furniture"])
    _build_lighting(roots["bar_lighting"], bpy.data.collections["bar_lighting"])
    _build_wear(roots["bar_wear_overlays"], bpy.data.collections["bar_wear_overlays"])
    _build_review_helpers()


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(argv)


if __name__ == "__main__":
    args = parse_args()
    build_master_scene()
    save_blend(args.output)
    print(f"BAR_MASTER_BUILD_PASS output={args.output}")
