"""Generated Blender entry point for bar-architecture.

The scaffold contains no geometry or material decisions. Implement each builder
from the approved contract, preserving the stable asset ID and required anchors.
"""

import argparse
import sys
from pathlib import Path

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
MODELING_TOOLS = Path(__file__).resolve().parent
BLENDER_TOOLS = REPOSITORY_ROOT / "tools" / "blender"
if str(MODELING_TOOLS) not in sys.path:
    sys.path.insert(0, str(MODELING_TOOLS))
if str(BLENDER_TOOLS) not in sys.path:
    sys.path.insert(0, str(BLENDER_TOOLS))

from build_bar_master import build_master_scene
from export_bar_modules import export_module
from bar_architecture_asset_contract import ASSETS, BATCH_ID, approved_contracts


SILHOUETTE_CHECKPOINT_STATUS = 'approved'
BUILDERS = {}

def build_bar_architecture(contract):
    if contract["room_size_m"] != (16.0, 10.0, 4.5):
        raise RuntimeError("Approved architecture contract has drifted from Z3/H3")
    build_master_scene()

BUILDERS['bar_architecture'] = build_bar_architecture


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else None
    parser = argparse.ArgumentParser(description="Generate approved bar-architecture Blender assets")
    parser.add_argument("--mode", required=True, choices=("silhouette", "final"))
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args(argv)


def main():
    args = parse_args()
    if args.mode == "final" and SILHOUETTE_CHECKPOINT_STATUS != "approved":
        raise SystemExit("final mode is locked until checkpoint 1 has explicit user approval")

    contracts = approved_contracts()
    args.output.mkdir(parents=True, exist_ok=True)
    for asset in ASSETS:
        asset_id = asset["asset_id"]
        BUILDERS[asset_id](contracts[asset_id])
        export_module(
            asset_id,
            args.mode,
            args.output / f"{asset_id}.glb",
            REPOSITORY_ROOT / "docs" / "assets" / "bar_architecture_asset_batch.json",
        )
    print(f"BAR_ARCHITECTURE_BATCH_GENERATION_PASS batch={BATCH_ID} mode={args.mode}")


if __name__ == "__main__":
    main()
