"""Generated neutral-review renderer for bar-architecture.

Run inside Blender only after approved geometry builders exist. Camera, scale
reference, lighting, and composition values must come from the batch design or an
approved repository baseline; this template intentionally supplies none.
"""

import argparse
import sys
from pathlib import Path

import bpy

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
BLENDER_TOOLS = REPOSITORY_ROOT / "tools" / "blender"
if str(BLENDER_TOOLS) not in sys.path:
    sys.path.insert(0, str(BLENDER_TOOLS))

from render_bar_review import VIEWS, render_views

REQUIRED_VIEWS = tuple(view[0] for view in VIEWS)


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else None
    parser = argparse.ArgumentParser(description="Render neutral bar-architecture review evidence")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args(argv)


def main():
    args = parse_args()
    input_path = args.input if args.input.is_absolute() else REPOSITORY_ROOT / args.input
    if not input_path.is_file():
        raise RuntimeError(f"Missing approved architecture master: {input_path}")
    bpy.ops.wm.open_mainfile(filepath=str(input_path.resolve()))
    render_views((args.output if args.output.is_absolute() else REPOSITORY_ROOT / args.output).resolve())


if __name__ == "__main__":
    main()
