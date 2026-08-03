"""Generated contract skeleton for bar-architecture.

Replace the stop marker only after the batch design and per-asset contracts are
approved. Do not invent envelopes, capacities, materials, poses, or gameplay data.
"""

BATCH_ID = 'bar-architecture'
STAGE = 'approved checkpoint 1 formal candidate'
ASSETS = [{'asset_id': 'bar_architecture', 'runtime_id': 'bar_architecture', 'required_anchors': ['Placement'], 'interaction_kind': 'environment_visual'}]


def approved_contracts():
    """Return approved per-asset contract data after the design is recorded."""
    return {
        "bar_architecture": {
            "required_root": "bar_architecture",
            "required_anchors": ("Placement",),
            "required_nodes": (
                "room_shell",
                "south_main_entry",
                "south_east_window",
                "north_east_service_door",
            ),
            "room_size_m": (16.0, 10.0, 4.5),
            "south_main_entry_m": (1.40, 2.10),
            "south_east_window_m": (3.20, 1.55),
            "south_east_window_sill_m": 0.75,
            "north_east_service_door_m": (0.90, 2.10),
            "wainscot_height_m": 1.05,
            "floor_board_width_m": 0.18,
            "visual_only": True,
        }
    }


def asset_by_id(asset_id):
    for asset in ASSETS:
        if asset["asset_id"] == asset_id:
            return asset
    raise KeyError(asset_id)
