"""Build and export the complete approved bar-interior module batch."""

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
from bar_model_common import MODULE_NAMES
from bar_architecture_asset_contract import BATCH_ID


SILHOUETTE_CHECKPOINT_STATUS = 'approved'


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

    args.output.mkdir(parents=True, exist_ok=True)
    build_master_scene()
    for asset_id in MODULE_NAMES:
        export_module(
            asset_id,
            args.mode,
            args.output / f"{asset_id}.glb",
            REPOSITORY_ROOT / "docs" / "assets" / "bar_architecture_asset_batch.json",
        )
    print(f"BAR_ARCHITECTURE_BATCH_GENERATION_PASS batch={BATCH_ID} mode={args.mode}")


if __name__ == "__main__":
    main()
