#!/usr/bin/env python3
"""Build the deterministic modular master scene for the approved Z3/H3 bar."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

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
    clay = material("silhouette_clay", (0.54, 0.50, 0.45, 1.0), 0.82)
    dark_clay = material("silhouette_dark_clay", (0.27, 0.25, 0.23, 1.0), 0.78)
    trim_clay = material("silhouette_trim", (0.68, 0.64, 0.57, 1.0), 0.68)
    glass_clay = material("silhouette_glass", (0.28, 0.39, 0.44, 1.0), 0.42)
    metal_clay = material("silhouette_metal", (0.36, 0.37, 0.38, 1.0), 0.32, 0.35)

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
